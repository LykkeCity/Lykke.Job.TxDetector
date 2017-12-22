using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Sagas.Commands;
using Lykke.Job.TxDetector.Sagas.Events;

namespace Lykke.Job.TxDetector.Sagas
{
    public class TradeSaga
    {
        private readonly ILog _log;

        public TradeSaga(
            ILog log)
        {
            _log = log;
        }

        private async Task Handle(TransactionCreatedEvent evt, ICommandSender sender)
        {
            await _log.WriteInfoAsync(nameof(TradeSaga), nameof(TransactionCreatedEvent), evt.ToJson());
            
            var cmd = new CreateTransactionCommand
            {
                OrderId = evt.OrderId
            };

            sender.SendCommand(cmd, "tx-handler");
        }
    }
}