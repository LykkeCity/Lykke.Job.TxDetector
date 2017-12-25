using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Sagas.Commands;

namespace Lykke.Job.TxDetector.Sagas.Handlers
{
    public class NotificationsHandler
    {
        private readonly IAppNotifications _appNotifications;
        private readonly ILog _log;

        public NotificationsHandler(
            [NotNull] ILog log,
            [NotNull] IAppNotifications appNotifications)
        {
            _appNotifications = appNotifications ?? throw new ArgumentNullException(nameof(appNotifications));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task Handle(SendNotificationCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(NotificationsHandler), nameof(SendNotificationCommand), command.ToJson());
            await _appNotifications.SendTextNotificationAsync(command.NotificationsIds, command.Type, command.Message);
        }
    }
}
