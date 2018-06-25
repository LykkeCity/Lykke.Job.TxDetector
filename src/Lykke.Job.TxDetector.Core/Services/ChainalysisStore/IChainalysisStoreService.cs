using System;
using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Services.BitCoin;

namespace Lykke.Job.TxDetector.Core.Services.ChainalysisStore
{
    public interface IChainalysisStoreService
    {
        Task ProccedAsync(IBlockchainTransaction blockchainTransaction, string clientId, string walletAddress);
    }
}
