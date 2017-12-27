using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.Settings
{
    public class AppGlobalSettingsEntity : TableEntity, IAppGlobalSettings
    {
        public static string GeneratePartitionKey()
        {
            return "Setup";
        }

        public static string GenerateRowKey()
        {
            return "AppSettings";
        }

        public bool BtcOperationsDisabled { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
    }

    public class AppGlobalSettingsRepository : IAppGlobalSettingsRepositry
    {

        private readonly INoSQLTableStorage<AppGlobalSettingsEntity> _tableStorage;

        public AppGlobalSettingsRepository(INoSQLTableStorage<AppGlobalSettingsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IAppGlobalSettings> GetAsync()
        {
            var partitionKey = AppGlobalSettingsEntity.GeneratePartitionKey();
            var rowKey = AppGlobalSettingsEntity.GenerateRowKey();
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

    }
}
