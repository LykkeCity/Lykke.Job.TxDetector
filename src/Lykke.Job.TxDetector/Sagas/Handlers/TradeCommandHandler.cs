using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Sagas.Commands;
using Lykke.Job.TxDetector.Sagas.Events;

namespace Lykke.Job.TxDetector.Sagas.Handlers
{
    public class TradeCommandHandler
    {
        private readonly ILog _log;

        public TradeCommandHandler(
            ILog log)
        {
            _log = log;
        }

        public async Task Handle(CreateTransactionCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(TradeCommandHandler), nameof(CreateTransactionCommand), command.ToJson());

            eventPublisher.PublishEvent(new TransactionCreatedEvent { OrderId = command.OrderId });
        }
    }
}