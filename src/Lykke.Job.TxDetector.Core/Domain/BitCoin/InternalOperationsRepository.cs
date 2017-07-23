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

    public class InternalOperation : IInternalOperation
    {
        public Guid TransactionId { get; set; }
        public string Hash { get; set; }
        public string CommandType { get; set; }
        public string[] OperationIds { get; set; }
    }

    public interface IInternalOperationsRepository
    {
        Task InsertAsync(IInternalOperation operation);
        Task<IInternalOperation> GetAsync(string hash);
    }
}