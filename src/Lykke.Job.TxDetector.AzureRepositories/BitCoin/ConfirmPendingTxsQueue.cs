using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class ConfirmPendingTxsQueue : IConfirmPendingTxsQueue
    {
        private readonly IQueueExt _queueExt;

        public ConfirmPendingTxsQueue(IQueueExt queueExt)
        {
            _queueExt = queueExt;
        }

        public Task PutAsync(PendingTxMsg msg)
        {
            return _queueExt.PutRawMessageAsync(msg.ToJson());
        }
    }
}