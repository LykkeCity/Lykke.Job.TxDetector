using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public class PendingTxMsg
    {
        public string Hash { get; set; }
    }

    public interface IConfirmPendingTxsQueue
    {
        Task PutAsync(PendingTxMsg msg);
    }
}