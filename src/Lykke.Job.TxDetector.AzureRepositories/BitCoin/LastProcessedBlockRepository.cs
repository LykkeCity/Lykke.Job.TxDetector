using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class LastProcessedBlockEntity : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "LastProcessed";
        }

        public static string GenerateRowKey(string clientId)
        {
            return clientId;
        }

        public static LastProcessedBlockEntity Create(string clientId, int blockHeight)
        {
            return new LastProcessedBlockEntity
            {
                BlockHeight = blockHeight,
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(clientId)
            };
        }

        public string ClientId => RowKey;
        public int BlockHeight { get; set; }
    }

    public class LastProcessedBlockRepository : ILastProcessedBlockRepository
    {
        private readonly INoSQLTableStorage<LastProcessedBlockEntity> _tableStorage;

        public LastProcessedBlockRepository(INoSQLTableStorage<LastProcessedBlockEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task InsertOrUpdateForClientAsync(string clientId, int blockHeight)
        {
            var entity = LastProcessedBlockEntity.Create(clientId, blockHeight);
            await _tableStorage.InsertOrReplaceAsync(entity);
        }

        public async Task<int> GetLastProcessedBlockHeightAsync(string clientId)
        {
            return
            (await _tableStorage.GetDataAsync(LastProcessedBlockEntity.GeneratePartitionKey(),
                LastProcessedBlockEntity.GenerateRowKey(clientId)))?.BlockHeight ?? 0;
        }
    }
}