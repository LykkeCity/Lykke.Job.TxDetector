using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Commands
{
    [ProtoContract]
    public class HandleTransferCommand
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
        [ProtoMember(2)]
        public string ClientId { get; set; }
    }
}
