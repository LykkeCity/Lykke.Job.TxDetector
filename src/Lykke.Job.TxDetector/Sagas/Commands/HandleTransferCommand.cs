using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Commands
{
    [ProtoContract]
    public class HandleTransferCommand
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
    }
}
