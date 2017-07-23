using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.TriggerHandlers.Handlers;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Job.TxDetector.TriggerHandlers
{
    public class ConfirmationsPendingScanner
    {
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly AppSettings.TxDetectorSettings _settings;
        private readonly IInternalOperationsRepository _internalOperationsRepository;
        private readonly TransferHandler _transferHandler;
        private readonly CashInHandler _cashInHandler;
        private readonly ICachedAssetsService _assetsService;
        private readonly ISrvBlockchainReader _srvBlockchainReader;
        private readonly IConfirmedTransactionsRepository _confirmedTransactionsRepository;

        public ConfirmationsPendingScanner(IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            AppSettings.TxDetectorSettings settings, IInternalOperationsRepository internalOperationsRepository, TransferHandler transferHandler,
            CashInHandler cashInHandler, ICachedAssetsService assetsService,
            ISrvBlockchainReader srvBlockchainReader,
            IConfirmedTransactionsRepository confirmedTransactionsRepository)
        {
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository;
            _settings = settings;
            _internalOperationsRepository = internalOperationsRepository;
            _transferHandler = transferHandler;
            _cashInHandler = cashInHandler;
            _assetsService = assetsService;
            _srvBlockchainReader = srvBlockchainReader;
            _confirmedTransactionsRepository = confirmedTransactionsRepository;
        }

        [QueueTrigger("txs-confirm-pending", maxPollingIntervalMs: 1000, maxDequeueCount: 1)]
        public async Task HandlePending(string message, QueueTriggeringContext context)
        {
            var txMsg = message.DeserializeJson<PendingTxMsg>();

            if (txMsg == null)
                throw new Exception("Unknown msg");

            var confirmations = await _srvBlockchainReader.GetConfirmationsCount(txMsg.Hash);

            if (confirmations >= _settings.TxDetectorConfirmationsLimit)
            {
                var alreadyProcessed = !await _confirmedTransactionsRepository.SaveConfirmedIfNotExist(txMsg.Hash);

                if (alreadyProcessed)
                    return;

                var balanceChangeTransaction = await _balanceChangeTransactionsRepository.GetAsync(txMsg.Hash);

                foreach (var tx in balanceChangeTransaction)
                {
                    var operation = await _internalOperationsRepository.GetAsync(txMsg.Hash);

                    if (operation != null && operation.CommandType == BitCoinCommands.Transfer)
                    {
                        foreach (var id in operation.OperationIds)
                        {
                            await _transferHandler.HandleTransferOperation(id, tx.ClientId);
                        }
                    }
                    else
                    {
                        if (tx.IsCashIn(tx.Multisig))
                        {
                            var cashIns = tx.GetOperationSummary(tx.Multisig);

                            foreach (var cashIn in cashIns)
                            {
                                var asset = await GetAssetByBcnIdAsync(cashIn.Key);
                                double sum = cashIn.Value * Math.Pow(10, -asset.MultiplierPower);

                                await _cashInHandler.HandleCashInOperation(tx, asset, sum);
                            }
                        }
                    }
                }
            }
            else
            {
                //put back if not confirmed yet
                context.MoveMessageToEnd(message);
                context.SetCountQueueBasedDelay(500, 100);
            }
        }

        private async Task<IAsset> GetAssetByBcnIdAsync(string bcnId)
        {
            return string.IsNullOrEmpty(bcnId)
                ? await _assetsService.TryGetAssetAsync(LykkeConstants.BitcoinAssetId)
                : (await _assetsService.GetAllAssetsAsync()).FirstOrDefault(x => x.BlockChainAssetId == bcnId);
        }
    }
}