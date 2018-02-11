using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface ILastProcessedBlockRepository
    {
        Task<int?> GetLastProcessedBlockHeightAsync();
        Task UpdateLastProcessedBlockHeightAsync(int currentBlock);
        Task<int> GetMinBlockHeight();
    }
}