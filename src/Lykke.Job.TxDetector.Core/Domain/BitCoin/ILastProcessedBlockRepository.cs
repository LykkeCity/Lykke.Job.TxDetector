using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface ILastProcessedBlockRepository
    {
        Task InsertOrUpdateForClientAsync(string clientId, int blockHeight);
        Task<int> GetLastProcessedBlockHeightAsync(string clientId);
    }
}