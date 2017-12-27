using Lykke.Job.TxDetector.Models;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
    [ProtoContract]
    public class TransactionProcessedEvent
    {
        [ProtoMember(1)]
        public string ClientId { get; set; }
        [ProtoMember(2)]
        public Asset Asset { get; set; }
        [ProtoMember(3)]
        public double Amount { get; set; }
    }
}
