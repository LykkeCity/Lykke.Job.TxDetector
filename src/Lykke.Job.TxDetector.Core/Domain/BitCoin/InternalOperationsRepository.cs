using System;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.BitCoin
{
    public interface IInternalOperation
    {
        Guid TransactionId { get; }
        string Hash { get; }
        string CommandType { get; }
        string[] OperationIds { get; set; }
    }

    public interface IInternalOperationsRepository
    {
        Task<IInternalOperation> GetAsync(string hash);
    }
}