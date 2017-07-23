using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class ConfirmedTransactionRecord : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "T";
        }

        public static string GenerateRowKey(string hash)
        {
            return hash;
        }

        public static ConfirmedTransactionRecord Create(string hash)
        {
            return new ConfirmedTransactionRecord
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(hash)
            };
        }
    }

    public class ConfirmedTransactionsRepository : IConfirmedTransactionsRepository
    {
        private readonly INoSQLTableStorage<ConfirmedTransactionRecord> _tableStorage;

        public ConfirmedTransactionsRepository(INoSQLTableStorage<ConfirmedTransactionRecord> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task<bool> SaveConfirmedIfNotExist(string hash)
        {
            return _tableStorage.CreateIfNotExistsAsync(ConfirmedTransactionRecord.Create(hash));
        }
    }
}