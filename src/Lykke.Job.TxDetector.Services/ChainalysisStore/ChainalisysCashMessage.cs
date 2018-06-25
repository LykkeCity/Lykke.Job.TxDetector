using System;
namespace Lykke.Job.TxDetector.Services.ChainalysisStore
{

    public class ChainalisysCashMessage
    {
        public string LwClientId { get; set; }
        public string BtcTransactionHash { get; set; }
        public string WalletAddress { get; set; }
        public int OutputNumber { get; set; }
        public decimal Amount { get; set; }
    }

}
