using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.BitCoin
{
    public class WalletCredentialsEntity : TableEntity, IWalletCredentials
    {
        public static class ByClientId
        {
            public static string GeneratePartitionKey()
            {
                return "Wallet";
            }

            public static string GenerateRowKey(string clientId)
            {
                return clientId;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ClientId);
                return entity;
            }
        }

        public static class ByColoredMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "WalletColoredMultisig";
            }

            public static string GenerateRowKey(string coloredMultisig)
            {
                return coloredMultisig;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ColoredMultiSig);
                return entity;
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "WalletMultisig";
            }

            public static string GenerateRowKey(string multisig)
            {
                return multisig;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.MultiSig);
                return entity;
            }
        }

        public static class ByEthContract
        {
            public static string GeneratePartitionKey()
            {
                return "EthConversionWallet";
            }

            public static string GenerateRowKey(string ethWallet)
            {
                return ethWallet;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.EthConversionWalletAddress);
                return entity;
            }
        }

        public static class BySolarCoinWallet
        {
            public static string GeneratePartitionKey()
            {
                return "SolarCoinWallet";
            }

            public static string GenerateRowKey(string address)
            {
                return address;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.SolarCoinWalletAddress);
                return entity;
            }
        }

        public static class ByChronoBankContract
        {
            public static string GeneratePartitionKey()
            {
                return "ChronoBankContract";
            }

            public static string GenerateRowKey(string contract)
            {
                return contract;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.ChronoBankContract);
                return entity;
            }
        }

        public static class ByQuantaContract
        {
            public static string GeneratePartitionKey()
            {
                return "QuantaContract";
            }

            public static string GenerateRowKey(string contract)
            {
                return contract;
            }

            public static WalletCredentialsEntity CreateNew(IWalletCredentials src)
            {
                var entity = Create(src);
                entity.PartitionKey = GeneratePartitionKey();
                entity.RowKey = GenerateRowKey(src.QuantaContract);
                return entity;
            }
        }

        public static WalletCredentialsEntity Create(IWalletCredentials src)
        {
            return new WalletCredentialsEntity
            {
                ClientId = src.ClientId,
                PrivateKey = src.PrivateKey,
                Address = src.Address,
                MultiSig = src.MultiSig,
                ColoredMultiSig = src.ColoredMultiSig,
                PreventTxDetection = src.PreventTxDetection,
                EncodedPrivateKey = src.EncodedPrivateKey,
                PublicKey = src.PublicKey,
                BtcConvertionWalletPrivateKey = src.BtcConvertionWalletPrivateKey,
                BtcConvertionWalletAddress = src.BtcConvertionWalletAddress,
                EthConversionWalletAddress = src.EthConversionWalletAddress,
                EthAddress = src.EthAddress,
                EthPublicKey = src.EthPublicKey,
                SolarCoinWalletAddress = src.SolarCoinWalletAddress,
                ChronoBankContract = src.ChronoBankContract,
                QuantaContract = src.QuantaContract
            };
        }

        public static void Update(WalletCredentialsEntity src, IWalletCredentials changed)
        {
            src.ClientId = changed.ClientId;
            src.PrivateKey = changed.PrivateKey;
            src.Address = changed.Address;
            src.MultiSig = changed.MultiSig;
            src.ColoredMultiSig = changed.ColoredMultiSig;
            src.PreventTxDetection = changed.PreventTxDetection;
            src.EncodedPrivateKey = changed.EncodedPrivateKey;
            src.PublicKey = changed.PublicKey;
            src.BtcConvertionWalletPrivateKey = changed.BtcConvertionWalletPrivateKey;
            src.BtcConvertionWalletAddress = changed.BtcConvertionWalletAddress;
            src.EthConversionWalletAddress = changed.EthConversionWalletAddress;
            src.EthAddress = changed.EthAddress;
            src.EthPublicKey = changed.EthPublicKey;
            src.SolarCoinWalletAddress = changed.SolarCoinWalletAddress;
            src.ChronoBankContract = changed.ChronoBankContract;
            src.QuantaContract = changed.QuantaContract;
        }

        public string ClientId { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string MultiSig { get; set; }
        public string ColoredMultiSig { get; set; }
        public bool PreventTxDetection { get; set; }
        public string EncodedPrivateKey { get; set; }
        public string BtcConvertionWalletPrivateKey { get; set; }
        public string BtcConvertionWalletAddress { get; set; }
        public string EthConversionWalletAddress { get; set; }
        public string EthAddress { get; set; }
        public string EthPublicKey { get; set; }
        public string SolarCoinWalletAddress { get; set; }
        public string ChronoBankContract { get; set; }
        public string QuantaContract { get; set; }
    }

    public class WalletCredentialsRepository : IWalletCredentialsRepository
    {
        private readonly INoSQLTableStorage<WalletCredentialsEntity> _tableStorage;

        public WalletCredentialsRepository(INoSQLTableStorage<WalletCredentialsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }


        public Task SaveAsync(IWalletCredentials walletCredentials)
        {
            var newByClientEntity = WalletCredentialsEntity.ByClientId.CreateNew(walletCredentials);
            var newByMultisigEntity = WalletCredentialsEntity.ByMultisig.CreateNew(walletCredentials);
            var newByColoredEntity = WalletCredentialsEntity.ByColoredMultisig.CreateNew(walletCredentials);

            var insertByEthTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.EthConversionWalletAddress))
            {
                var newByEthWalletEntity = WalletCredentialsEntity.ByEthContract.CreateNew(walletCredentials);
                insertByEthTask = _tableStorage.InsertAsync(newByEthWalletEntity);
            }

            var insertBySolarTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.SolarCoinWalletAddress))
            {
                var newBySolarWalletEntity = WalletCredentialsEntity.BySolarCoinWallet.CreateNew(walletCredentials);
                insertBySolarTask = _tableStorage.InsertAsync(newBySolarWalletEntity);
            }

            var insertByChronoBankTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.ChronoBankContract))
            {
                var newByChronoContractEntity = WalletCredentialsEntity.ByChronoBankContract.CreateNew(walletCredentials);
                insertByChronoBankTask = _tableStorage.InsertAsync(newByChronoContractEntity);
            }

            var insertByQuantaTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.QuantaContract))
            {
                var newByQuantaContractEntity = WalletCredentialsEntity.ByQuantaContract.CreateNew(walletCredentials);
                insertByQuantaTask = _tableStorage.InsertAsync(newByQuantaContractEntity);
            }

            return Task.WhenAll(
                _tableStorage.InsertAsync(newByClientEntity),
                _tableStorage.InsertAsync(newByMultisigEntity),
                _tableStorage.InsertAsync(newByColoredEntity),
                insertByEthTask,
                insertBySolarTask,
                insertByChronoBankTask,
                insertByQuantaTask
            );
        }

        public Task MergeAsync(IWalletCredentials walletCredentials)
        {
            var newByClientEntity = WalletCredentialsEntity.ByClientId.CreateNew(walletCredentials);
            var newByMultisigEntity = WalletCredentialsEntity.ByMultisig.CreateNew(walletCredentials);
            var newByColoredEntity = WalletCredentialsEntity.ByColoredMultisig.CreateNew(walletCredentials);

            var insertByEthTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.EthConversionWalletAddress))
            {
                var newByEthWalletEntity = WalletCredentialsEntity.ByEthContract.CreateNew(walletCredentials);
                insertByEthTask = _tableStorage.InsertOrMergeAsync(newByEthWalletEntity);
            }

            var insertBySolarTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.SolarCoinWalletAddress))
            {
                var newBySolarWalletEntity = WalletCredentialsEntity.BySolarCoinWallet.CreateNew(walletCredentials);
                insertBySolarTask = _tableStorage.InsertOrMergeAsync(newBySolarWalletEntity);
            }

            var insertByChronoBankTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.ChronoBankContract))
            {
                var newByChronoContractEntity = WalletCredentialsEntity.ByChronoBankContract.CreateNew(walletCredentials);
                insertByChronoBankTask = _tableStorage.InsertOrMergeAsync(newByChronoContractEntity);
            }

            var insertByQuantaTask = Task.CompletedTask;
            if (!string.IsNullOrEmpty(walletCredentials.QuantaContract))
            {
                var newByQuantaEntity = WalletCredentialsEntity.ByQuantaContract.CreateNew(walletCredentials);
                insertByQuantaTask = _tableStorage.InsertOrMergeAsync(newByQuantaEntity);
            }

            return Task.WhenAll(
                _tableStorage.InsertOrMergeAsync(newByClientEntity),
                _tableStorage.InsertOrMergeAsync(newByMultisigEntity),
                _tableStorage.InsertOrMergeAsync(newByColoredEntity),
                insertByEthTask,
                insertBySolarTask,
                insertByChronoBankTask,
                insertByQuantaTask);
        }

        public async Task<IWalletCredentials> GetAsync(string clientId)
        {
            var partitionKey = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            if (entity == null)
                return null;

            return string.IsNullOrEmpty(entity.MultiSig) ? null : entity;
        }

        public async Task<IWalletCredentials> GetByEthConversionWalletAsync(string ethWallet)
        {
            var partitionKey = WalletCredentialsEntity.ByEthContract.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.ByEthContract.GenerateRowKey(ethWallet);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<IWalletCredentials> GetBySolarCoinWalletAsync(string address)
        {
            var partitionKey = WalletCredentialsEntity.BySolarCoinWallet.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.BySolarCoinWallet.GenerateRowKey(address);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<IWalletCredentials> GetByChronoBankContractAsync(string contract)
        {
            var partitionKey = WalletCredentialsEntity.ByChronoBankContract.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.ByChronoBankContract.GenerateRowKey(contract);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<string> GetClientIdByMultisig(string multisig)
        {
            var partitionKey = WalletCredentialsEntity.ByMultisig.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.ByMultisig.GenerateRowKey(multisig);

            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            if (entity == null)
            {
                //try to find by colored
                partitionKey = WalletCredentialsEntity.ByColoredMultisig.GeneratePartitionKey();
                rowKey = WalletCredentialsEntity.ByColoredMultisig.GenerateRowKey(multisig);
                entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);
            }

            return entity?.ClientId;
        }

        public async Task SetPreventTxDetection(string clientId, bool value)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);
            var changedRecord = WalletCredentials.Create(currentRecord);
            changedRecord.PreventTxDetection = value;

            await MergeAsync(changedRecord);
        }

        public async Task SetEncodedPrivateKey(string clientId, string encodedPrivateKey)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);
            var changedRecord = WalletCredentials.Create(currentRecord);
            changedRecord.EncodedPrivateKey = encodedPrivateKey;

            await MergeAsync(changedRecord);
        }

        public async Task SetEthConversionWallet(string clientId, string contract)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);

            if (string.IsNullOrEmpty(currentRecord.EthConversionWalletAddress))
            {
                var changedRecord = WalletCredentials.Create(currentRecord);
                changedRecord.EthConversionWalletAddress = contract;

                var newByEthWalletEntity = WalletCredentialsEntity.ByEthContract.CreateNew(changedRecord);
                await _tableStorage.InsertOrReplaceAsync(newByEthWalletEntity);

                await MergeAsync(changedRecord);
            }
        }

        public async Task SetEthFieldsWallet(string clientId, string contract, string address, string pubKey)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);

            var changedRecord = WalletCredentials.Create(currentRecord);
            changedRecord.EthConversionWalletAddress = contract;
            changedRecord.EthPublicKey = pubKey;
            changedRecord.EthAddress = address;

            if (string.IsNullOrEmpty(currentRecord.EthConversionWalletAddress))
            {
                var newByEthWalletEntity = WalletCredentialsEntity.ByEthContract.CreateNew(changedRecord);
                await _tableStorage.InsertOrReplaceAsync(newByEthWalletEntity);
            }

            await MergeAsync(changedRecord);
        }

        public async Task SetSolarCoinWallet(string clientId, string address)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);

            if (string.IsNullOrEmpty(currentRecord.SolarCoinWalletAddress))
            {
                var changedRecord = WalletCredentials.Create(currentRecord);
                changedRecord.SolarCoinWalletAddress = address;
                await MergeAsync(changedRecord);
            }
        }

        public async Task SetChronoBankContract(string clientId, string contract)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);

            if (string.IsNullOrEmpty(currentRecord.ChronoBankContract))
            {
                var changedRecord = WalletCredentials.Create(currentRecord);
                changedRecord.ChronoBankContract = contract;
                await MergeAsync(changedRecord);
            }
        }

        public async Task<IWalletCredentials> ScanAndFind(Func<IWalletCredentials, bool> callBack)
        {
            var partitionKey = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();

            return await _tableStorage.FirstOrNullViaScanAsync(partitionKey, chunk =>
                { return chunk.FirstOrDefault(item => callBack(item)); });
        }

        public Task ScanAllAsync(Func<IEnumerable<IWalletCredentials>, Task> chunk)
        {
            var partitionKey = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();

            return _tableStorage.ScanDataAsync(partitionKey, chunk);

        }

        public async Task<IWalletCredentials> GetByQuantaContractAsync(string contract)
        {
            var partitionKey = WalletCredentialsEntity.ByQuantaContract.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.ByQuantaContract.GenerateRowKey(contract);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task SetQuantaContract(string clientId, string contract)
        {
            var partitionKeyByClient = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKeyByClient = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var currentRecord = await _tableStorage.GetDataAsync(partitionKeyByClient, rowKeyByClient);

            if (string.IsNullOrEmpty(currentRecord.QuantaContract))
            {
                var changedRecord = WalletCredentials.Create(currentRecord);
                changedRecord.QuantaContract = contract;
                await MergeAsync(changedRecord);
            }
        }
    }
}