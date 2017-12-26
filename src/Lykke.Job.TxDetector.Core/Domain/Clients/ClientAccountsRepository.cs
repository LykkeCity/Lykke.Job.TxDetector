using System;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Domain.Clients
{
    public interface IClientAccount
    {
        DateTime Registered { get; }
        string Id { get; }
        string Email { get; }
        string PartnerId { get; }
        string Phone { get; }
        string Pin { get; }
        string NotificationsId { get; }
    }

    public interface IClientAccountsRepository
    {
        Task<IClientAccount> GetByIdAsync(string id);
    }
}