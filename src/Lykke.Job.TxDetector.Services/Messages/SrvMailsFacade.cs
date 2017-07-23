using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator.MessagesData;
using Lykke.Job.TxDetector.Core.Services.Messages;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;

namespace Lykke.Job.TxDetector.Services.Messages
{
    public class SrvEmailsFacade : ISrvEmailsFacade
    {
        private readonly IEmailSender _emailSender;

        public SrvEmailsFacade(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendNoRefundDepositDoneMail(string email, double amount, string assetBcnId)
        {
            var msgData = new NoRefundDepositDoneData
            {
                Amount = amount,
                AssetBcnId = assetBcnId
            };
            await _emailSender.SendEmailAsync(email, msgData);
        }
    }
}