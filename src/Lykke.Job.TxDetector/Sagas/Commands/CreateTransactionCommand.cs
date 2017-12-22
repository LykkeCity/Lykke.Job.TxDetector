using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Commands
{
    [ProtoContract]
    public class CreateTransactionCommand
    {
        [ProtoMember(1)]
        public string OrderId { get; set; }
    }
}