using System;
using System.Net;
using Lykke.Service.OperationsRepository.Client;
using Lykke.SettingsReader.Attributes;
using NBitcoin;

namespace Lykke.Job.TxDetector.Core
{
    public class AppSettings
    {
        public TxDetectorSettings TxDetectorJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }
        public OperationsRepositoryServiceClientSettings OperationsRepositoryServiceClient { get; set; }
        public ClientAccountClientSettings ClientAccountClient { get; set; }

        public class TxDetectorSettings
        {
            public DbSettings Db { get; set; }
            public MatchingEngineSettings MatchingEngine { get; set; }
            public AssetsCacheSettings AssetsCache { get; set; }
            public NinjaSettings Ninja { get; set; }
            public NotificationsSettings Notifications { get; set; }
            public int TxDetectorConfirmationsLimit { get; set; }
            public int ProcessInParallelCount { get; set; }
            public long RetryDelayInMilliseconds { get; set; }
            [Optional]
            public ChaosSettings ChaosKitty { get; set; }
            public string RabbitMQConnectionString { get; set; }
            public string Environment { get; set; }
        }

        public class ChaosSettings
        {
            public double StateOfChaos { get; set; }
        }

        public class DbSettings
        {
            public string LogsConnString { get; set; }
            public string BitCoinQueueConnectionString { get; set; }
            public string ClientPersonalInfoConnString { get; set; }
        }

        public class NotificationsSettings
        {
            public string HubConnectionString { get; set; }
            public string HubName { get; set; }
        }

        public class NinjaSettings
        {
            public bool IsMainNet { get; set; }

            [HttpCheck("/")]
            public string Url { get; set; }
        }

        public class MatchingEngineSettings
        {
            public IpEndpointSettings IpEndpoint { get; set; }
        }

        public class IpEndpointSettings
        {
            public string Host { get; set; }
            public int Port { get; set; }

            public IPEndPoint GetClientIpEndPoint(bool useInternal = false)
            {
                if (!IPAddress.TryParse(Host, out var address))
                    address = Dns.GetHostAddressesAsync(Host).Result[0];

                return new IPEndPoint(address, Port);
            }
        }

        public class AssetsCacheSettings
        {
            public TimeSpan ExpirationPeriod { get; set; }
        }

        public class AssetsSettings
        {
            public string ServiceUrl { get; set; }
        }

        public class SlackNotificationsSettings
        {
            public AzureQueueSettings AzureQueue { get; set; }
        }

        public class AzureQueueSettings
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }
        }

        public class ClientAccountClientSettings
        {
            public string ServiceUrl { get; set; }
        }
}

    public static class NinjaSettingsExtensions
    {
        public static Network GetNetwork(this AppSettings.NinjaSettings settings)
        {
            return settings.IsMainNet ? Network.Main : Network.TestNet;
        }
    }
}