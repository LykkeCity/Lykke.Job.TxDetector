using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TxDetector.Core.Domain.PaymentSystems;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Sagas;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Projections
{
    public class EventLogProjection
    {
        private readonly ILog _log;
        private readonly IPaymentTransactionEventsLog _paymentTransactionEventsLog;

        public EventLogProjection(
            [NotNull] ILog log,
            [NotNull] IPaymentTransactionEventsLog paymentTransactionEventsLog)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _paymentTransactionEventsLog = paymentTransactionEventsLog ?? throw new ArgumentNullException(nameof(paymentTransactionEventsLog));
        }

        public async Task Handle(TransferProcessedEvent evt)
        {
            await _log.WriteInfoAsync(nameof(EventLogProjection), nameof(TransferProcessedEvent), evt.ToJson(), "");

            ChaosKitty.Meow();

            await _paymentTransactionEventsLog.WriteAsync(PaymentTransactionLogEvent.Create(
                transactionId: evt.TransferId,
                techData: "",
                message: "Confirmed",
                who: "Tx Detector"));
        }
    }
}
