using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class ConfirmedTransactionRecord : TableEntity
    {
        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static string GenerateRowKey(string hash)
        {
            return hash;
        }

        public static ConfirmedTransactionRecord Create(string hash, string clientId)
        {
            return new ConfirmedTransactionRecord
            {
                PartitionKey = GeneratePartitionKey(clientId),
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

        public async Task<bool> SaveConfirmedIfNotExist(string hash, string clientId)
        {
            // processed in old version (can be removed later)
            const string oldParitionKey = "T";
            if (await _tableStorage.GetDataAsync(oldParitionKey, hash) != null)
                return false;

            return await _tableStorage.CreateIfNotExistsAsync(ConfirmedTransactionRecord.Create(hash, clientId));
        }
    }
}