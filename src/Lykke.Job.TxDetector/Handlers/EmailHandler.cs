using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Handlers
{
    public class EmailHandler
    {
        private readonly IEmailSender _emailSender;

        public EmailHandler([NotNull] IEmailSender emailSender)
        {
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        public async Task<CommandHandlingResult> Handle(SendNoRefundDepositDoneMailCommand command)
        {
            ChaosKitty.Meow();

            var content = new NoRefundDepositDoneData
            {
                Amount = command.Amount,
                AssetBcnId = command.AssetId
            };
            await _emailSender.SendEmailAsync(command.Email, content);

            return CommandHandlingResult.Ok();
        }
    }
}