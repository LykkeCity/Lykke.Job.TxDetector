using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Sagas.Commands;
using Lykke.Job.TxDetector.Sagas.Events;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Job.TxDetector.Sagas.Handlers
{
    public class ConfirmationsHandler
    {
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly IInternalOperationsRepository _internalOperationsRepository;
        private readonly ICachedAssetsService _assetsService;
        private readonly IConfirmedTransactionsRepository _confirmedTransactionsRepository;
        private readonly IPostponedCashInRepository _postponedCashInRepository;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;

        private readonly ILog _log;

        public ConfirmationsHandler(
            IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            IInternalOperationsRepository internalOperationsRepository,
            ICachedAssetsService assetsService,
            IConfirmedTransactionsRepository confirmedTransactionsRepository,
            IPostponedCashInRepository postponedCashInRepository,
            IAppGlobalSettingsRepositry appGlobalSettingsRepositry,
            ILog log)
        {
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository;
            _internalOperationsRepository = internalOperationsRepository;
            _assetsService = assetsService;
            _confirmedTransactionsRepository = confirmedTransactionsRepository;
            _postponedCashInRepository = postponedCashInRepository;
            _appGlobalSettingsRepositry = appGlobalSettingsRepositry;
            _log = log;
        }

        public async Task Handle(ProcessTransactionCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsHandler), nameof(ProcessTransactionCommand), command.ToJson());
            var hash = command.TransactionHash;

            var balanceChangeTransaction = await _balanceChangeTransactionsRepository.GetAsync(hash);

            var operation = await _internalOperationsRepository.GetAsync(hash);

            foreach (var tx in balanceChangeTransaction)
            {
                var alreadyProcessed = !await _confirmedTransactionsRepository.SaveConfirmedIfNotExist(hash, tx.ClientId);

                if (alreadyProcessed)
                    continue;

                if (operation != null && operation.CommandType == BitCoinCommands.Transfer)
                {
                    foreach (var id in operation.OperationIds)
                    {
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        eventPublisher.PublishEvent(new TransferOperationCreatedEvent { TransferId = id, ClientId = tx.ClientId });
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

                            var skipBtc = (await _appGlobalSettingsRepositry.GetAsync()).BtcOperationsDisabled;
                            if (asset.Id == LykkeConstants.BitcoinAssetId && skipBtc)
                            {
                                await _postponedCashInRepository.SaveAsync(tx.Hash);
                                continue;
                            }

                            double sum = cashIn.Value * Math.Pow(10, -asset.MultiplierPower);

                            eventPublisher.PublishEvent(new CashInOperationCreatedEvent { Transaction = tx, Asset = asset, Amount = sum });
                        }
                    }
                }
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