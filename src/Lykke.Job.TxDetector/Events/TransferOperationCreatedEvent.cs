using System;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Events
{
    [Obsolete("Class is not deleted now only for compatibility purpose. Should be deleted after next release.")]
    [ProtoContract]
    public class TransferOperationCreatedEvent
    {
        [ProtoMember(1)]
        public string TransferId { get; set; }
    }
}
