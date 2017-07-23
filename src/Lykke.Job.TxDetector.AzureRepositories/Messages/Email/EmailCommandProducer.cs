using System.Threading.Tasks;
using AzureStorage.Queue;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;

namespace Lykke.Job.TxDetector.AzureRepositories.Messages.Email
{
    public class EmailCommandProducer : IEmailCommandProducer
    {
        private readonly IQueueExt _queueExt;

        public EmailCommandProducer(IQueueExt queueExt)
        {
            _queueExt = queueExt;

            _queueExt.RegisterTypes(
                QueueType.Create(new NoRefundDepositDoneData().MessageId(), typeof(QueueRequestModel<SendEmailData<NoRefundDepositDoneData>>))
            );
        }

        public Task ProduceSendEmailCommand<T>(string mailAddress, T msgData)
        {
            var data = SendEmailData<T>.Create(mailAddress, msgData);
            var msg = new QueueRequestModel<SendEmailData<T>> { Data = data };
            return _queueExt.PutMessageAsync(msg);
        }
    }
}