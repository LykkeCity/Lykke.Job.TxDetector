using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface IPostponedCashInRepository
    {
        Task SaveAsync(string hash);
    }
}
