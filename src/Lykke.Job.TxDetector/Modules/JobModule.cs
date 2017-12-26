using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Queue;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.RabbitMq;
using Lykke.Cqrs;
using Lykke.SettingsReader;
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
using Lykke.Job.TxDetector.Core.Services.Messages.Email;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Sagas;
using Lykke.Job.TxDetector.Sagas.Commands;
using Lykke.Job.TxDetector.Sagas.Events;
using Lykke.Job.TxDetector.Sagas.Handlers;
using Lykke.Job.TxDetector.Services;
using Lykke.Job.TxDetector.Services.BitCoin;
using Lykke.Job.TxDetector.Services.Messages.Email;
using Lykke.Job.TxDetector.Services.Notifications;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Messaging;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.OperationsRepository.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.TxDetector.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settingsManager;
        private readonly AppSettings _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(IReloadingManager<AppSettings> settingsManager, ILog log)
        {
            _settingsManager = settingsManager;
            _settings = settingsManager.CurrentValue;
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

            builder.RegisterOperationsRepositoryClients(_settings.OperationsRepositoryServiceClient, _log);

            // NOTE: You can implement your own poison queue notifier. See https://github.com/LykkeCity/JobTriggers/blob/master/readme.md
            // builder.Register<PoisionQueueNotifierImplementation>().As<IPoisionQueueNotifier>();

            _services.UseAssetsClient(new AssetServiceSettings
            {
                BaseUri = new Uri(_settings.Assets.ServiceUrl),
                AssetPairsCacheExpirationPeriod = _settings.TxDetectorJob.AssetsCache.ExpirationPeriod,
                AssetsCacheExpirationPeriod = _settings.TxDetectorJob.AssetsCache.ExpirationPeriod
            });

            BindMatchingEngineChannel(builder);
            BindRepositories(builder);
            BindServices(builder);
            BindSaga(builder);

            builder.Populate(_services);
        }

        private void BindServices(ContainerBuilder builder)
        {
            builder.RegisterType<SrvNinjaBlockChainReader>().As<ISrvBlockchainReader>().SingleInstance();
            
            builder.RegisterType<EmailSender>().As<IEmailSender>().SingleInstance();

            builder.Register<IAppNotifications>(x => new SrvAppNotifications(_settings.TxDetectorJob.Notifications.HubConnectionString, _settings.TxDetectorJob.Notifications.HubName));
        }

        private void BindRepositories(ContainerBuilder builder)
        {
            builder.RegisterInstance<IPostponedCashInRepository>(
                new PostponedCashInRepository(
                    AzureTableStorage<PostponedCashInRecord>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "PostponedBtcCashIns", _log)));

            builder.RegisterInstance<IConfirmedTransactionsRepository>(
                new ConfirmedTransactionsRepository(
                    AzureTableStorage<ConfirmedTransactionRecord>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "ConfirmedTransactions", _log)));

            builder.RegisterInstance<IBalanceChangeTransactionsRepository>(
                new BalanceChangeTransactionsRepository(
                    AzureTableStorage<BalanceChangeTransactionEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "BalanceChangeTransactions", _log)));

            builder.RegisterInstance<IBlockchainTransactionsCache>(
                new BlockchainTransactionsCache(
                    AzureTableStorage<ObsoleteBlockchainTransactionsCacheItem>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "Transactions", _log)));
            
            builder.RegisterInstance<ILastProcessedBlockRepository>(
                new LastProcessedBlockRepository(
                        AzureTableStorage<LastProcessedBlockEntity>.Create(
                            _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "LastProcessedBlocks", _log)));

            builder.RegisterInstance<IInternalOperationsRepository>(
                new InternalOperationsRepository(
                    AzureTableStorage<InternalOperationEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "InternalOperations", _log)));

            builder.RegisterInstance<IWalletCredentialsRepository>(
                new WalletCredentialsRepository(
                    AzureTableStorage<WalletCredentialsEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "WalletCredentials", _log)));

            builder.RegisterInstance<IAppGlobalSettingsRepositry>(
                new AppGlobalSettingsRepository(
                    AzureTableStorage<AppGlobalSettingsEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "Setup", _log)));

            builder.RegisterInstance<IClientAccountsRepository>(
                new ClientsRepository(
                    AzureTableStorage<ClientAccountEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "Traders", _log)));

            builder.RegisterInstance<IClientSettingsRepository>(
                new ClientSettingsRepository(
                    AzureTableStorage<ClientSettingsEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "TraderSettings", _log)));

            builder.RegisterInstance<IEmailCommandProducer>(
                new EmailCommandProducer(
                    AzureQueueExt.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "emailsqueue")));

            builder.RegisterInstance<IPaymentTransactionEventsLog>(
                new PaymentTransactionEventsLog(
                    AzureTableStorage<PaymentTransactionLogEventEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.LogsConnString), "PaymentsLog", _log)));

            builder.RegisterInstance<IPaymentTransactionsRepository>(
                new PaymentTransactionsRepository(
                    AzureTableStorage<PaymentTransactionEntity>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "PaymentTransactions", _log),
                    AzureTableStorage<AzureMultiIndex>.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.ClientPersonalInfoConnString), "PaymentTransactions", _log)));
        }

        private void BindMatchingEngineChannel(ContainerBuilder container)
        {
            var socketLog = new SocketLogDynamic(i => { },
                str => Console.WriteLine(DateTime.UtcNow.ToIsoDateTime() + ": " + str));

            container.BindMeClient(_settings.TxDetectorJob.MatchingEngine.IpEndpoint.GetClientIpEndPoint(), socketLog);
        }

        private void BindSaga(ContainerBuilder container)
        {
            var rabbitConnectionString = string.Empty;
            var messagingEngine = new MessagingEngine(null,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"rmq", new TransportInfo(rabbitConnectionString, string.Empty, string.Empty, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory());


            container.RegisterType<ConfirmationsSaga>();

            container.Register(ctx => new CqrsEngine(
                null,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver("rmq", "protobuf", environment: "dev")),

                Register.BoundedContext("transactions")
                    .FailedCommandRetryDelay((long)TimeSpan.FromSeconds(3).TotalMilliseconds)
                    .ListeningCommands(typeof(ProcessTransactionCommand))
                        .On("transactions-commands")
                    .PublishingEvents(typeof(TransferOperationCreatedEvent), typeof(CashInOperationCreatedEvent))
                        .With("transactions-events")
                    .WithCommandsHandler<TransactionHandler>(),

                Register.BoundedContext("transfer")
                    .FailedCommandRetryDelay((long)TimeSpan.FromSeconds(5).TotalMilliseconds)
                    .ListeningCommands(typeof(HandleTransferCommand))
                        .On("transfer-commands")
                    .WithCommandsHandler<TransferHandler>(),

                Register.BoundedContext("cachein")
                    .FailedCommandRetryDelay((long)TimeSpan.FromSeconds(5).TotalMilliseconds)
                    .ListeningCommands(typeof(ProcessCashInCommand))
                        .On("cachein-commands")
                    .PublishingEvents(typeof(TransactionProcessedEvent))
                        .With("cachein-events")
                    .WithCommandsHandler<CashInHandler>(),

                Register.BoundedContext("notifications")
                    .FailedCommandRetryDelay((long)TimeSpan.FromSeconds(5).TotalMilliseconds)
                    .ListeningCommands(typeof(SendNotificationCommand))
                        .On("notifications-commands")
                    .WithCommandsHandler<NotificationsHandler>(),

                Register.BoundedContext("email")
                    .FailedCommandRetryDelay((long)TimeSpan.FromMinutes(1).TotalMilliseconds)
                    .ListeningCommands(typeof(SendNoRefundDepositDoneMailCommand))
                        .On("email-commands")
                    .WithCommandsHandler<EmailHandler>(),
                
                Register.Saga<ConfirmationsSaga>("transactions-saga")
                    .ListeningEvents(typeof(TransferOperationCreatedEvent), typeof(CashInOperationCreatedEvent))
                        .From("transactions").On("transactions-events")
                    .ListeningEvents(typeof(TransactionProcessedEvent))
                        .From("cachein").On("cachein-events")
                    .PublishingCommands(typeof(HandleTransferCommand))
                        .To("transfer").With("transfer-commands")
                    .PublishingCommands(typeof(ProcessCashInCommand))
                        .To("cachein").With("cachein-commands")
                    .PublishingCommands(typeof(SendNoRefundDepositDoneMailCommand))
                        .To("transfer").With("transfer-commands")
                    .PublishingCommands(typeof(SendNotificationCommand))
                        .To("email").With("email-commands"),
                
                Register.DefaultRouting
                    .PublishingCommands(typeof(ProcessTransactionCommand))
                        .To("transactions").With("transactions-commands")
                    .PublishingCommands(typeof(HandleTransferCommand))
                        .To("transfer").With("transfer-commands")
                    .PublishingCommands(typeof(ProcessCashInCommand))
                        .To("cachein").With("cachein-commands")))
            .As<ICqrsEngine>().SingleInstance();
        }
    }
}