using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
    [ProtoContract]
    public class TransferOperationCreatedEvent
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
    }
}
