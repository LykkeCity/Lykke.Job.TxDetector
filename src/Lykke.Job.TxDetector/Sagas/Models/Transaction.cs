using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Models
{
    [ProtoContract]
    public class Transaction
    {
        [ProtoMember(1)]
        public string Hash { get; set; }
        [ProtoMember(2)]
        public string ClientId { get; set; }
        [ProtoMember(3)]
        public string Multisig { get; set; }
    }
}
