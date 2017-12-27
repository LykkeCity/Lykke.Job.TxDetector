using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Domain.Clients;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Resources;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Sagas
{
    public class ConfirmationsSaga
    {
        private readonly ILog _log;
        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly IClientAccountsRepository _clientAccountsRepository;

        public ConfirmationsSaga(
            [NotNull] ILog log,
            [NotNull] IClientSettingsRepository clientSettingsRepository,
            [NotNull] IClientAccountsRepository clientAccountsRepository)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _clientAccountsRepository = clientAccountsRepository ?? throw new ArgumentNullException(nameof(clientAccountsRepository));
            _clientSettingsRepository = clientSettingsRepository ?? throw new ArgumentNullException(nameof(clientSettingsRepository));
        }

        private async Task Handle(CashInOperationCreatedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(CashInOperationCreatedEvent), evt.ToJson());

            ChaosKitty.Meow();

            var cmd = new RegisterCashInOutCommand
            {
                Transaction = evt.Transaction,
                Asset = evt.Asset,
                Amount = evt.Amount,
                CommandId = Guid.NewGuid().ToString("N")
            };

            sender.SendCommand(cmd, "cashin");
        }

        private async Task Handle(CashInOutOperationRegisteredEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(CashInOutOperationRegisteredEvent), evt.ToJson());

            ChaosKitty.Meow();

            var cmd = new ProcessCashInCommand
            {
                Transaction = evt.Transaction,
                Asset = evt.Asset,
                Amount = evt.Amount,
                CommandId = evt.CommandId
            };

            sender.SendCommand(cmd, "cashin");
        }

        private async Task Handle(TransferOperationCreatedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(TransferOperationCreatedEvent), evt.ToJson());

            ChaosKitty.Meow();

            var cmd = new ProcessTransferCommand
            {
                TransferId = evt.TransferId
            };

            sender.SendCommand(cmd, "transfer");
        }

        private async Task Handle(TransactionProcessedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(TransactionProcessedEvent), evt.ToJson());

            ChaosKitty.Meow();

            var clientAcc = await _clientAccountsRepository.GetByIdAsync(evt.ClientId);

            var sendEmailCommand = new SendNoRefundDepositDoneMailCommand
            {
                Email = clientAcc.Email,
                Amount = evt.Amount,
                AssetId = evt.Asset.Id
            };
            sender.SendCommand(sendEmailCommand, "email");

            ChaosKitty.Meow();

            var pushSettings = await _clientSettingsRepository.GetSettings<PushNotificationsSettings>(evt.ClientId);
            if (pushSettings.Enabled)
            {
                var sendNotificationCommand = new SendNotificationCommand
                {
                    NotificationId = clientAcc.NotificationsId,
                    Type = NotificationType.TransactionConfirmed,
                    Message = string.Format(TextResources.CashInSuccessText, evt.Amount.GetFixedAsString(evt.Asset.Accuracy), evt.Asset.Id)
                };
                sender.SendCommand(sendNotificationCommand, "notifications");
            }
        }
    }
}