﻿using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
    [ProtoContract]
    public class ConfirmationSavedEvent
    {
        [ProtoMember(1)]
        public string TransactionHash { get; set; }
        [ProtoMember(2)]
        public string ClientId { get; set; }
        [ProtoMember(3)]
        public string Multisig { get; set; }
    }
}
