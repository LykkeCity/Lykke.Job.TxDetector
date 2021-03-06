﻿using Lykke.Job.TxDetector.Core.Services.Notifications;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Commands
{
    [ProtoContract]
    public class SendNotificationCommand
    {
        [ProtoMember(1)]
        public string NotificationId { get; set; }
        [ProtoMember(2)]
        public NotificationType Type { get; set; }
        [ProtoMember(3)]
        public string Message { get; set; }
    }
}
