using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Models;
using Lykke.Job.TxDetector.Modules;
using Lykke.JobTriggers.Extenstions;
using Lykke.JobTriggers.Triggers;
using Lykke.Logs;
using Lykke.Logs.Slack;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.TxDetector
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }
        public IConfigurationRoot Configuration { get; }
        public ILog Log { get; private set; }

        private TriggerHost _triggerHost;
        private Task _triggerHostTask;

        private const string AppName = "Lykke.Job.TxDetector";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                        .AddJsonOptions(options =>
                        {
                            options.SerializerSettings.ContractResolver =
                                new Newtonsoft.Json.Serialization.DefaultContractResolver();
                        });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "TxDetector API");
                });

                var builder = new ContainerBuilder();
                IReloadingManager<AppSettings> settingsManager = Configuration.LoadSettings<AppSettings>();
                Log = CreateLogWithSlack(services, settingsManager);

                builder.RegisterModule(new JobModule(settingsManager, Log));

                string bitCoinQueueConnectionString = settingsManager.CurrentValue.TxDetectorJob.Db.BitCoinQueueConnectionString;
                if (string.IsNullOrWhiteSpace(bitCoinQueueConnectionString))
                {
                    builder.AddTriggers();
                }
                else
                {
                    builder.AddTriggers(pool =>
                    {
                        pool.AddDefaultConnection(bitCoinQueueConnectionString);
                    });
                }

                builder.Populate(services);

                ApplicationContainer = builder.Build();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseLykkeMiddleware("TxDetector", ex => new ErrorResponse { ErrorMessage = "Technical problem" });

                app.UseMvc();
                app.UseSwagger();
                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().Wait());
                appLifetime.ApplicationStopping.Register(() => StopApplication().Wait());
                appLifetime.ApplicationStopped.Register(() => CleanUp().Wait());
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                _triggerHost = new TriggerHost(new AutofacServiceProvider(ApplicationContainer));
                _triggerHostTask = _triggerHost.Start();

                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                _triggerHost?.Cancel();
                _triggerHostTask?.Wait();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }
                throw;
            }
        }

        private static ILog CreateLogWithSlack(IServiceCollection services, IReloadingManager<AppSettings> settingsManager)
        {
            LykkeLogToAzureStorage logToAzureStorage = null;

            var logToConsole = new LogToConsole();
            var logAggregate = new LogAggregate();

            logAggregate.AddLogger(logToConsole);

            var settings = settingsManager.CurrentValue;

            var dbLogConnectionString = settings.TxDetectorJob.Db.LogsConnString;

            // Creating azure storage logger, which logs own messages to concole log
            if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}")))
            {
                var tableStorage = AzureTableStorage<LogEntity>.Create(
                    settingsManager.ConnectionString(i => i.TxDetectorJob.Db.LogsConnString), "TxDetectorLog", logToConsole);
                var persistanceManager = new LykkeLogToAzureStoragePersistenceManager(AppName, tableStorage);
                logToAzureStorage =
                    new LykkeLogToAzureStorage(AppName, persistanceManager, lastResortLog: logToConsole);

                logAggregate.AddLogger(logToAzureStorage);
            }

            // Creating aggregate log, which logs to console and to azure storage, if last one specified
            var log = logAggregate.CreateLogger();

            // Creating slack notification service, which logs own azure queue processing messages to aggregate log
            var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueIntegration.AzureQueueSettings
            {
                ConnectionString = settings.SlackNotifications.AzureQueue.ConnectionString,
                QueueName = settings.SlackNotifications.AzureQueue.QueueName
            }, log);

            // Finally, setting slack notification for azure storage log, which will forward necessary message to slack service
            logToAzureStorage?.SetSlackNotificationsManager(
                new LykkeLogToAzureSlackNotificationsManager(
                    $"{AppName} {PlatformServices.Default.Application.ApplicationVersion}", slackService, logToConsole));

            var logToSlack = LykkeLogToSlack.Create(slackService, "TxDetector");
            logAggregate.AddLogger(logToSlack);
            log = logAggregate.CreateLogger();

            logToAzureStorage?.Start();
            return log;
        }
    }
}