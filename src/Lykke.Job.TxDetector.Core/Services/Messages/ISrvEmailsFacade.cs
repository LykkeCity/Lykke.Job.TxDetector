using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Services.Messages
{
    public interface ISrvEmailsFacade
    {
        Task SendNoRefundDepositDoneMail(string email, double amount, string assetBcnId);
    }
}