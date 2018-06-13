using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Models;
using Lykke.Job.TxDetector.Resources;
using Lykke.Job.TxDetector.Utils;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Sagas
{
    public class ConfirmationsSaga
    {
        private readonly ILog _log;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IAssetsServiceWithCache _assetsService;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly IInternalOperationsRepository _internalOperationsRepository;

        public ConfirmationsSaga(
            [NotNull] ILog log,
            [NotNull] IClientAccountClient clientAccountClient,
            [NotNull] IAssetsServiceWithCache assetsService,
            [NotNull] IAppGlobalSettingsRepositry appGlobalSettingsRepositry,
            [NotNull] IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            [NotNull] IInternalOperationsRepository internalOperationsRepository)
        {
            _log = log.CreateComponentScope(nameof(ConfirmationsSaga));
            _clientAccountClient = clientAccountClient ?? throw new ArgumentNullException(nameof(clientAccountClient));
            _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));
            _appGlobalSettingsRepositry = appGlobalSettingsRepositry ?? throw new ArgumentNullException(nameof(appGlobalSettingsRepositry));
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository ?? throw new ArgumentNullException(nameof(balanceChangeTransactionsRepository));
            _internalOperationsRepository = internalOperationsRepository ?? throw new ArgumentNullException(nameof(internalOperationsRepository));
        }

        private void Handle(CashInOutOperationRegisteredEvent evt, ICommandSender sender)
        {
            var cmd = new RegisterBitcoinCashInCommand
            {
                Transaction = evt.Transaction,
                Asset = evt.Asset,
                Amount = evt.Amount,
                CommandId = evt.CommandId
            };

            sender.SendCommand(cmd, "cashin");
        }

        private void Handle(BitcoinCashInRegisteredEvent evt, ICommandSender sender)
        {
            var cmd = new ProcessCashInCommand
            {
                Transaction = evt.Transaction,
                Asset = evt.Asset,
                Amount = evt.Amount,
                CommandId = evt.CommandId
            };

            sender.SendCommand(cmd, "cashin");
        }

        private async Task Handle(TransactionProcessedEvent evt, ICommandSender sender)
        {
            ChaosKitty.Meow();

            var clientAcc = await _clientAccountClient.GetByIdAsync(evt.ClientId);

            var sendEmailCommand = new SendNoRefundDepositDoneMailCommand
            {
                Email = clientAcc.Email,
                Amount = evt.Amount,
                AssetId = evt.Asset.Id
            };
            sender.SendCommand(sendEmailCommand, "email");

            ChaosKitty.Meow();

            var pushSettings = await _clientAccountClient.GetPushNotificationAsync(evt.ClientId);
            if (pushSettings.Enabled)
            {
                var sendNotificationCommand = new SendNotificationCommand
                {
                    NotificationId = clientAcc.NotificationsId,
                    Type = NotificationType.TransactionConfirmed,
                    Message = string.Format(TextResources.CashInSuccessText, new decimal(evt.Amount).TruncateDecimalPlaces(evt.Asset.Accuracy), evt.Asset.Id)
                };
                sender.SendCommand(sendNotificationCommand, "notifications");
            }
        }

        private async Task Handle(ConfirmationSavedEvent evt, ICommandSender sender)
        {
            var hash = evt.TransactionHash;
            var clientId = evt.ClientId;

            var balanceChangeTransaction = await _balanceChangeTransactionsRepository.GetAsync(hash);

            var operation = await _internalOperationsRepository.GetAsync(hash);

            var tx = balanceChangeTransaction.First(x => x.ClientId == clientId);

            ChaosKitty.Meow();

            if (operation != null && operation.CommandType == BitCoinCommands.Transfer)
            {
                foreach (var id in operation.OperationIds)
                {
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    sender.SendCommand(new ProcessTransferCommand
                    {
                        TransferId = id
                    }, "transfer");
                }
            }
            else
            {
                if (tx.IsCashIn(tx.Multisig))
                {
                    var cashIns = tx.GetOperationSummary(tx.Multisig);
                    if (cashIns.Count > 1)
                    {
                        _log.WriteWarning(nameof(ConfirmationSavedEvent), evt, $"Multiple assets in a single transaction detected: {cashIns.ToJson()}");
                        // there should be only one asset in cash-in operation; 
                        // code bellow with 'foreach' statement is kept for a while in case of obsolete request with multiple assets;
                    }

                    var skipBtc = (await _appGlobalSettingsRepositry.GetAsync()).BtcOperationsDisabled;

                    foreach (var cashIn in cashIns)
                    {
                        var asset = await GetAssetByBcnIdAsync(cashIn.Key);

                        ChaosKitty.Meow();

                        if (asset.Id == LykkeConstants.BitcoinAssetId && skipBtc)
                        {
                            sender.SendCommand(new SavePostponedCashInCommand
                            {
                                TransactionHash = tx.Hash
                            }, "cashin");
                        }
                        else
                        {
                            var sum = (cashIn.Value * Math.Pow(10, -asset.MultiplierPower)).TruncateDecimalPlaces(asset.Accuracy);

                            var cmd = new RegisterCashInOutCommand
                            {
                                Transaction = new Transaction { Hash = tx.Hash, ClientId = tx.ClientId, Multisig = tx.Multisig, IsSegwit = tx.IsSegwit },
                                Asset = new Asset { Id = asset.Id, Accuracy = asset.Accuracy },
                                Amount = sum,
                                CommandId = Guid.NewGuid().ToString("N")
                            };

                            sender.SendCommand(cmd, "cashin");
                        }
                    }
                }
            }
        }

        private async Task<Service.Assets.Client.Models.Asset> GetAssetByBcnIdAsync(string bcnId)
        {
            return string.IsNullOrEmpty(bcnId)
                ? await _assetsService.TryGetAssetAsync(LykkeConstants.BitcoinAssetId)
                : (await _assetsService.GetAllAssetsAsync(true)).FirstOrDefault(x => x.BlockChainAssetId == bcnId);
        }
    }
}