using ProtoBuf;

namespace Lykke.Job.TxDetector.Models
{
    [ProtoContract]
    public class Asset
    {
        [ProtoMember(1)]
        public string Id { get; set; }
        [ProtoMember(2)]
        public int Accuracy { get; set; }
    }
}
