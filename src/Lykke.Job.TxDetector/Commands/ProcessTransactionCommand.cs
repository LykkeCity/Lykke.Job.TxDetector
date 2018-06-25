using Lykke.Job.TxDetector.Core.Services.BitCoin;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Commands
{
    [ProtoContract]
    public class ProcessTransactionCommand
    {
        [ProtoMember(1)]
        public string TransactionHash { get; set; }
        [ProtoMember(2)]
        public IBlockchainTransaction BlockchainTransaction { get; set; }
    }
}
