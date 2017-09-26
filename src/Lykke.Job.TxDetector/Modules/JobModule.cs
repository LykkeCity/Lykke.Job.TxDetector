﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Queue;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
using AzureStorage.Tables.Templates.Index;
using Common;
using Common.Log;
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
using Lykke.Job.TxDetector.Core.Services.Messages;
using Lykke.Job.TxDetector.Core.Services.Messages.Email;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Services;
using Lykke.Job.TxDetector.Services.BitCoin;
using Lykke.Job.TxDetector.Services.Messages;
using Lykke.Job.TxDetector.Services.Messages.Email;
using Lykke.Job.TxDetector.Services.Notifications;
using Lykke.Job.TxDetector.TriggerHandlers.Handlers;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Services;
using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
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

            builder.RegisterType<TransferHandler>().SingleInstance();

            builder.RegisterType<CashInHandler>().SingleInstance();
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

            builder.RegisterInstance<IConfirmPendingTxsQueue>(
                new ConfirmPendingTxsQueue(
                    AzureQueueExt.Create(
                        _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "txs-confirm-pending")));

            builder.RegisterInstance<ILastProcessedBlockRepository>(
                new LastProcessedBlockRepository(
                    new RetryOnFailureAzureTableStorageDecorator<LastProcessedBlockEntity>(
                        AzureTableStorage<LastProcessedBlockEntity>.Create(
                            _settingsManager.ConnectionString(i => i.TxDetectorJob.Db.BitCoinQueueConnectionString), "LastProcessedBlocks", _log),
                        onGettingRetryCount: 5)));

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
    }
}