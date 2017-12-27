using ProtoBuf;

namespace Lykke.Job.TxDetector.Commands
{
    [ProtoContract]
    public class ProcessTransferCommand
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
    }
}
