using System.Linq;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Lykke.Job.TxDetector.Services.BitCoin.Ninja
{
    public static class NinjaUtils
    {
        public static BlockchainTransaction ConvertToBlockchainTransaction(this BitcoinAddressOperation item, bool isMainNet)
        {
            var network = isMainNet ? Network.Main : Network.RegTest;
            return new BlockchainTransaction
            {
                Confirmations = item.Confirmations,
                Hash = item.TransactionId,
                BlockId = item.BlockId,
                Height = item.Height,
                ReceivedCoins = item.ReceivedCoins.Select(
                    x => new InputOutput
                    {
                        Address = GetScriptFromBytes(x.ScriptPubKey).GetDestinationAddress(network).ToString(),
                        Amount = x.Quantity ?? x.Value,
                        BcnAssetId = x.AssetId
                    }).ToArray(),
                SpentCoins = item.SpentCoins.Select(
                    x => new InputOutput
                    {
                        Address = GetScriptFromBytes(x.ScriptPubKey).GetDestinationAddress(network).ToString(),
                        Amount = x.Quantity ?? x.Value,
                        BcnAssetId = x.AssetId
                    }).ToArray()
            };
        }
        
        private static Script GetScriptFromBytes(string data)
        {
            var bytes = Encoders.Hex.DecodeData(data);
            var script = Script.FromBytesUnsafe(bytes);
            bool hasOps = false;
            var reader = script.CreateReader();
            foreach (var op in reader.ToEnumerable())
            {
                hasOps = true;
                if (op.IsInvalid || (op.Name == "OP_UNKNOWN" && op.PushData == null))
                    return null;
            }
            return !hasOps ? null : script;
        }
    }
}