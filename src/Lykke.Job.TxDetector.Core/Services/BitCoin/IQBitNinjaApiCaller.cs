using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using QBitNinja.Client.Models;

namespace Lykke.Job.TxDetector.Core.Services.BitCoin
{
    public interface IQBitNinjaApiCaller
    {
        Task<BalanceModel> GetAddressBalance(string walletAddress, bool colored = true, bool unspentonly = true);
        Task<BalanceSummary> GetBalanceSummary(string walletAddress);
        Task<GetTransactionResponse> GetTransaction(string hash);
        Task<GetBlockResponse> GetBlock(int blockHeight);
        Task<int> GetCurrentBlockNumber();
    }
}
