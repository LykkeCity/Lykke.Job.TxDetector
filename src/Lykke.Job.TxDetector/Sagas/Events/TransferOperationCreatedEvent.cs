using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Events
{
    [ProtoContract]
    public class TransferOperationCreatedEvent
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
        [ProtoMember(2)]
        public string ClientId { get; set; }
    }
}
