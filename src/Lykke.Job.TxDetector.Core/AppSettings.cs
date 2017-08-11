using System;
using System.Net;

namespace Lykke.Job.TxDetector.Core
{
    public class AppSettings
    {
        public OperationsRepositoryClientSettings OperationsRepositoryClient { get; set; }
        public TxDetectorSettings TxDetectorJob { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }

        public class TxDetectorSettings
        {
            public DbSettings Db { get; set; }
            public MatchingEngineSettings MatchingEngine { get; set; }
            public AssetsCacheSettings AssetsCache { get; set; }
            public NinjaSettings Ninja { get; set; }
            public NotificationsSettings Notifications { get; set; }
            public int TxDetectorConfirmationsLimit { get; set; }
            public int ProcessInParallelCount { get; set; }
        }

        public class DbSettings
        {
            public string LogsConnString { get; set; }
            public string BitCoinQueueConnectionString { get; set; }
            public string ClientPersonalInfoConnString { get; set; }
            public string HTradesConnString { get; set; }
        }

        public class NotificationsSettings
        {
            public string HubConnectionString { get; set; }
            public string HubName { get; set; }
        }

        public class NinjaSettings
        {
            public bool IsMainNet { get; set; }
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
                return new IPEndPoint(IPAddress.Parse(Host), Port);
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

            public int ThrottlingLimitSeconds { get; set; }
        }

        public class AzureQueueSettings
        {
            public string ConnectionString { get; set; }

            public string QueueName { get; set; }
        }

        public class OperationsRepositoryClientSettings
        {
            public string ServiceUrl { get; set; }
            public int RequestTimeout { get; set; }
        }
    }

    
}