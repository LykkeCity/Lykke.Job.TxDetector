using System;
using System.Linq;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Services.BitCoin;

namespace Lykke.Job.TxDetector.Services.BitCoin.Ninja
{
    public static class NinjaUtils
    {
        public static IObsoleteBlockchainTransaction ObsoleteConvertToBlockchainTransaction(this TransactionContract item)
        {
            if (IsColoredTransaction(item.ReceivedCoins, item.SpentCoins))
            {
                var receivedCoins = item.ReceivedCoins.GetColoredOnly(x => x.TransactionId == item.TransactionId);
                var spentCoins = item.SpentCoins.GetColoredOnly(x => x.TransactionId == item.TransactionId);

                //skip if received/spent coins has other tx id => it's just a related colored operation
                if (!Enumerable.Any<BitCoinInOut>(receivedCoins) && !Enumerable.Any<BitCoinInOut>(spentCoins))
                    return null;

                var clrTrans = item.ReceivedCoins?.FirstOrDefault() ?? item.SpentCoins.FirstOrDefault();

                return new ObsoleteBlockchainTransaction
                {
                    AssetId = clrTrans?.AssetId,
                    DateTime = item.Block?.BlockTime ?? item.FirstSeen,
                    TxId = clrTrans?.TransactionId,
                    Amount = Enumerable.Sum<BitCoinInOut>(receivedCoins, itm => itm.Quantity.Value) - Enumerable.Sum<BitCoinInOut>(spentCoins, itm => itm.Quantity.Value),
                    Confirmations = item.Block?.Confirmations ?? 0,
                    BlockId = item.Block?.BlockId,
                    Height = item.Block?.Height ?? 0
                };
            }

            double amount = item.ReceivedCoins?.Sum(x => x.Value) ?? 0;
            amount -= item.SpentCoins?.Sum(x => x.Value) ?? 0;

            return new ObsoleteBlockchainTransaction
            {
                DateTime = item.FirstSeen,
                TxId = item.TransactionId,
                Amount = amount,
                Confirmations = item.Block?.Confirmations ?? 0,
                BlockId = item.Block?.BlockId,
                Height = item.Block?.Height ?? 0

            };
        }

        public static IObsoleteBlockchainTransaction ObsoleteConvertToBlockchainTransaction(this BitcoinAddressOperation item, string address)
        {
            if (IsColoredTransaction(item.ReceivedCoins, item.SpentCoins))
            {
                var receivedCoins = item.ReceivedCoins.GetColoredOnly(x => x.TransactionId == item.TransactionId);
                var spentCoins = item.SpentCoins.GetColoredOnly(x => x.TransactionId == item.TransactionId);

                //skip if received/spent coins has other tx id => it's just a related colored operation
                if (!Enumerable.Any<BitCoinInOut>(receivedCoins) && !Enumerable.Any<BitCoinInOut>(spentCoins))
                    return null;

                var clrTrans = Enumerable.FirstOrDefault<BitCoinInOut>(receivedCoins) ?? Enumerable.FirstOrDefault<BitCoinInOut>(spentCoins);

                return new ObsoleteBlockchainTransaction
                {
                    AssetId = clrTrans.AssetId,
                    DateTime = item.FirstSeen,
                    TxId = clrTrans.TransactionId,
                    Amount = Enumerable.Sum<BitCoinInOut>(receivedCoins, itm => itm.Quantity.Value) - Enumerable.Sum<BitCoinInOut>(spentCoins, itm => itm.Quantity.Value),
                    Address = address,
                    Confirmations = item.Confirmations,
                    BlockId = item.BlockId,
                    Height = item.Height
                };
            }

            if (item.Amount != 0)
                return new ObsoleteBlockchainTransaction
                {
                    DateTime = item.FirstSeen,
                    TxId = item.TransactionId,
                    Amount = item.Amount,
                    Address = address,
                    Confirmations = item.Confirmations,
                    BlockId = item.BlockId,
                    Height = item.Height

                };

            return null;

        }

        public static BlockchainTransaction ConvertToBlockchainTransaction(this BitcoinAddressOperation item, string address)
        {
            return new BlockchainTransaction
            {
                Confirmations = item.Confirmations,
                Hash = item.TransactionId,
                BlockId = item.BlockId,
                Height = item.Height,
                ReceivedCoins = item.ReceivedCoins.Select(
                    x => new InputOutput
                    {
                        Address = x.Address,
                        Amount = x.Quantity ?? x.Value,
                        BcnAssetId = x.AssetId
                    }).ToArray(),
                SpentCoins = item.SpentCoins.Select(
                    x => new InputOutput
                    {
                        Address = x.Address,
                        Amount = x.Quantity ?? x.Value,
                        BcnAssetId = x.AssetId
                    }).ToArray()
            };
        }

        private static bool IsColoredTransaction(BitCoinInOut[] receivedCoins, BitCoinInOut[] spentCoins)
        {
            return Enumerable.Any<BitCoinInOut>(receivedCoins.GetColoredOnly()) || Enumerable.Any<BitCoinInOut>(spentCoins.GetColoredOnly());
        }

        private static BitCoinInOut[] GetColoredOnly(this BitCoinInOut[] coins,
            Func<BitCoinInOut, bool> whereClause = null)
        {
            var result = coins.Where(itm => !string.IsNullOrEmpty(itm.AssetId));
            if (whereClause != null)
                result = result.Where(whereClause);
            return result.ToArray();
        }

    }
}