using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Sagas;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Handlers
{
    public class NotificationsHandler
    {
        private readonly ILog _log;
        private readonly IAppNotifications _appNotifications;

        public NotificationsHandler([NotNull] ILog log, [NotNull] IAppNotifications appNotifications)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _appNotifications = appNotifications ?? throw new ArgumentNullException(nameof(appNotifications));
        }

        public async Task Handle(SendNotificationCommand command)
        {
            await _log.WriteInfoAsync(nameof(NotificationsHandler), nameof(SendNotificationCommand), command.ToJson(), "");

            ChaosKitty.Meow();

            await _appNotifications.SendTextNotificationAsync(new [] {command.NotificationId}, command.Type, command.Message);
        }
    }
}
