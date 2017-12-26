using Lykke.Job.TxDetector.Sagas.Models;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Events
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
