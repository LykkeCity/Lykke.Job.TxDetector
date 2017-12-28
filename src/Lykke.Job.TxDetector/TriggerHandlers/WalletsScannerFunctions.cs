﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.TxDetector.TriggerHandlers
{
    public class WalletsScannerFunctions
    {
        private readonly IWalletCredentialsRepository _walletCredentialsRepository;
        private readonly ISrvBlockchainReader _srvBlockchainReader;
        private readonly ITradeOperationsRepositoryClient _clientTradesRepositoryClient;
        private readonly ILog _log;
        private readonly IInternalOperationsRepository _internalOperationsRepository;
        private readonly ILastProcessedBlockRepository _lastProcessedBlockRepository;
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly AppSettings.TxDetectorSettings _txDetectorSettings;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;
        private readonly ICqrsEngine _cqrsEngine;

        private int _currentBlockHeight;

        public WalletsScannerFunctions(IWalletCredentialsRepository walletCredentialsRepository,
            ISrvBlockchainReader srvBlockchainReader, ITradeOperationsRepositoryClient clientTradesRepositoryClient,
            ILog log, IInternalOperationsRepository internalOperationsRepository,
            ILastProcessedBlockRepository lastProcessedBlockRepository, IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            AppSettings.TxDetectorSettings txDetectorSettings,
            IAppGlobalSettingsRepositry appGlobalSettingsRepositry,
            ICqrsEngine cqrsEngine)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
            _srvBlockchainReader = srvBlockchainReader;
            _clientTradesRepositoryClient = clientTradesRepositoryClient;
            _log = log;
            _internalOperationsRepository = internalOperationsRepository;
            _lastProcessedBlockRepository = lastProcessedBlockRepository;
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository;
            _txDetectorSettings = txDetectorSettings;
            _appGlobalSettingsRepositry = appGlobalSettingsRepositry;
            _cqrsEngine = cqrsEngine;
        }

        [TimerTrigger("00:02:00")]
        public async Task ScanClients()
        {
            if ((await _appGlobalSettingsRepositry.GetAsync()).BitcoinBlockchainOperationsDisabled)
            {
                await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(ScanClients), "",
                    "Scan skipped. Btc operations disabled");

                return;
            }

            var dtStart = DateTime.UtcNow;
            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(ScanClients), "",
                $"Scan started at:{dtStart}");

            try
            {
                _currentBlockHeight = await _srvBlockchainReader.GetCurrentBlockHeight();
                await _walletCredentialsRepository.ScanAllAsync(HandleWallets);
            }
            catch (Exception exc) when (exc is TaskCanceledException || exc is WebException)
            {
                await _log.WriteWarningAsync(
                    nameof(TxDetector),
                    nameof(WalletsScannerFunctions),
                    nameof(ScanClients),
                    exc.GetBaseException().Message,
                    DateTime.UtcNow);
            }

            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(ScanClients), "",
                $"Scan finished. Scan duration: {DateTime.UtcNow - dtStart}");
        }

        private async Task HandleWallets(IEnumerable<IWalletCredentials> walletCredentials)
        {
            foreach (var chunk in walletCredentials.ToChunks(_txDetectorSettings.ProcessInParallelCount))
            {
                try
                {
                    await Task.WhenAll(chunk.Select(HandleWallet));
                }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync(nameof(WalletsScannerFunctions), nameof(HandleWallets), "", ex);
                }
            }
        }

        private async Task HandleWallet(IWalletCredentials walletCredentials)
        {
            var lastProcessedBlockHeight =
                await _lastProcessedBlockRepository.GetLastProcessedBlockHeightAsync(walletCredentials.ClientId);

            if (lastProcessedBlockHeight == _currentBlockHeight)
                return;

            var newTransactions = (await _srvBlockchainReader.GetBalanceChangesByAddressAsync(walletCredentials.MultiSig, lastProcessedBlockHeight))
                .ToArray();

            foreach (var tx in newTransactions)
            {
                var balanceChangeTx = BalanceChangeTransaction.Create(tx,
                    walletCredentials.ClientId, walletCredentials.MultiSig);

                //check if transaction was already processed (ninja issue https://github.com/MetacoSA/QBitNinja/issues/24 or some fail during processing occurred)
                var shouldBeProcessed = await _balanceChangeTransactionsRepository.InsertIfNotExistsAsync(balanceChangeTx);

                await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(HandleWallet),
                    $"ClientId: {balanceChangeTx.ClientId}, tx hash: {balanceChangeTx.Hash}",
                    $"Got transaction; shouldBeProcessed: {shouldBeProcessed}");

                if (shouldBeProcessed)
                    await HandleDetectedTransaction(walletCredentials, tx, balanceChangeTx);
            }

            try
            {
                await _lastProcessedBlockRepository.InsertOrUpdateForClientAsync(walletCredentials.ClientId,
                    _currentBlockHeight);
            }
            catch (TaskCanceledException ex)
            {
                await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(HandleWallet), "",
                    $"Timeout while updating last block for {walletCredentials.ClientId}");
            }
        }

        private async Task HandleDetectedTransaction(IWalletCredentials walletCredentials, IBlockchainTransaction tx, IBalanceChangeTransaction balanceChangeTx)
        {
            var internalOperation = await _internalOperationsRepository.GetAsync(tx.Hash);

            if (internalOperation?.CommandType == BitCoinCommands.Swap)
            {
                foreach (var id in internalOperation.OperationIds)
                {
                    await _clientTradesRepositoryClient.SetDetectionTimeAndConfirmations(
                            walletCredentials.ClientId, id, DateTime.UtcNow,
                            tx.Confirmations);
                }
            }
            else if (internalOperation?.CommandType == BitCoinCommands.Transfer
                     || IsExternalCashIn(walletCredentials, tx, internalOperation)
                     || IsOtherClientsCashOut(walletCredentials, tx, internalOperation))
            {
                var processTransactionCommand = new ProcessTransactionCommand
                {
                    TransactionHash = balanceChangeTx.Hash
                };

                _cqrsEngine.SendCommand(processTransactionCommand, "transactions", "transactions");
            }
        }

        private static bool IsOtherClientsCashOut(IWalletCredentials walletCredentials, IBlockchainTransaction tx, IInternalOperation internalOperation)
        {
            return tx.IsCashIn(walletCredentials.MultiSig) && internalOperation?.CommandType == BitCoinCommands.CashOut;
        }

        private static bool IsExternalCashIn(IWalletCredentials walletCredentials, IBlockchainTransaction tx, IInternalOperation internalOperation)
        {
            return tx.IsCashIn(walletCredentials.MultiSig) && internalOperation == null;
        }
    }
}
