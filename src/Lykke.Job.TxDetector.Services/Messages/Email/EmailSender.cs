using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;

namespace Lykke.Job.TxDetector.Services.Messages.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly IEmailCommandProducer _emailCommandProducer;

        public EmailSender(IEmailCommandProducer emailCommandProducer)
        {
            _emailCommandProducer = emailCommandProducer;
        }

        public async Task SendEmailAsync<T>(string email, T msgData) where T : IEmailMessageData
        {
            await _emailCommandProducer.ProduceSendEmailCommand(email, msgData);
        }
    }
}