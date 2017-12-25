using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Service.Assets.Client.Custom;
using ProtoBuf;

namespace Lykke.Job.TxDetector.Sagas.Commands
{
    [ProtoContract]
    public class ProcessCashInCommand
    {
        [ProtoMember(1)]
        public IBalanceChangeTransaction Transaction { get; set; }
        [ProtoMember(2)]
        public IAsset Asset { get; set; }
        [ProtoMember(3)]
        public double Amount { get; set; }
    }
}
