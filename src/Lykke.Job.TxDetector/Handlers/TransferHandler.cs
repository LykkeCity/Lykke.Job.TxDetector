using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Domain.PaymentSystems;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Handlers
{
    public class TransferHandler
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;

        public TransferHandler([NotNull] IPaymentTransactionsRepository paymentTransactionsRepository)
        {
            _paymentTransactionsRepository = paymentTransactionsRepository ?? throw new ArgumentNullException(nameof(paymentTransactionsRepository));
        }

        public async Task<CommandHandlingResult> Handle(ProcessTransferCommand command, IEventPublisher eventPublisher)
        {
            ChaosKitty.Meow();

            if (await _paymentTransactionsRepository.SetStatus(command.TransferId, PaymentStatus.NotifyProcessed) != null)
            {
				eventPublisher.PublishEvent(new TransferProcessedEvent { TransferId = command.TransferId });
            }

            return CommandHandlingResult.Ok();
        }
    }
}
