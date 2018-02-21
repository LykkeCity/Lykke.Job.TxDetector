﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class BcnCredentialsRecordEntity : TableEntity, IBcnCredentialsRecord
    {
        public static class ByClientId
        {
            public static string GeneratePartition(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string assetId)
            {
                return assetId;
            }

            public static BcnCredentialsRecordEntity Create(IBcnCredentialsRecord record)
            {
                return new BcnCredentialsRecordEntity
                {
                    Address = record.Address,
                    AssetAddress = record.AssetAddress,
                    AssetId = record.AssetId,
                    ClientId = record.ClientId,
                    EncodedKey = record.EncodedKey,
                    PublicKey = record.PublicKey,
                    PartitionKey = GeneratePartition(record.ClientId),
                    RowKey = GenerateRowKey(record.AssetId)
                };
            }
        }


        public static class ByAssetAddress
        {
            public static string GeneratePartition()
            {
                return "ByAssetAddress";
            }

            public static string GenerateRowKey(string assetAddress)
            {
                return assetAddress;
            }

            public static BcnCredentialsRecordEntity Create(IBcnCredentialsRecord record)
            {
                return new BcnCredentialsRecordEntity
                {
                    Address = record.Address,
                    AssetAddress = record.AssetAddress,
                    AssetId = record.AssetId,
                    ClientId = record.ClientId,
                    EncodedKey = record.EncodedKey,
                    PublicKey = record.PublicKey,
                    PartitionKey = GeneratePartition(),
                    RowKey = GenerateRowKey(record.AssetAddress)
                };
            }
        }


        public string Address { get; set; }
        public string EncodedKey { get; set; }
        public string PublicKey { get; set; }
        public string ClientId { get; set; }
        public string AssetAddress { get; set; }
        public string AssetId { get; set; }
    }

    public class BcnClientCredentialsRepository : IBcnClientCredentialsRepository
    {
        private readonly INoSQLTableStorage<BcnCredentialsRecordEntity> _tableStorage;

        public BcnClientCredentialsRepository(INoSQLTableStorage<BcnCredentialsRecordEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task SaveAsync(IBcnCredentialsRecord credsRecord)
        {
            var byClientEntity = BcnCredentialsRecordEntity.ByClientId.Create(credsRecord);
            var byAssetAddressEntity = BcnCredentialsRecordEntity.ByAssetAddress.Create(credsRecord);

            await _tableStorage.InsertAsync(byClientEntity);
            await _tableStorage.InsertAsync(byAssetAddressEntity);
        }

        public async Task<IBcnCredentialsRecord> GetAsync(string clientId, string assetId)
        {
            return await _tableStorage.GetDataAsync(BcnCredentialsRecordEntity.ByClientId.GeneratePartition(clientId),
                BcnCredentialsRecordEntity.ByClientId.GenerateRowKey(assetId));
        }

        public async Task<IEnumerable<IBcnCredentialsRecord>> GetAsync(string clientId)
        {
            return await _tableStorage.GetDataAsync(BcnCredentialsRecordEntity.ByClientId.GeneratePartition(clientId));
        }

        public async Task<string> GetClientAddress(string clientId)
        {
            return (await _tableStorage.GetTopRecordAsync(BcnCredentialsRecordEntity.ByClientId.GeneratePartition(clientId))).Address;
        }

        public async Task<IEnumerable<IBcnCredentialsRecord>> GetAllAsync(string assetId)
        {
            var filter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                    BcnCredentialsRecordEntity.ByAssetAddress.GeneratePartition()),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("AssetId", QueryComparisons.Equal, assetId)
            );

            var query = new TableQuery<BcnCredentialsRecordEntity>().Where(filter);

            return await _tableStorage.WhereAsync(query);
        }

        public async Task<IBcnCredentialsRecord> GetByAssetAddressAsync(string assetAddress)
        {
            return await _tableStorage.GetDataAsync(BcnCredentialsRecordEntity.ByAssetAddress.GeneratePartition(),
                BcnCredentialsRecordEntity.ByAssetAddress.GenerateRowKey(assetAddress));
        }
    }
}
