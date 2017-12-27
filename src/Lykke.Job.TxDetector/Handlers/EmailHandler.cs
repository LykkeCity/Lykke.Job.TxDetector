using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;
using Lykke.Job.TxDetector.Sagas;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Handlers
{
    public class EmailHandler
    {
        private readonly ILog _log;
        private readonly IEmailSender _emailSender;

        public EmailHandler([NotNull] ILog log, [NotNull] IEmailSender emailSender)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public async Task Handle(SendNoRefundDepositDoneMailCommand command)
        {
            await _log.WriteInfoAsync(nameof(NotificationsHandler), nameof(SendNoRefundDepositDoneMailCommand), command.ToJson(), "");

            ChaosKitty.Meow();

            var content = new NoRefundDepositDoneData
            {
                Amount = command.Amount,
                AssetBcnId = command.AssetId
            };
            await _emailSender.SendEmailAsync(command.Email, content);
        }
    }
}