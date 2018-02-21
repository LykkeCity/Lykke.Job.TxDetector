using System;
using Lykke.Job.TxDetector.Models;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
    [Obsolete("Class is not deleted now only for compatibility purpose. Should be deleted after next release.")]
    [ProtoContract]
    public class CashInOperationCreatedEvent
    {
        [ProtoMember(1)]
        public Transaction Transaction { get; set; }
        [ProtoMember(2)]
        public Asset Asset { get; set; }
        [ProtoMember(3)]
        public double Amount { get; set; }
    }
}
