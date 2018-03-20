using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface IBcnCredentialsRecord
    {
        string Address { get; set; }
        string EncodedKey { get; set; }
        string PublicKey { get; set; }
        string AssetId { get; set; }
        string ClientId { get; set; }
        string AssetAddress { get; set; }
    }

    public interface IBcnClientCredentialsRepository
    {
        Task<IEnumerable<IBcnCredentialsRecord>> GetAllAsync(string assetId);
    }
}
