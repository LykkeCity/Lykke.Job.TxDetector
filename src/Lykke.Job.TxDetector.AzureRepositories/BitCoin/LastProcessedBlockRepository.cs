using System.Linq;
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

        public static string GenerateRowKey()
        {
            return "Block";
        }

        public static LastProcessedBlockEntity Create(int blockHeight)
        {
            return new LastProcessedBlockEntity
            {
                BlockHeight = blockHeight,
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey()
            };
        }

        public int BlockHeight { get; set; }
    }

    public class LastProcessedBlockRepository : ILastProcessedBlockRepository
    {
        private readonly INoSQLTableStorage<LastProcessedBlockEntity> _tableStorage;

        public LastProcessedBlockRepository(INoSQLTableStorage<LastProcessedBlockEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<int?> GetLastProcessedBlockHeightAsync()
        {
            return (await _tableStorage.GetDataAsync(LastProcessedBlockEntity.GeneratePartitionKey(), LastProcessedBlockEntity.GenerateRowKey()))?.BlockHeight;
        }

        public Task UpdateLastProcessedBlockHeightAsync(int currentBlock)
        {
            return _tableStorage.InsertOrReplaceAsync(LastProcessedBlockEntity.Create(currentBlock));
        }

        public async Task<int> GetMinBlockHeight()
        {
            var records = await _tableStorage.GetDataAsync(LastProcessedBlockEntity.GeneratePartitionKey());
            return records.DefaultIfEmpty().Min(x => x?.BlockHeight ?? 0);
        }

    }
}