using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Events
{
    [ProtoContract]
    public class TransactionCreatedEvent
    {        
        [ProtoMember(1)]

        public string OrderId { get; set; }
    }
}