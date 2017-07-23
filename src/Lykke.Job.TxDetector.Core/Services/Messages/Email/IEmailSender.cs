using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;

namespace Lykke.Job.TxDetector.Core.Services.Messages.Email
{
    public interface IEmailSender
    {
        Task SendEmailAsync<T>(string emailAddress, T messageData) where T : IEmailMessageData;
    }
}