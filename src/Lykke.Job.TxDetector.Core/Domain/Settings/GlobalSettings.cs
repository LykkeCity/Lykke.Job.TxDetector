using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.Settings
{
    public interface IAppGlobalSettings
    {
        bool BitcoinOperationsDisabled { get; }
    }

    public class AppGlobalSettings : IAppGlobalSettings
    {
        public bool BitcoinOperationsDisabled { get; set; }
    }

    public interface IAppGlobalSettingsRepositry
    {
        Task<IAppGlobalSettings> GetAsync();
    }
}
