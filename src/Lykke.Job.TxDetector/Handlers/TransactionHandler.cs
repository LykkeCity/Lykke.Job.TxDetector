using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Models;
using Lykke.Job.TxDetector.Utils;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Job.TxDetector.Handlers
{
    public class TransactionHandler
    {
        private static readonly TimeSpan RetryTimeoutForTransactionConfirmations = TimeSpan.FromSeconds(10);

        private readonly ILog _log;
        private readonly AppSettings.TxDetectorSettings _settings;
        private readonly ISrvBlockchainReader _srvBlockchainReader;
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly IInternalOperationsRepository _internalOperationsRepository;
        private readonly ICachedAssetsService _assetsService;
        private readonly IConfirmedTransactionsRepository _confirmedTransactionsRepository;
        private readonly IPostponedCashInRepository _postponedCashInRepository;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;

        public TransactionHandler(
            [NotNull] ILog log,
            [NotNull] IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            [NotNull] IInternalOperationsRepository internalOperationsRepository,
            [NotNull] ICachedAssetsService assetsService,
            [NotNull] IConfirmedTransactionsRepository confirmedTransactionsRepository,
            [NotNull] IPostponedCashInRepository postponedCashInRepository,
            [NotNull] IAppGlobalSettingsRepositry appGlobalSettingsRepositry,
            [NotNull] AppSettings.TxDetectorSettings settings,
            [NotNull] ISrvBlockchainReader srvBlockchainReader)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository ?? throw new ArgumentNullException(nameof(balanceChangeTransactionsRepository));
            _internalOperationsRepository = internalOperationsRepository ?? throw new ArgumentNullException(nameof(internalOperationsRepository));
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));
            _confirmedTransactionsRepository = confirmedTransactionsRepository ?? throw new ArgumentNullException(nameof(confirmedTransactionsRepository));
            _postponedCashInRepository = postponedCashInRepository ?? throw new ArgumentNullException(nameof(postponedCashInRepository));
            _appGlobalSettingsRepositry = appGlobalSettingsRepositry ?? throw new ArgumentNullException(nameof(appGlobalSettingsRepositry));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _srvBlockchainReader = srvBlockchainReader ?? throw new ArgumentNullException(nameof(srvBlockchainReader));
        }

        // entry point
        public async Task<CommandHandlingResult> Handle(ProcessTransactionCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(TransactionHandler), nameof(ProcessTransactionCommand), command.ToJson(), "");

            var confirmations = await _srvBlockchainReader.GetConfirmationsCount(command.TransactionHash);
            var isConfirmed = confirmations >= _settings.TxDetectorConfirmationsLimit;
            if (!isConfirmed)
            {
                //put back if not confirmed yet
                return new CommandHandlingResult { Retry = true, RetryDelay = (long)RetryTimeoutForTransactionConfirmations.TotalMilliseconds };
            }

            ChaosKitty.Meow();

            var hash = command.TransactionHash;

            var balanceChangeTransaction = await _balanceChangeTransactionsRepository.GetAsync(hash);

            var operation = await _internalOperationsRepository.GetAsync(hash);

            foreach (var tx in balanceChangeTransaction)
            {
                var alreadyProcessed = !await _confirmedTransactionsRepository.SaveConfirmedIfNotExist(hash, tx.ClientId);
                if (alreadyProcessed)
                {
                    await _log.WriteInfoAsync(nameof(TransactionHandler), nameof(ProcessTransactionCommand), "",
                        $"Transaction with hash {hash} for client {tx.ClientId} is already processed; ignoring it.");
                    continue;
                }

                if (operation != null && operation.CommandType == BitCoinCommands.Transfer)
                {
                    foreach (var id in operation.OperationIds)
                    {
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        eventPublisher.PublishEvent(new TransferOperationCreatedEvent { TransferId = id });
                    }
                }
                else
                {
                    if (tx.IsCashIn(tx.Multisig))
                    {
                        var cashIns = tx.GetOperationSummary(tx.Multisig);

                        var skipBtc = (await _appGlobalSettingsRepositry.GetAsync()).BtcOperationsDisabled;

                        foreach (var cashIn in cashIns)
                        {
                            var asset = await GetAssetByBcnIdAsync(cashIn.Key);

                            if (asset.Id == LykkeConstants.BitcoinAssetId && skipBtc)
                            {
                                await _postponedCashInRepository.SaveAsync(tx.Hash);
                                continue;
                            }

                            var sum = cashIn.Value * Math.Pow(10, -asset.MultiplierPower);

                            eventPublisher.PublishEvent(new CashInOperationCreatedEvent
                            {
                                Transaction = new Transaction { Hash = tx.Hash, ClientId = tx.ClientId, Multisig = tx.Multisig },
                                Asset = new Asset { Id = asset.Id, Accuracy = asset.Accuracy },
                                Amount = sum
                            });
                        }
                    }
                }
            }
            return CommandHandlingResult.Ok();
        }

        private async Task<IAsset> GetAssetByBcnIdAsync(string bcnId)
        {
            return string.IsNullOrEmpty(bcnId)
                ? await _assetsService.TryGetAssetAsync(LykkeConstants.BitcoinAssetId)
                : (await _assetsService.GetAllAssetsAsync()).FirstOrDefault(x => x.BlockChainAssetId == bcnId);
        }
    }
}