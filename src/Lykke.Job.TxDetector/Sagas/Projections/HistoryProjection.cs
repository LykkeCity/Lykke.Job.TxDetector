using System.Threading.Tasks;
using Lykke.Job.TxDetector.Sagas.Events;

namespace Lykke.Job.TxDetector.Sagas.Projections
{
    public class HistoryProjection
    {

        public HistoryProjection()
        {
        }

        public async Task Handle(TransactionCreatedEvent evt)
        {            
        }        
    }
}