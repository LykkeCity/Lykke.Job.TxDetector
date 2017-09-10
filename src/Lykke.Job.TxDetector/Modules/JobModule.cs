using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.ResolveAnything;
using AzureStorage.Queue;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
using AzureStorage.Tables.Templates.Index;
using Common;
using Common.Log;
using Lykke.Job.TxDetector.AzureRepositories.BitCoin;
using Lykke.Job.TxDetector.AzureRepositories.Clients;
using Lykke.Job.TxDetector.AzureRepositories.Messages.Email;
using Lykke.Job.TxDetector.AzureRepositories.PaymentSystems;
using Lykke.Job.TxDetector.AzureRepositories.Settings;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Lykke.Job.TxDetector.Core.Domain.Clients;
using Lykke.Job.TxDetector.Core.Domain.Messages.Email.ContentGenerator;
using Lykke.Job.TxDetector.Core.Domain.PaymentSystems;
using Lykke.Job.TxDetector.Core.Domain.Settings;
using Lykke.Job.TxDetector.Core.Services;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Core.Services.Messages;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Services;
using Lykke.Job.TxDetector.Services.BitCoin;
using Lykke.Job.TxDetector.Services.Messages;
using Lykke.Job.TxDetector.Services.Messages.Email;
using Lykke.Job.TxDetector.Services.Notifications;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.OperationsRepository.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.TxDetector.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _settings;
        private readonly AppSettings.DbSettings _dbSettings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(AppSettings settings, ILog log)
        {
            _settings = settings;
            _dbSettings = settings.TxDetectorJob.Db;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.TxDetectorJob)
                .SingleInstance();

            builder.RegisterInstance(_settings.TxDetectorJob.Ninja)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(TimeSpan.FromSeconds(30)));

            //// NOTE: You can implement your own poison queue notifier. See https://github.com/LykkeCity/JobTriggers/blob/master/readme.md
            //// builder.Register<PoisionQueueNotifierImplementation>().As<IPoisionQueueNotifier>();

            _services.UseAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.Assets.ServiceUrl),
                AssetPairsCacheExpirationPeriod = _settings.TxDetectorJob.AssetsCache.ExpirationPeriod,
                AssetsCacheExpirationPeriod = _settings.TxDetectorJob.AssetsCache.ExpirationPeriod
            });

            BindMatchingEngineChannel(builder);
            BindRepositories(builder);
            BindServices(builder);

            builder.Populate(_services);
        }

        private void BindServices(ContainerBuilder builder)
        {
            builder.RegisterType<SrvNinjaBlockChainReader>().As<ISrvBlockchainReader>().SingleInstance();

            builder.RegisterType<SrvEmailsFacade>().As<ISrvEmailsFacade>().SingleInstance();

            builder.RegisterType<EmailSender>().As<IEmailSender>().SingleInstance();

            builder.Register<IAppNotifications>(x => new SrvAppNotifications(_settings.TxDetectorJob.Notifications.HubConnectionString, _settings.TxDetectorJob.Notifications.HubName));

            builder.RegisterOperationsRepositoryClients(_settings.OperationsRepositoryClient.ServiceUrl, _log,
                _settings.OperationsRepositoryClient.RequestTimeout);

            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
        }

        private void BindRepositories(ContainerBuilder builder)
        {
            builder.RegisterInstance<IPostponedCashInRepository>(
                new PostponedCashInRepository(
                    new AzureTableStorage<PostponedCashInRecord>(_dbSettings.BitCoinQueueConnectionString, "PostponedBtcCashIns", _log)));

            builder.RegisterInstance<IConfirmedTransactionsRepository>(
                new ConfirmedTransactionsRepository(
                    new AzureTableStorage<ConfirmedTransactionRecord>(_dbSettings.BitCoinQueueConnectionString, "ConfirmedTransactions", _log)));

            builder.RegisterInstance<IBalanceChangeTransactionsRepository>(
                new BalanceChangeTransactionsRepository(
                    new AzureTableStorage<BalanceChangeTransactionEntity>(_dbSettings.BitCoinQueueConnectionString, "BalanceChangeTransactions", _log)));

            builder.RegisterInstance<IBlockchainTransactionsCache>(
                new BlockchainTransactionsCache(
                    new AzureTableStorage<ObsoleteBlockchainTransactionsCacheItem>(_dbSettings.BitCoinQueueConnectionString, "Transactions", _log)));

            builder.RegisterInstance<IConfirmPendingTxsQueue>(
                new ConfirmPendingTxsQueue(
                    new AzureQueueExt(_dbSettings.BitCoinQueueConnectionString, "txs-confirm-pending")));

            builder.RegisterInstance<ILastProcessedBlockRepository>(
                new LastProcessedBlockRepository(
                    new RetryOnFailureAzureTableStorageDecorator<LastProcessedBlockEntity>(
                        new AzureTableStorage<LastProcessedBlockEntity>(_dbSettings.BitCoinQueueConnectionString, "LastProcessedBlocks", _log),
                        onGettingRetryCount: 5)));

            builder.RegisterInstance<IInternalOperationsRepository>(
                new InternalOperationsRepository(
                    new AzureTableStorage<InternalOperationEntity>(_dbSettings.BitCoinQueueConnectionString, "InternalOperations", _log)));

            builder.RegisterInstance<IWalletCredentialsRepository>(
                new WalletCredentialsRepository(
                    new AzureTableStorage<WalletCredentialsEntity>(_dbSettings.ClientPersonalInfoConnString, "WalletCredentials", _log)));

            builder.RegisterInstance<IAppGlobalSettingsRepositry>(
                new AppGlobalSettingsRepository(
                    new AzureTableStorage<AppGlobalSettingsEntity>(_dbSettings.ClientPersonalInfoConnString, "Setup", _log)));

            builder.RegisterInstance<IClientAccountsRepository>(
                new ClientsRepository(
                    new AzureTableStorage<ClientAccountEntity>(_dbSettings.ClientPersonalInfoConnString, "Traders", _log)));

            builder.RegisterInstance<IClientSettingsRepository>(
                new ClientSettingsRepository(new AzureTableStorage<ClientSettingsEntity>(_dbSettings.ClientPersonalInfoConnString, "TraderSettings", _log)));

            builder.RegisterInstance<IEmailCommandProducer>(
                new EmailCommandProducer(
                    new AzureQueueExt(_dbSettings.ClientPersonalInfoConnString, "emailsqueue")));

            builder.RegisterInstance<IPaymentTransactionEventsLog>(
                new PaymentTransactionEventsLog(
                    new AzureTableStorage<PaymentTransactionLogEventEntity>(_dbSettings.LogsConnString, "PaymentsLog", _log)));

            builder.RegisterInstance<IPaymentTransactionsRepository>(
                new PaymentTransactionsRepository(
                        new AzureTableStorage<PaymentTransactionEntity>(_dbSettings.ClientPersonalInfoConnString, "PaymentTransactions", _log), 
                        new AzureTableStorage<AzureMultiIndex>(_dbSettings.ClientPersonalInfoConnString, "PaymentTransactions", _log)));
        }

        private void BindMatchingEngineChannel(ContainerBuilder container)
        {
            var socketLog = new SocketLogDynamic(i => { },
                str => Console.WriteLine(DateTime.UtcNow.ToIsoDateTime() + ": " + str));

            container.BindMeConnector(_settings.TxDetectorJob.MatchingEngine.IpEndpoint.GetClientIpEndPoint(), socketLog);
        }
    }
}