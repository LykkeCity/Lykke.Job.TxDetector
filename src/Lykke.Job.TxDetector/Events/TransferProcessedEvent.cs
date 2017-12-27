using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
    [ProtoContract]
    public class TransferProcessedEvent
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
    }
}
