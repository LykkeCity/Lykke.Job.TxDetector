using System.Threading.Tasks;
using Lykke.Job.TxDetector.Core.Domain.PaymentSystems;

namespace Lykke.Job.TxDetector.TriggerHandlers.Handlers
{
    public class TransferHandler
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly IPaymentTransactionEventsLog _paymentTransactionEventsLog;

        public TransferHandler(IPaymentTransactionsRepository paymentTransactionsRepository, IPaymentTransactionEventsLog paymentTransactionEventsLog)
        {
            _paymentTransactionsRepository = paymentTransactionsRepository;
            _paymentTransactionEventsLog = paymentTransactionEventsLog;
        }

        public async Task HandleTransferOperation(string transferId, string clientId)
        {
            //not need for offchain
            //await _transferEventsRepository.SetIsSettledIfExistsAsync(clientId, transferId, false);

            if (await _paymentTransactionsRepository.SetStatus(transferId, PaymentStatus.NotifyProcessed) != null)
            {
                await
                    _paymentTransactionEventsLog.WriteAsync(PaymentTransactionLogEvent.Create(transferId, "",
                        "Confirmed", "Tx Detector"));
            }
        }
    }
}