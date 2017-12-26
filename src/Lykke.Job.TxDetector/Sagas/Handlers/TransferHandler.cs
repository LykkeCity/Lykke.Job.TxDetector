using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TxDetector.Core.Domain.PaymentSystems;
using Lykke.Job.TxDetector.Sagas.Commands;

namespace Lykke.Job.TxDetector.Sagas.Handlers
{
    public class TransferHandler
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly IPaymentTransactionEventsLog _paymentTransactionEventsLog;
        private readonly ILog _log;

        public TransferHandler(
            [NotNull] ILog log,
            [NotNull] IPaymentTransactionsRepository paymentTransactionsRepository,
            [NotNull] IPaymentTransactionEventsLog paymentTransactionEventsLog)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _paymentTransactionsRepository = paymentTransactionsRepository ?? throw new ArgumentNullException(nameof(paymentTransactionsRepository));
            _paymentTransactionEventsLog = paymentTransactionEventsLog ?? throw new ArgumentNullException(nameof(paymentTransactionEventsLog));
        }

        public async Task Handle(HandleTransferCommand command)
        {
            await _log.WriteInfoAsync(nameof(TransferHandler), nameof(HandleTransferCommand), command.ToJson(), "");

            ChaosKitty.Meow();

            //not need for offchain
            //await _transferEventsRepository.SetIsSettledIfExistsAsync(clientId, transferId, false);

            if (await _paymentTransactionsRepository.SetStatus(command.TransferId, PaymentStatus.NotifyProcessed) != null)
            {
                ChaosKitty.Meow();

                await _paymentTransactionEventsLog.WriteAsync(PaymentTransactionLogEvent.Create(
                    command.TransferId, 
                    "",
                    "Confirmed", 
                    "Tx Detector"));
            }
        }
    }
}
