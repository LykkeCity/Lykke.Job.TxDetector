using System;
using Newtonsoft.Json;

namespace Lykke.Job.TxDetector.Services.BitCoin.Ninja
{
    public static class WhatIsItTypes
    {
        public const string PUBKEY_ADDRESS = "PUBKEY_ADDRESS";
        public const string SCRIPT_ADDRESS = "SCRIPT_ADDRESS";
        public const string SECRET_KEY = "SECRET_KEY";
        public const string EXT_PUBLIC_KEY = "EXT_PUBLIC_KEY";
        public const string EXT_SECRET_KEY = "EXT_SECRET_KEY";
        public const string ENCRYPTED_SECRET_KEY_EC = "ENCRYPTED_SECRET_KEY_EC";
        public const string ENCRYPTED_SECRET_KEY_NO_EC = "ENCRYPTED_SECRET_KEY_NO_EC";
        public const string PASSPHRASE_CODE = "PASSPHRASE_CODE";
        public const string CONFIRMATION_CODE = "CONFIRMATION_CODE";
        public const string STEALTH_ADDRESS = "STEALTH_ADDRESS";
        public const string ASSET_ID = "ASSET_ID";
        public const string COLORED_ADDRESS = "COLORED_ADDRESS";
        public const string WITNESS_P2WPKH = "WITNESS_P2WPKH";
        public const string WITNESS_P2WSH = "WITNESS_P2WSH";
        public const string MAX_BASE58_TYPES = "MAX_BASE58_TYPES";
    }

    public class WhatIsItContract
    {
        [JsonProperty("isP2SH")]
        public bool IsP2SH { get; set; }

        [JsonProperty("coloredAddress")]
        public string ColoredAddress { get; set; }

        [JsonProperty("uncoloredAddress")]
        public string UncoloredAddress { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class TransactionContract
    {
        public string TransactionId { get; set; }

        [JsonProperty("block")]
        public BlockModel Block { get; set; }

        [JsonProperty("receivedCoins")]
        public BitCoinInOut[] ReceivedCoins { get; set; }

        [JsonProperty("spentCoins")]
        public BitCoinInOut[] SpentCoins { get; set; }

        [JsonProperty("firstSeen")]
        public DateTime FirstSeen { get; set; }

        public class BlockModel
        {
            [JsonProperty("confirmations")]
            public int Confirmations { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("blockId")]
            public string BlockId { get; set; }

            [JsonProperty("blockTime")]
            public DateTime BlockTime { get; set; }
        }
    }

    public class BitCoinInOut
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("scriptPubKey")]
        public string ScriptPubKey { get; set; }

        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        [JsonProperty("quantity")]
        public double? Quantity { get; set; }
    }

    public class BitcoinAddressOperation
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("confirmations")]
        public int Confirmations { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("blockId")]
        public string BlockId { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("receivedCoins")]
        public BitCoinInOut[] ReceivedCoins { get; set; }

        [JsonProperty("spentCoins")]
        public BitCoinInOut[] SpentCoins { get; set; }


        [JsonProperty("firstSeen")]
        public DateTime FirstSeen { get; set; }
    }

    public class BtcAddressModel
    {
        [JsonProperty("operations")]
        public BitcoinAddressOperation[] Operations { get; set; }
    }

    public class BalanceSummaryModel
    {
        public class SummaryData
        {
            [JsonProperty("transactionCount")]
            public int Count { get; set; }

            [JsonProperty("amount")]
            public double Amount { get; set; }

            [JsonProperty("received")]
            public double Recieved { get; set; }

            [JsonProperty("assets")]
            public ColoredSummaryData[] Assets { get; set; }
        }

        public class ColoredSummaryData
        {
            [JsonProperty("asset")]
            public string AssetId { get; set; }

            [JsonProperty("quantity")]
            public double Quantity { get; set; }

            [JsonProperty("received")]
            public double Recieved { get; set; }
        }

        [JsonProperty("unConfirmed")]
        public SummaryData Unconfirmed { get; set; }

        [JsonProperty("confirmed")]
        public SummaryData Confirmed { get; set; }

        [JsonProperty("spendable")]
        public SummaryData Spendable { get; set; }

        [JsonProperty("immature")]
        public SummaryData Immature { get; set; }
    }

    public class BlockModel
    {
        public class AdditionalInfo
        {
            [JsonProperty("blockId")]
            public string BlockId { get; set; }

            [JsonProperty("blockHeader")]
            public string BlockHeader { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("confirmations")]
            public int Confirmations { get; set; }

            [JsonProperty("medianTimePast")]
            public DateTime MedianTimePast { get; set; }

            [JsonProperty("blockTime")]
            public DateTime BlockTime { get; set; }
        }

        [JsonProperty("additionalInformation")]
        public AdditionalInfo AdditionalInformation { get; set; }
    }

}