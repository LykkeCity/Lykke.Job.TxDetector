using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class ObsoleteBlockchainTransactionsCacheItem : TableEntity, IObsoleteBlockchainTransaction
    {
        public static string GeneratePartitionKey(string partitionKey)
        {
            return partitionKey;
        }

        public static string GenerateRowKey(string txId)
        {
            return txId;
        }

        public string Address { get; set; }
        public string TxId { get; set; }
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public DateTime DateTime { get; set; }
        public int Height { get; set; }
        public string BlockId { get; set; }
        public int Confirmations { get; set; }

        public static ObsoleteBlockchainTransactionsCacheItem Create(IObsoleteBlockchainTransaction src)
        {
            return new ObsoleteBlockchainTransactionsCacheItem
            {
                PartitionKey = GeneratePartitionKey(src.Address),
                RowKey = GenerateRowKey(src.TxId),
                Address = src.Address,
                Amount = src.Amount,
                AssetId = src.AssetId,
                DateTime = src.DateTime,
                Height = src.Height,
                BlockId = src.BlockId,
                Confirmations = src.Confirmations,
                TxId = src.TxId
            };
        }

    }


    public class BlockchainTransactionsCache : IBlockchainTransactionsCache
    {

        private readonly INoSQLTableStorage<ObsoleteBlockchainTransactionsCacheItem> _tableStorage;

        public BlockchainTransactionsCache(INoSQLTableStorage<ObsoleteBlockchainTransactionsCacheItem> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IObsoleteBlockchainTransaction>> GetAllAsync(string address)
        {
            var partitionKey = ObsoleteBlockchainTransactionsCacheItem.GeneratePartitionKey(address);
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task RegisterAsync(IObsoleteBlockchainTransaction[] transactions)
        {

            foreach (var chunk in transactions.ToChunks(50))
            {
                var chunkArray = chunk.ToArray();

                var entities = chunkArray.Select(ObsoleteBlockchainTransactionsCacheItem.Create).ToArray();

                await _tableStorage.InsertOrReplaceBatchAsync(entities);
            }

        }

        public async Task<IObsoleteBlockchainTransaction> GetAsync(string address, string hash)
        {
            var paritionKey = ObsoleteBlockchainTransactionsCacheItem.GeneratePartitionKey(address);
            var rowkKey = ObsoleteBlockchainTransactionsCacheItem.GenerateRowKey(hash);

            return await _tableStorage.GetDataAsync(paritionKey, rowkKey);
        }

        public async Task<IObsoleteBlockchainTransaction> GetAsync(string[] addresses, string hash)
        {
            var tasks =
                addresses.Select(
                    x =>
                        _tableStorage.GetDataAsync(ObsoleteBlockchainTransactionsCacheItem.GeneratePartitionKey(x),
                            ObsoleteBlockchainTransactionsCacheItem.GenerateRowKey(hash)));

            var results = await Task.WhenAll(tasks);

            return results.FirstOrDefault(x => x != null);
        }
    }
}