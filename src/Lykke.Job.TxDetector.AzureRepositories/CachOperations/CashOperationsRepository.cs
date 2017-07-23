using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Lykke.Job.TxDetector.Core.Domain.CashOperations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.CachOperations
{
    public class CashInOutOperationEntity : TableEntity, ICashInOutOperation
    {
        public string Id => RowKey;
        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public string BlockChainHash { get; set; }
        public string Multisig { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool? IsSettled { get; set; }

        public string StateField { get; set; }
        public TransactionStates State
        {
            get
            {
                TransactionStates type = TransactionStates.InProcessOnchain;
                if (!string.IsNullOrEmpty(StateField))
                {
                    Enum.TryParse(StateField, out type);
                }
                return type;
            }
            set { StateField = value.ToString(); }
        }

        public bool IsRefund { get; set; }

        public string TypeField { get; set; }
        public CashOperationType Type
        {
            get
            {
                CashOperationType type = CashOperationType.None;
                if (!string.IsNullOrEmpty(TypeField))
                {
                    Enum.TryParse(TypeField, out type);
                }
                return type;
            }
            set { TypeField = value.ToString(); }
        }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            internal static string GenerateRowKey(string id)
            {
                return id;
            }

            public static CashInOutOperationEntity Create(ICashInOutOperation src)
            {
                return new CashInOutOperationEntity
                {
                    PartitionKey = GeneratePartitionKey(src.ClientId),
                    RowKey = GenerateRowKey(src.Id),
                    DateTime = src.DateTime,
                    AssetId = src.AssetId,
                    Amount = src.Amount,
                    BlockChainHash = src.BlockChainHash,
                    IsHidden = src.IsHidden,
                    IsRefund = src.IsRefund,
                    AddressFrom = src.AddressFrom,
                    AddressTo = src.AddressTo,
                    Multisig = src.Multisig,
                    ClientId = src.ClientId,
                    IsSettled = src.IsSettled,
                    Type = src.Type,
                    TransactionId = src.TransactionId
                };
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey(string multisig)
            {
                return multisig;
            }

            internal static string GenerateRowKey(string id)
            {
                return id;
            }

            public static CashInOutOperationEntity Create(ICashInOutOperation src)
            {
                return new CashInOutOperationEntity
                {
                    PartitionKey = GeneratePartitionKey(src.Multisig),
                    RowKey = GenerateRowKey(src.Id),
                    DateTime = src.DateTime,
                    AssetId = src.AssetId,
                    Amount = src.Amount,
                    BlockChainHash = src.BlockChainHash,
                    IsHidden = src.IsHidden,
                    IsRefund = src.IsRefund,
                    AddressFrom = src.AddressFrom,
                    AddressTo = src.AddressTo,
                    Multisig = src.Multisig,
                    ClientId = src.ClientId,
                    IsSettled = src.IsSettled,
                    Type = src.Type,
                    State = src.State,
                    TransactionId = src.TransactionId
                };
            }
        }
    }

    public class CashOperationsRepository : ICashOperationsRepository
    {
        private readonly INoSQLTableStorage<CashInOutOperationEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureIndex> _blockChainHashIndices;

        public CashOperationsRepository(INoSQLTableStorage<CashInOutOperationEntity> tableStorage, INoSQLTableStorage<AzureIndex> blockChainHashIndices)
        {
            _tableStorage = tableStorage;
            _blockChainHashIndices = blockChainHashIndices;
        }

        public async Task<string> RegisterAsync(ICashInOutOperation operation)
        {
            var newItem = CashInOutOperationEntity.ByClientId.Create(operation);
            var byMultisig = CashInOutOperationEntity.ByMultisig.Create(operation);
            await _tableStorage.InsertAsync(newItem);
            await _tableStorage.InsertAsync(byMultisig);

            if (!string.IsNullOrEmpty(operation.BlockChainHash))
            {
                var indexEntity = AzureIndex.Create(operation.BlockChainHash, newItem.Id, newItem);
                await _blockChainHashIndices.InsertAsync(indexEntity);
            }

            return newItem.Id;
        }
    }
}