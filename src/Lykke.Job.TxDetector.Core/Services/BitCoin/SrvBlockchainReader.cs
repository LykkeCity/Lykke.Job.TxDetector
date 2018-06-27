using System.Collections.Generic;
using System.Linq;

namespace Lykke.Job.TxDetector.Core.Services.BitCoin
{
    public interface IBlockchainTransaction
    {
        string Hash { get; set; }
        int Confirmations { get; set; }
        InputOutput[] ReceivedCoins { get; set; }
        InputOutput[] SpentCoins { get; set; }
        string BlockId { get; set; }
        int Height { get; set; }
    }

    public class InputOutput
    {
        public string BcnAssetId { get; set; }
        public double Amount { get; set; }
        public string Address { get; set; }
    }

    public class BlockchainTransaction : IBlockchainTransaction
    {
        public string Hash { get; set; }
        public int Confirmations { get; set; }
        public InputOutput[] ReceivedCoins { get; set; }
        public InputOutput[] SpentCoins { get; set; }
        public string BlockId { get; set; }
        public int Height { get; set; }
    }

    public static class SrvBlockchainReaderExt
    {
        public static bool IsCashIn(this IBlockchainTransaction tx, string address)
        {
            return (tx.SpentCoins == null || tx.SpentCoins.All(x => x.Address != address)) &&
                   tx.ReceivedCoins != null && tx.ReceivedCoins.Any(x => x.Address == address);
        }

        public static Dictionary<string, double> GetOperationSummary(this IBlockchainTransaction tx, string address)
        {
            var received =
                tx.ReceivedCoins.Where(x => x.Address == address);

            var spent =
                tx.SpentCoins.Where(x => x.Address == address);

            var res = new Dictionary<string, double>();

            foreach (var input in received)
            {
                var assetBcnId = input.BcnAssetId ?? string.Empty;
                if (res.ContainsKey(assetBcnId))
                    res[assetBcnId] += input.Amount;
                else
                {
                    res.Add(assetBcnId, input.Amount);
                }
            }

            foreach (var input in spent)
            {
                var assetBcnId = input.BcnAssetId ?? string.Empty;
                if (res.ContainsKey(assetBcnId))
                    res[assetBcnId] -= input.Amount;
                else
                {
                    res.Add(assetBcnId, -input.Amount);
                }
            }

            return res;
        }
    }
}