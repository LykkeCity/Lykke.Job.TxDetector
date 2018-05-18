using System;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Services.Clients
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

    public interface IClientAccounts
    {
        Task<IClientAccount> GetByIdAsync(string id);
    }
}