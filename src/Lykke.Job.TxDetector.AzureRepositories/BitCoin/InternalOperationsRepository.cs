using System;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class InternalOperationEntity : TableEntity, IInternalOperation
    {
        public static string GeneratePartitionKey(string hash)
        {
            return hash;
        }
        
        public Guid TransactionId => Guid.Parse(RowKey);
        public string Hash => PartitionKey;
        public string CommandType { get; set; }

        public string OperationIdsVal { get; set; }
        public string[] OperationIds
        {
            get { return OperationIdsVal.DeserializeJson<string[]>(); }
            set { OperationIdsVal = value.ToJson(); }
        }
    }

    public class InternalOperationsRepository : IInternalOperationsRepository
    {
        private readonly INoSQLTableStorage<InternalOperationEntity> _tableStorage;

        public InternalOperationsRepository(INoSQLTableStorage<InternalOperationEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IInternalOperation> GetAsync(string hash)
        {
            return (await _tableStorage.GetDataAsync(InternalOperationEntity.GeneratePartitionKey(hash))).FirstOrDefault();
        }
    }
}