using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface IBitcoinCashin
    {
        string Id { get; }
        string ClientId { get; set; }
        string Address { get; set; }
        string TxHash { get; set; }
        bool IsSegwit { get; set; }
    }

    public interface IBitcoinCashinRepository
    {
        Task InsertOrReplaceAsync(string id, string clientId, string address, string hash, bool isSegwit);
    }
}
