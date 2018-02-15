using System.Linq;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using NBitcoin;
using NBitcoin.DataEncoders;
using QBitNinja.Client.Models;

namespace Lykke.Job.TxDetector.Services.BitCoin.Ninja
{
    public static class NinjaUtils
    {
        public static BlockchainTransaction ConvertToBlockchainTransaction(this GetTransactionResponse item, bool isMainNet, string address)
        {
            var network = isMainNet ? Network.Main : Network.RegTest;
            return new BlockchainTransaction
            {
                Confirmations = item.Block.Confirmations,
                Hash = item.TransactionId.ToString(),
                BlockId = item.Block.BlockId.ToString(),
                Height = item.Block.Height,
                ReceivedCoins = item.ReceivedCoins.Select(
                    x => new InputOutput
                    {
                        Address = x.TxOut.ScriptPubKey.GetDestinationAddress(network)?.ToString(),
                        Amount = (x as ColoredCoin)?.Amount.Quantity ?? ((Coin)x).Amount.Satoshi,
                        BcnAssetId = (x as ColoredCoin)?.AssetId.ToString(network)
                    }).Where(x => x.Address == address).ToArray(),
                SpentCoins = item.SpentCoins.Select(
                    x => new InputOutput
                    {
                        Address = x.TxOut.ScriptPubKey.GetDestinationAddress(network)?.ToString(),
                        Amount = (x as ColoredCoin)?.Amount.Quantity ?? ((Coin)x).Amount.Satoshi,
                        BcnAssetId = (x as ColoredCoin)?.AssetId.ToString(network)
                    }).Where(x => x.Address == address).ToArray()
            };
        }
    }
}