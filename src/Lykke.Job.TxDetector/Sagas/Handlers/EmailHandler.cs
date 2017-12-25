using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;
using Lykke.Job.TxDetector.Sagas.Commands;

namespace Lykke.Job.TxDetector.Sagas.Handlers
{
    public class EmailHandler
    {
        private readonly IEmailSender _emailSender;

        public EmailHandler(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Handle(SendNoRefundDepositDoneMailCommand command)
        {
            var msgData = new NoRefundDepositDoneData
            {
                Amount = command.Amount,
                AssetBcnId = command.AssetBcnId
            };
            await _emailSender.SendEmailAsync(command.Email, msgData);
        }
    }
}