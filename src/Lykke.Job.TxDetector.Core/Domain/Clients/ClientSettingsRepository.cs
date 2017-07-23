using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.Clients
{
    public abstract class TraderSettingsBase
    {
        public abstract string GetId();

        public static T CreateDefault<T>() where T : TraderSettingsBase, new()
        {
            if (typeof(T) == typeof(RefundAddressSettings))
                return RefundAddressSettings.CreateDefault() as T;

            if (typeof(T) == typeof(PushNotificationsSettings))
                return PushNotificationsSettings.CreateDefault() as T;

            return new T();
        }
    }

    public class RefundAddressSettings : TraderSettingsBase
    {
        public override string GetId()
        {
            return "RefundAddressSettings";
        }

        public string Address { get; set; }
        public int? ValidDays { get; set; }
        public bool? SendAutomatically { get; set; }

        public static RefundAddressSettings CreateDefault()
        {
            return new RefundAddressSettings
            {
                Address = string.Empty,
                ValidDays = LykkeConstants.DefaultRefundTimeoutDays,
                SendAutomatically = false
            };
        }
    }

    public class PushNotificationsSettings : TraderSettingsBase
    {
        public override string GetId()
        {
            return "PushNotificationsSettings";
        }

        public bool Enabled { get; set; }

        public static PushNotificationsSettings CreateDefault()
        {
            return new PushNotificationsSettings
            {
                Enabled = true
            };
        }
    }

    public interface IClientSettingsRepository
    {
        Task<T> GetSettings<T>(string traderId) where T : TraderSettingsBase, new();
    }
}