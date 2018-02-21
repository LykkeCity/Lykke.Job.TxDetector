using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Services.BitCoin.Ninja;
using Lykke.JobTriggers.Triggers.Attributes;
using QBitNinja.Client.Models;

namespace Lykke.Job.TxDetector.TriggerHandlers
{
    public class WalletsScannerFunctions
    {
        private readonly IWalletCredentialsRepository _walletCredentialsRepository;
        private readonly ILog _log;
        private readonly IInternalOperationsRepository _internalOperationsRepository;
        private readonly ILastProcessedBlockRepository _lastProcessedBlockRepository;
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly AppSettings.NinjaSettings _ninjaSettings;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IBcnClientCredentialsRepository _bcnClientCredentialsRepository;
        private readonly IQBitNinjaApiCaller _qbitNinjaApiCaller;

        public WalletsScannerFunctions(IWalletCredentialsRepository walletCredentialsRepository,
            ILog log, IInternalOperationsRepository internalOperationsRepository,
            ILastProcessedBlockRepository lastProcessedBlockRepository, IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            IAppGlobalSettingsRepositry appGlobalSettingsRepositry,
            ICqrsEngine cqrsEngine, IBcnClientCredentialsRepository bcnClientCredentialsRepository, IQBitNinjaApiCaller qbitNinjaApiCaller, AppSettings.NinjaSettings ninjaSettings)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
            _log = log;
            _internalOperationsRepository = internalOperationsRepository;
            _lastProcessedBlockRepository = lastProcessedBlockRepository;
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository;
            _appGlobalSettingsRepositry = appGlobalSettingsRepositry;
            _cqrsEngine = cqrsEngine;
            _bcnClientCredentialsRepository = bcnClientCredentialsRepository;
            _qbitNinjaApiCaller = qbitNinjaApiCaller;
            _ninjaSettings = ninjaSettings;
        }

        [TimerTrigger("00:01:00")]
        public async Task StartDetection()
        {
            if ((await _appGlobalSettingsRepositry.GetAsync()).BitcoinBlockchainOperationsDisabled)
            {
                await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(StartDetection), "",
                    "Detection skipped. Btc operations disabled");

                return;
            }

            var dtStart = DateTime.UtcNow;
            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(StartDetection), "",
                $"Detection started at:{dtStart}");

            try
            {
                await StartBlockScan();
            }
            catch (Exception exc) when (exc is TaskCanceledException || exc is WebException)
            {
                await _log.WriteWarningAsync(
                    nameof(TxDetector),
                    nameof(WalletsScannerFunctions),
                    nameof(StartDetection),
                    exc.GetBaseException().Message,
                    DateTime.UtcNow);
            }

            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(StartDetection), "",
                $"Detection finished. Scan duration: {DateTime.UtcNow - dtStart}");
        }

        private async Task StartBlockScan()
        {
            var multisigs = (await _walletCredentialsRepository.GetAllAsync()).GroupBy(x => x.MultiSig).ToDictionary(x => x.Key, x => x.First().ClientId);
            var segwits = (await _bcnClientCredentialsRepository.GetAllAsync(LykkeConstants.BitcoinAssetId)).GroupBy(x => x.AssetAddress).ToDictionary(x => x.Key, x => x.First().ClientId);

            var currentBlock = await _lastProcessedBlockRepository.GetLastProcessedBlockHeightAsync() ?? await _lastProcessedBlockRepository.GetMinBlockHeight();

            do
            {
                var dtStart = DateTime.UtcNow;
                await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(StartBlockScan), "", $"Start processing block {currentBlock}.");

                var block = await _qbitNinjaApiCaller.GetBlock(currentBlock);
                if (block == null)
                    break;

                foreach (var transaction in block.Block.Transactions)
                {
                    GetTransactionResponse coloredTx = null;
                    var usedAddresses = new HashSet<string>();

                    foreach (var transactionOutput in transaction.Outputs.AsIndexedOutputs())
                    {
                        var address = transactionOutput.TxOut.ScriptPubKey.GetDestinationAddress(_ninjaSettings.GetNetwork())?.ToString();

                        if (string.IsNullOrWhiteSpace(address) || usedAddresses.Contains(address))
                            continue;

                        if (multisigs.ContainsKey(address))
                        {
                            usedAddresses.Add(address);

                            coloredTx = coloredTx ?? await _qbitNinjaApiCaller.GetTransaction(transaction.GetHash().ToString());

                            var convertedTx = coloredTx.ConvertToBlockchainTransaction(_ninjaSettings.IsMainNet, address);

                            await ProcessNewTransaction(multisigs[address], address, convertedTx, false);

                            await _log.WriteWarningAsync(nameof(WalletsScannerFunctions), nameof(StartBlockScan), "", $"Client have used old multisig for cashin: {address}.");
                        }
                        else if (segwits.ContainsKey(address))
                        {
                            usedAddresses.Add(address);

                            coloredTx = coloredTx ?? await _qbitNinjaApiCaller.GetTransaction(transaction.GetHash().ToString());

                            var convertedTx = coloredTx.ConvertToBlockchainTransaction(_ninjaSettings.IsMainNet, address);

                            await ProcessNewTransaction(segwits[address], address, convertedTx, true);
                        }
                    }
                }

                currentBlock++;

                await _lastProcessedBlockRepository.UpdateLastProcessedBlockHeightAsync(currentBlock);

                await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(StartBlockScan), "", $"Finish processing block {currentBlock - 1}. Duration: {DateTime.UtcNow - dtStart}");

            } while (true);
        }

        private async Task ProcessNewTransaction(string clientId, string address, IBlockchainTransaction tx, bool segwit)
        {
            var balanceChangeTx = BalanceChangeTransaction.Create(tx, clientId, address, segwit);

            var shouldBeProcessed = await _balanceChangeTransactionsRepository.InsertIfNotExistsAsync(balanceChangeTx);

            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(ProcessNewTransaction),
                $"ClientId: {balanceChangeTx.ClientId}, tx hash: {balanceChangeTx.Hash}",
                $"Got transaction; shouldBeProcessed: {shouldBeProcessed}");

            if (shouldBeProcessed)
                await HandleDetectedTransaction(address, tx, balanceChangeTx);
        }

        private async Task HandleDetectedTransaction(string address, IBlockchainTransaction tx, IBalanceChangeTransaction balanceChangeTx)
        {
            var internalOperation = await _internalOperationsRepository.GetAsync(tx.Hash);

            if (internalOperation?.CommandType == BitCoinCommands.Transfer
                     || IsExternalCashIn(address, tx, internalOperation)
                     || IsOtherClientsCashOut(address, tx, internalOperation))
            {
                var processTransactionCommand = new ProcessTransactionCommand
                {
                    TransactionHash = balanceChangeTx.Hash
                };

                _cqrsEngine.SendCommand(processTransactionCommand, "transactions", "transactions");
            }
        }

        private static bool IsOtherClientsCashOut(string address, IBlockchainTransaction tx, IInternalOperation internalOperation)
        {
            return tx.IsCashIn(address) && internalOperation?.CommandType == BitCoinCommands.CashOut;
        }

        private static bool IsExternalCashIn(string address, IBlockchainTransaction tx, IInternalOperation internalOperation)
        {
            return tx.IsCashIn(address) && internalOperation == null;
        }
    }
}
