using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface IObsoleteBlockchainTransaction
    {
        string Address { get; }
        string TxId { get; }
        string AssetId { get; }
        DateTime DateTime { get; }
        double Amount { get; }
        int Height { get; }
        string BlockId { get; }
        int Confirmations { get; }
    }

    public class ObsoleteBlockchainTransaction : IObsoleteBlockchainTransaction
    {
        public string Address { get; set; }
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public DateTime DateTime { get; set; }
        public int Height { get; set; }
        public string TxId { get; set; }
        public string BlockId { get; set; }
        public int Confirmations { get; set; }
    }


    public interface IBlockchainTransactionsCache
    {
        Task<IEnumerable<IObsoleteBlockchainTransaction>> GetAllAsync(string address);
        Task RegisterAsync(IObsoleteBlockchainTransaction[] transactions);
        Task<IObsoleteBlockchainTransaction> GetAsync(string address, string hash);
        Task<IObsoleteBlockchainTransaction> GetAsync(string[] addresses, string hash);
    }

    public static class BlockchainTransactionExt
    {
        public static string GetBriefTxInfo(this IObsoleteBlockchainTransaction item)
        {
            return $"DT: {item.DateTime}, Hash: {item.TxId}, Amount: {item.Amount}, Confirms: {item.Confirmations} ";
        }

        public static string GetBriefTxInfo(this IObsoleteBlockchainTransaction[] items)
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                $"New transactions found. Count: {items.Length}");

            foreach (var item in items)
            {
                sb.AppendLine(GetBriefTxInfo(item));
            }

            return sb.ToString();
        }
    }
}