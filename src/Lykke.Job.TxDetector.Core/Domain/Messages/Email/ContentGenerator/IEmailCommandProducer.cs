using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator
{
    public interface IEmailCommandProducer
    {
        Task ProduceSendEmailCommand<T>(string mailAddress, T msgData);
    }
}