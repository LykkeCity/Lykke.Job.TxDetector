using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class PostponedCashInRecord : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "PC";
        }

        public static string GenerateRowKey(string hash)
        {
            return hash;
        }

        public static PostponedCashInRecord Create(string hash)
        {
            return new PostponedCashInRecord
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(hash)
            };
        }
    }

    public class PostponedCashInRepository : IPostponedCashInRepository
    {
        private readonly INoSQLTableStorage<PostponedCashInRecord> _tableStorage;

        public PostponedCashInRepository(INoSQLTableStorage<PostponedCashInRecord> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task SaveAsync(string hash)
        {
            return _tableStorage.InsertAsync(PostponedCashInRecord.Create(hash));
        }
    }
}