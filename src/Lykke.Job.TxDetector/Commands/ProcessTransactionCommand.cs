using ProtoBuf;

namespace Lykke.Job.TxDetector.Commands
{
    [ProtoContract]
    public class ProcessTransactionCommand
    {
        [ProtoMember(1)]
        public string TransactionHash { get; set; }
    }
}
