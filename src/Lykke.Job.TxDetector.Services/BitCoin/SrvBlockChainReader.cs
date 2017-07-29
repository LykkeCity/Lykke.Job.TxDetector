using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Services.BitCoin.Ninja;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Job.TxDetector.Services.BitCoin
{
    public class SrvNinjaBlockChainReader : ISrvBlockchainReader
    {
        private readonly AppSettings.NinjaSettings _ninjaSettings;
        private const string Base58Symbols = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public SrvNinjaBlockChainReader(AppSettings.NinjaSettings ninjaSettings)
        {
            _ninjaSettings = ninjaSettings;

            if (!_ninjaSettings.Url.EndsWith("/"))
                _ninjaSettings.Url += "/";
        }

        private static async Task<string> DoRequest(string url)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            var webResponse = await webRequest.GetResponseAsync();
            using (var receiveStream = webResponse.GetResponseStream())
            {
                using (var sr = new StreamReader(receiveStream))
                {
                    return await sr.ReadToEndAsync();
                }

            }
        }

        private async Task<T> DoRequest<T>(string url)
        {
            var result = await DoRequest(url);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result);
        }

        public async Task<IEnumerable<IBlockchainTransaction>> GetBalanceChangesByAddressAsync(string address, int? until = null)
        {
            var untilParameter = until != null ? $"&until={until}" : "";
            var data = await DoRequest<BtcAddressModel>($"{_ninjaSettings.Url}balances/{address}?colored=true&from=tip{untilParameter}");

            var result = new List<BlockchainTransaction>();

            foreach (var item in data.Operations)
            {
                result.Add(item.ConvertToBlockchainTransaction(_ninjaSettings.IsMainNet)); ;
            }

            return result;
        }

        public async Task<int?> GetConfirmationsCount(string hash)
        {
            try
            {
                var result = await DoRequest(_ninjaSettings.Url + $"transactions/{hash}?colored=true");

                var contract = Newtonsoft.Json.JsonConvert.DeserializeObject<TransactionContract>(result);

                return contract.Block.Confirmations;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public async Task<int> GetCurrentBlockHeight()
        {
            var result = await DoRequest($"{_ninjaSettings.Url}blocks/tip?headerOnly=true");
            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<BlockModel>(result);

            return model.AdditionalInformation.Height;
        }
    }
}
