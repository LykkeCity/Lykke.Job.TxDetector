using ProtoBuf;

namespace Lykke.Job.TxDetector.Commands
{
    [ProtoContract]
    public class SavePostponedCashInCommand
    {
        [ProtoMember(1)]
        public string TransactionHash { get; set; }
    }
}
