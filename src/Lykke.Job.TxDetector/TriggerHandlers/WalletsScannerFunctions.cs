using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.CashOperations;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.JobTriggers.Triggers.Attributes;

namespace Lykke.Job.TxDetector.TriggerHandlers
{
    public class WalletsScannerFunctions
    {
        private readonly IWalletCredentialsRepository _walletCredentialsRepository;
        private readonly ISrvBlockchainReader _srvBlockchainReader;
        private readonly IClientTradesRepository _clientTradesRepository;
        private readonly ILog _log;
        private readonly IInternalOperationsRepository _internalOperationsRepository;
        private readonly ILastProcessedBlockRepository _lastProcessedBlockRepository;
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly IConfirmPendingTxsQueue _confirmPendingTxsQueue;
        private readonly IBlockchainTransactionsCache _blockchainTransactionsCache;

        private int _currentBlockHeight;

        private const int ProcessInParallelCount = 5;

        public WalletsScannerFunctions(IWalletCredentialsRepository walletCredentialsRepository,
            ISrvBlockchainReader srvBlockchainReader, IClientTradesRepository clientTradesRepository,
            ILog log, IInternalOperationsRepository internalOperationsRepository,
            ILastProcessedBlockRepository lastProcessedBlockRepository, IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            IConfirmPendingTxsQueue confirmPendingTxsQueue,
            IBlockchainTransactionsCache blockchainTransactionsCache)
        {
            _walletCredentialsRepository = walletCredentialsRepository;
            _srvBlockchainReader = srvBlockchainReader;
            _clientTradesRepository = clientTradesRepository;
            _log = log;
            _internalOperationsRepository = internalOperationsRepository;
            _lastProcessedBlockRepository = lastProcessedBlockRepository;
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository;
            _confirmPendingTxsQueue = confirmPendingTxsQueue;
            _blockchainTransactionsCache = blockchainTransactionsCache;
        }

        [TimerTrigger("00:02:00")]
        public async Task ScanClients()
        {
            var dtStart = DateTime.UtcNow;
            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(ScanClients), "",
                $"Scan started at:{dtStart}");

            _currentBlockHeight = await _srvBlockchainReader.GetCurrentBlockHeight();
            await _walletCredentialsRepository.ScanAllAsync(HandleWallets);

            await _log.WriteInfoAsync(nameof(WalletsScannerFunctions), nameof(ScanClients), "",
                $"Scan finised. Scan duration: {DateTime.UtcNow - dtStart}");
        }

        private async Task HandleWallets(IEnumerable<IWalletCredentials> walletCredentials)
        {
            foreach (var chunk in walletCredentials.ToChunks(ProcessInParallelCount))
            {
                var tasks = new List<Task>();
                foreach (var item in chunk)
                {
                    tasks.Add(HandleWallet(item));
                }

                try
                {
                    await Task.WhenAll(tasks);
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

            var alreadyProcessedHashes = new List<string>();
            if (lastProcessedBlockHeight == 0)
            {
                //Processed in old version
                //ToDo: can be safely removed later
                alreadyProcessedHashes =
                    (await _blockchainTransactionsCache.GetAllAsync(walletCredentials.MultiSig)).Select(x => x.TxId)
                    .ToList();
            }

            foreach (var tx in newTransactions)
            {
                var balanceChangeTx = BalanceChangeTransaction.Create(tx,
                    walletCredentials.ClientId, walletCredentials.MultiSig);

                //check if transaction was already proccessed (ninja issue https://github.com/MetacoSA/QBitNinja/issues/24 or some fail during processing occured)
                var shouldBeProcessed =
                    await _balanceChangeTransactionsRepository.InsertIfNotExistsAsync(balanceChangeTx);

                if (!alreadyProcessedHashes.Contains(tx.Hash) && shouldBeProcessed)
                    await HandleDetectedTransaction(walletCredentials, tx, balanceChangeTx);
            }

            await _lastProcessedBlockRepository.InsertOrUpdateForClientAsync(walletCredentials.ClientId,
                _currentBlockHeight);
        }

        private async Task HandleDetectedTransaction(IWalletCredentials walletCredentials, IBlockchainTransaction tx, IBalanceChangeTransaction balanceChangeTx)
        {
            var internalOperation = await _internalOperationsRepository.GetAsync(tx.Hash);

            if (internalOperation?.CommandType == BitCoinCommands.Swap)
            {
                foreach (var id in internalOperation.OperationIds)
                {
                    await
                        _clientTradesRepository.SetDetectionTimeAndConfirmations(
                            walletCredentials.ClientId, id, DateTime.UtcNow,
                            tx.Confirmations);
                }
            }
            else if (internalOperation?.CommandType == BitCoinCommands.Transfer
                     || IsExternalCashIn(walletCredentials, tx, internalOperation)
                     || IsOtherClientsCashOut(walletCredentials, tx, internalOperation))
            {
                await _confirmPendingTxsQueue.PutAsync(new PendingTxMsg { Hash = balanceChangeTx.Hash });
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
