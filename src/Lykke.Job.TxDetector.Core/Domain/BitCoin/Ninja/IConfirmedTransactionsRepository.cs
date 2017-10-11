using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja
{
    public interface IConfirmedTransactionsRepository
    {
        Task<bool> SaveConfirmedIfNotExist(string hash, string clientId);
    }
}