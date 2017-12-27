using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Domain.PaymentSystems;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Sagas;

namespace Lykke.Job.TxDetector.Handlers
{
    public class TransferHandler
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly ILog _log;

        public TransferHandler(
            [NotNull] ILog log,
            [NotNull] IPaymentTransactionsRepository paymentTransactionsRepository)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _paymentTransactionsRepository = paymentTransactionsRepository ?? throw new ArgumentNullException(nameof(paymentTransactionsRepository));
        }

        public async Task Handle(ProcessTransferCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(TransferHandler), nameof(ProcessTransferCommand), command.ToJson(), "");

            ChaosKitty.Meow();

            //not need for offchain
            //await _transferEventsRepository.SetIsSettledIfExistsAsync(clientId, transferId, false);

            if (await _paymentTransactionsRepository.SetStatus(command.TransferId, PaymentStatus.NotifyProcessed) != null)
            {
                eventPublisher.PublishEvent(new TransferProcessedEvent { TransferId = command.TransferId });
            }
        }
    }
}
