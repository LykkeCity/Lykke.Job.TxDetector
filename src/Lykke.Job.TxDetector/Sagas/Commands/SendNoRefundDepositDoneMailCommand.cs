using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Commands
{
    [ProtoContract]
    public class SendNoRefundDepositDoneMailCommand
    {
        [ProtoMember(1)]
        public string Email { get; set; }
        [ProtoMember(2)]
        public double Amount { get; set; }
        [ProtoMember(3)]
        public string AssetId { get; set; }
    }
}
