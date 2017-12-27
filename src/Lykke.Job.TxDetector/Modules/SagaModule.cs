﻿using System;
using System.Collections.Generic;
using Autofac;
using Common.Log;
using Inceptum.Cqrs.Configuration;
using Inceptum.Messaging;
using Inceptum.Messaging.RabbitMq;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Handlers;
using Lykke.Job.TxDetector.Sagas;
using Lykke.Messaging;
using Lykke.SettingsReader;

namespace Lykke.Job.TxDetector.Modules
{
    public class SagaModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;

        public SagaModule(IReloadingManager<AppSettings> settingsManager, ILog log)
        {
            _settings = settingsManager.CurrentValue;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_settings.TxDetectorJob.ChaosKitty != null)
            {
                ChaosKitty.StateOfChaos = _settings.TxDetectorJob.ChaosKitty.StateOfChaos;
            }
            var virtualHost = string.Empty; // "/debug";

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo($"amqp://{_settings.RabbitMq.ExternalHost}{virtualHost}", _settings.RabbitMq.Username, _settings.RabbitMq.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory());

            builder.RegisterType<ConfirmationsSaga>();

            builder.RegisterType<TransactionHandler>();
            builder.RegisterType<TransferHandler>();
            builder.RegisterType<CashInHandler>();
            builder.RegisterType<NotificationsHandler>();
            builder.RegisterType<EmailHandler>();

            var defaultRetryDelay = (long)TimeSpan.FromSeconds(3).TotalMilliseconds;
            builder.Register(ctx => new CqrsEngine(
                _log,
                ctx.Resolve<IDependencyResolver>(),
                messagingEngine,
                new DefaultEndpointProvider(),
                true,
                Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver("RabbitMq", "protobuf", environment: _settings.TxDetectorJob.ExchangePrefix)),

                Register.BoundedContext("transactions")
                    .FailedCommandRetryDelay(defaultRetryDelay)
                    .ListeningCommands(typeof(ProcessTransactionCommand))
                        .On("transactions-commands")
                    .PublishingEvents(typeof(TransferOperationCreatedEvent), typeof(CashInOperationCreatedEvent))
                        .With("transactions-events")
                    .WithCommandsHandler<TransactionHandler>(),

                Register.BoundedContext("transfer")
                    .FailedCommandRetryDelay(defaultRetryDelay)
                    .ListeningCommands(typeof(ProcessTransferCommand))
                        .On("transfer-commands")
                    .WithCommandsHandler<TransferHandler>(),

                Register.BoundedContext("cachein")
                    .FailedCommandRetryDelay(defaultRetryDelay)
                    .ListeningCommands(typeof(RegisterCachInOutCommand), typeof(ProcessCashInCommand))
                        .On("cachein-commands")
                    .PublishingEvents(typeof(CashInOutOperationRegisteredEvent), typeof(TransactionProcessedEvent))
                        .With("cachein-events")
                    .WithCommandsHandler<CashInHandler>(),

                Register.BoundedContext("notifications")
                    .FailedCommandRetryDelay(defaultRetryDelay)
                    .ListeningCommands(typeof(SendNotificationCommand))
                        .On("notifications-commands")
                    .WithCommandsHandler<NotificationsHandler>(),

                Register.BoundedContext("email")
                    .FailedCommandRetryDelay(defaultRetryDelay)
                    .ListeningCommands(typeof(SendNoRefundDepositDoneMailCommand))
                        .On("email-commands")
                    .WithCommandsHandler<EmailHandler>(),

                Register.Saga<ConfirmationsSaga>("transactions-saga")
                    .ListeningEvents(typeof(TransferOperationCreatedEvent), typeof(CashInOperationCreatedEvent))
                        .From("transactions").On("transactions-events")
                    .ListeningEvents(typeof(CashInOutOperationRegisteredEvent), typeof(TransactionProcessedEvent))
                        .From("cachein").On("cachein-events")
                    .PublishingCommands(typeof(ProcessTransferCommand))
                        .To("transfer").With("transfer-commands")
                    .PublishingCommands(typeof(RegisterCachInOutCommand), typeof(ProcessCashInCommand))
                        .To("cachein").With("cachein-commands")
                    .PublishingCommands(typeof(SendNoRefundDepositDoneMailCommand))
                        .To("email").With("email-commands")
                    .PublishingCommands(typeof(SendNotificationCommand))
                        .To("notifications").With("notifications-commands"),

                Register.DefaultRouting
                    .PublishingCommands(typeof(ProcessTransactionCommand))
                        .To("transactions").With("transactions-commands")
                    .PublishingCommands(typeof(ProcessTransferCommand))
                        .To("transfer").With("transfer-commands")
                    .PublishingCommands(typeof(RegisterCachInOutCommand), typeof(ProcessCashInCommand))
                        .To("cachein").With("cachein-commands")
                    .PublishingCommands(typeof(SendNoRefundDepositDoneMailCommand))
                        .To("email").With("email-commands")
                    .PublishingCommands(typeof(SendNotificationCommand))
                        .To("notifications").With("notifications-commands")))
            .As<ICqrsEngine>().SingleInstance();
        }
    }
}