using System;
using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace Lykke.Job.TxDetector.Services.BitCoin
{
    public class QBitNinjaApiCaller : IQBitNinjaApiCaller
    {
        private readonly Func<QBitNinjaClient> _clientFactory;

        public QBitNinjaApiCaller(Func<QBitNinjaClient> clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<BalanceModel> GetAddressBalance(string walletAddress, bool colored = true, bool unspentonly = true)
        {
            var client = _clientFactory();
            client.Colored = colored;
            return await client.GetBalance(BitcoinAddress.Create(walletAddress), unspentonly);
        }

        public async Task<BalanceSummary> GetBalanceSummary(string walletAddress)
        {
            var client = _clientFactory();
            client.Colored = true;
            return await client.GetBalanceSummary(BitcoinAddress.Create(walletAddress));
        }

        public Task<GetTransactionResponse> GetTransaction(string hash)
        {
            var client = _clientFactory();
            client.Colored = true;
            return client.GetTransaction(uint256.Parse(hash));
        }

        public Task<GetBlockResponse> GetBlock(int blockHeight)
        {
            var client = _clientFactory();

            return client.GetBlock(new BlockFeature(blockHeight));
        }

        public async Task<int> GetCurrentBlockNumber()
        {
            var client = _clientFactory();

            return (await client.GetBlock(new BlockFeature(SpecialFeature.Last), true)).AdditionalInformation.Height;
        }
    }
}
