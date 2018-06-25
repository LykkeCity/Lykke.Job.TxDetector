using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Core.Services.ChainalysisStore;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Common.Log;

namespace Lykke.Job.TxDetector.Services.ChainalysisStore
{
    public class ChainalysisStoreService : IChainalysisStoreService
    {
        private readonly RabbitMqPublisher<ChainalisysCashMessage> _rabbitMqPublisher;

        
        public ChainalysisStoreService(ILog log, string rabbitUrl, string exchangeName)
        {
            if(log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (string.IsNullOrEmpty(rabbitUrl))
            {
                throw new ArgumentNullException(nameof(rabbitUrl));
            }

            if (string.IsNullOrEmpty(exchangeName))
            {
                throw new ArgumentNullException(nameof(exchangeName)); 
            }


            var settings = RabbitMqSubscriptionSettings.CreateForPublisher(rabbitUrl, exchangeName);
            settings.IsDurable = true;



            _rabbitMqPublisher = new RabbitMqPublisher<ChainalisysCashMessage>(settings)
                                        .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                                        .SetSerializer(new JsonMessageSerializer<ChainalisysCashMessage>())
                                        .DisableInMemoryQueuePersistence()
                                        .SetLogger(log);
                                        
        }

        public async Task ProccedAsync(IBlockchainTransaction blockchainTransaction, string clientId, string walletAddress)
        {

            var rec = from r in blockchainTransaction.ReceivedCoins
                      where r.Address.Equals(walletAddress)
                      select r;

            _rabbitMqPublisher.Start();
            var tasks = new List<Task>();
            foreach(var r in rec)
            {
                tasks.Add(_rabbitMqPublisher.ProduceAsync(new ChainalisysCashMessage{
                    LwClientId = clientId,
                    BtcTransactionHash = blockchainTransaction.Hash,
                    WalletAddress = walletAddress,
                    OutputNumber = (int)r.Output,
                    Amount = (decimal)r.Amount
                }));
            }

            await Task.WhenAll(tasks.ToArray());
            
            _rabbitMqPublisher.Stop();
        }
    }
}
