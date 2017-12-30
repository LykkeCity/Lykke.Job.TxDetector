using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class BalanceChangeTransactionEntity : TableEntity, IBalanceChangeTransaction
    {
        public static string GeneratePartition(string hash)
        {
            return hash;
        }

        public static string GenerateRowKey(string clientId)
        {
            return clientId;
        }

        public static BalanceChangeTransactionEntity Create(IBalanceChangeTransaction balanceChangeTransaction)
        {
            return new BalanceChangeTransactionEntity
            {
                ClientId = balanceChangeTransaction.ClientId,
                Confirmations = balanceChangeTransaction.Confirmations,
                Hash = balanceChangeTransaction.Hash,
                Multisig = balanceChangeTransaction.Multisig,
                PartitionKey = GeneratePartition(balanceChangeTransaction.Hash),
                RowKey = GenerateRowKey(balanceChangeTransaction.ClientId),
                ReceivedCoins = balanceChangeTransaction.ReceivedCoins,
                SpentCoins = balanceChangeTransaction.SpentCoins,
                BlockId = balanceChangeTransaction.BlockId,
                Height = balanceChangeTransaction.Height,
                DetectDt = balanceChangeTransaction.DetectDt,
                IsSegwit = balanceChangeTransaction.IsSegwit
            };
        }

        public string Hash { get; set; }
        public int Confirmations { get; set; }
        public string ClientId { get; set; }
        public string Multisig { get; set; }
        public bool IsSegwit { get; set; }
        public DateTime DetectDt { get; set; }

        public InputOutput[] ReceivedCoins
        {
            get { return ReceivedCoinsJson.DeserializeJson<InputOutput[]>(); }
            set { ReceivedCoinsJson = value.ToJson(); }
        }
        public string ReceivedCoinsJson { get; set; }

        public InputOutput[] SpentCoins
        {
            get { return SpentCoinsJson.DeserializeJson<InputOutput[]>(); }
            set { SpentCoinsJson = value.ToJson(); }
        }

        public string BlockId { get; set; }
        public int Height { get; set; }
        public string SpentCoinsJson { get; set; }
    }

    public class BalanceChangeTransactionsRepository : IBalanceChangeTransactionsRepository
    {
        private readonly INoSQLTableStorage<BalanceChangeTransactionEntity> _tableStorage;

        public BalanceChangeTransactionsRepository(INoSQLTableStorage<BalanceChangeTransactionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<bool> InsertIfNotExistsAsync(IBalanceChangeTransaction balanceChangeTransaction)
        {
            var entity = BalanceChangeTransactionEntity.Create(balanceChangeTransaction);
            return await _tableStorage.CreateIfNotExistsAsync(entity);
        }

        public async Task<IEnumerable<IBalanceChangeTransaction>> GetAsync(string hash)
        {
            return await _tableStorage.GetDataAsync(BalanceChangeTransactionEntity.GeneratePartition(hash));
        }
    }
}