using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Commands
{
    [ProtoContract]
    public class SendNoRefundDepositDoneMailCommand
    {
        public string Email { get; set; }
        public double Amount { get; set; }
        public string AssetBcnId { get; set; }
    }
}
