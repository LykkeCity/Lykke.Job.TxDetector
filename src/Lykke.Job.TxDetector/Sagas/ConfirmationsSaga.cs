using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.Clients;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Resources;
using Lykke.Job.TxDetector.Sagas.Commands;
using Lykke.Job.TxDetector.Sagas.Events;

namespace Lykke.Job.TxDetector.Sagas
{
    public class ConfirmationsSaga
    {
        private readonly AppSettings.TxDetectorSettings _settings;
        private readonly ISrvBlockchainReader _srvBlockchainReader;
        private readonly ILog _log;
        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly IClientAccountsRepository _clientAccountsRepository;

        public ConfirmationsSaga(
            ILog log,
            [NotNull] IClientSettingsRepository clientSettingsRepository,
            [NotNull] IClientAccountsRepository clientAccountsRepository, 
            AppSettings.TxDetectorSettings settings, 
            ISrvBlockchainReader srvBlockchainReader)
        {
            _log = log;
            _settings = settings;
            _srvBlockchainReader = srvBlockchainReader;
            _clientAccountsRepository = clientAccountsRepository ?? throw new ArgumentNullException(nameof(clientAccountsRepository));
            _clientSettingsRepository = clientSettingsRepository ?? throw new ArgumentNullException(nameof(clientSettingsRepository));
        }

        // entry point
        private async Task Handle(TransactionDetectedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(CashInOperationCreatedEvent), evt.ToJson());

            var confirmations = await _srvBlockchainReader.GetConfirmationsCount(evt.TransactionHash);

            if (confirmations >= _settings.TxDetectorConfirmationsLimit)
            {
                var cmd = new ProcessTransactionCommand
                {
                    TransactionHash = evt.TransactionHash
                };
                sender.SendCommand(cmd, "confirmations");
            }
            else
            {
                throw new Exception();
                //put back if not confirmed yet
                //context.MoveMessageToEnd(message);
                //context.SetCountQueueBasedDelay(500, 100);
            }

        }

        private async Task Handle(CashInOperationCreatedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(CashInOperationCreatedEvent), evt.ToJson());

            var cmd = new ProcessCashInCommand
            {
                Transaction = evt.Transaction,
                Asset = evt.Asset,
                Amount = evt.Amount
            };

            sender.SendCommand(cmd, "cachein");
        }

        private async Task Handle(TransferOperationCreatedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(TransferOperationCreatedEvent), evt.ToJson());

            var cmd = new HandleTransferCommand
            {
                TransferId = evt.TransferId,
                ClientId = evt.ClientId
            };

            sender.SendCommand(cmd, "transfer");
        }

        private async Task Handle(TransactionConfirmedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(ConfirmationsSaga), nameof(TransferOperationCreatedEvent), evt.ToJson());

            var clientAcc = await _clientAccountsRepository.GetByIdAsync(evt.Transaction.ClientId);

            var cmd = new SendNoRefundDepositDoneMailCommand
            {
                Email = clientAcc.Email,
                Amount = evt.Amount,
                AssetBcnId = evt.Asset.Id
            };
            sender.SendCommand(cmd, "transfer");

            var pushSettings = await _clientSettingsRepository.GetSettings<PushNotificationsSettings>(evt.Transaction.ClientId);
            if (pushSettings.Enabled)
            {
                var cmd2 = new SendNotificationCommand
                {
                    NotificationsIds = new[] { clientAcc.NotificationsId },
                    Type = NotificationType.TransactionConfirmed,
                    Message = string.Format(TextResources.CashInSuccessText, evt.Amount.GetFixedAsString(evt.Asset.Accuracy), evt.Asset.Id)
                };
                sender.SendCommand(cmd2, "transfer");
            }
        }
    }
}