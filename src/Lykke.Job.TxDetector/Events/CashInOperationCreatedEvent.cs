using Lykke.Job.TxDetector.Models;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
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
