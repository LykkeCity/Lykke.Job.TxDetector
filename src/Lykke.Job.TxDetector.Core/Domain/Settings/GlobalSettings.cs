using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.Settings
{
    public interface IAppGlobalSettings
    {
        bool BtcOperationsDisabled { get; }
        bool BitcoinBlockchainOperationsDisabled { get; }
    }

    public class AppGlobalSettings : IAppGlobalSettings
    {
        public bool BtcOperationsDisabled { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
    }

    public interface IAppGlobalSettingsRepositry
    {
        Task<IAppGlobalSettings> GetAsync();
    }
}
