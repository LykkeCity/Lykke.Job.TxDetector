using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Handlers
{
    public class NotificationsHandler
    {
        private readonly IAppNotifications _appNotifications;

        public NotificationsHandler([NotNull] IAppNotifications appNotifications)
        {
            _appNotifications = appNotifications ?? throw new ArgumentNullException(nameof(appNotifications));
        }

        public async Task<CommandHandlingResult> Handle(SendNotificationCommand command)
        {
            ChaosKitty.Meow();

            await _appNotifications.SendTextNotificationAsync(new [] {command.NotificationId}, command.Type, command.Message);

            return CommandHandlingResult.Ok();
        }
    }
}
