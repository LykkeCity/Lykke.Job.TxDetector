﻿using Lykke.Job.TxDetector.Sagas.Models;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Events
{
    [ProtoContract]
    public class CashInOutOperationRegisteredEvent
    {
        [ProtoMember(1)]
        public Transaction Transaction { get; set; }
        [ProtoMember(2)]
        public Asset Asset { get; set; }
        [ProtoMember(3)]
        public double Amount { get; set; }
        [ProtoMember(4)]
        public string CommandId { get; set; }
    }
}
