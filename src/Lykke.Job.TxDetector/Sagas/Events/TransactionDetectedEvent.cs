using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Events
{
    [ProtoContract]
    public class TransactionDetectedEvent
    {
        [ProtoMember(1)]
        public string TransactionHash { get; set; }
    }
}
