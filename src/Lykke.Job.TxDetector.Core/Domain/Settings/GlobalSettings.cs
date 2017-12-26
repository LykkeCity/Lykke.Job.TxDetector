using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.Settings
{
    public interface IAppGlobalSettings
    {
        bool BtcOperationsDisabled { get; }
        bool BitcoinBlockchainOperationsDisabled { get; }
    }

    public interface IAppGlobalSettingsRepositry
    {
        Task<IAppGlobalSettings> GetAsync();
    }
}
