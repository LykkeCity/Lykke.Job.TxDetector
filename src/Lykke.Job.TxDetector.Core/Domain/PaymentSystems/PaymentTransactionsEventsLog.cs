using System;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.PaymentSystems
{
    public interface IPaymentTransactionLogEvent
    {
        string PaymentTransactrionId { get; }
        DateTime DateTime { get; }

        /// <summary>
        /// We have for shit cleaning processes
        /// </summary>
        string TechData { get; }

        /// <summary>
        /// We have for backoffice and other reports
        /// </summary>
        string Message { get; }

        string Who { get; }

    }

    public class PaymentTransactionLogEvent : IPaymentTransactionLogEvent
    {

        public string PaymentTransactrionId { get; set; }
        public DateTime DateTime { get; set; }
        public string TechData { get; set; }
        public string Message { get; set; }
        public string Who { get; set; }

        public static PaymentTransactionLogEvent Create(string transactionId, string techData, string message,
            string who)
        {
            return new PaymentTransactionLogEvent
            {
                PaymentTransactrionId = transactionId,
                DateTime = DateTime.UtcNow,
                Message = message,
                TechData = techData,
                Who = who
            };
        }

    }


    /// <summary>
    /// Log within the cetain transaction
    /// </summary>
    public interface IPaymentTransactionEventsLog
    {

        Task WriteAsync(IPaymentTransactionLogEvent newEvent);
    }
}