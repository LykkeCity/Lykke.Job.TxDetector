using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Services.BitCoin;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface IBalanceChangeTransaction : IBlockchainTransaction
    {
        string ClientId { get; set; }
        string Multisig { get; set; }
        bool IsSegwit { get; set; }
        DateTime DetectDt { get; }
    }

    public class BalanceChangeTransaction : IBalanceChangeTransaction
    {
        public static IBalanceChangeTransaction Create(IBlockchainTransaction blockchainTx, string clientId, string multisig, bool isSegwit)
        {
            return new BalanceChangeTransaction
            {
                ClientId = clientId,
                Confirmations = blockchainTx.Confirmations,
                Hash = blockchainTx.Hash,
                ReceivedCoins = blockchainTx.ReceivedCoins,
                SpentCoins = blockchainTx.SpentCoins,
                Multisig = multisig,
                BlockId = blockchainTx.BlockId,
                Height = blockchainTx.Height,
                DetectDt = DateTime.UtcNow,
                IsSegwit = isSegwit
            };
        }

        public string Hash { get; set; }
        public int Confirmations { get; set; }
        public InputOutput[] ReceivedCoins { get; set; }
        public InputOutput[] SpentCoins { get; set; }
        public string BlockId { get; set; }
        public int Height { get; set; }
        public string ClientId { get; set; }
        public string Multisig { get; set; }
        public bool IsSegwit { get; set; }
        public DateTime DetectDt { get; set; }
    }

    public interface IBalanceChangeTransactionsRepository
    {
        Task<bool> InsertIfNotExistsAsync(IBalanceChangeTransaction balanceChangeTransaction);
        Task<IEnumerable<IBalanceChangeTransaction>> GetAsync(string hash);
    }
}