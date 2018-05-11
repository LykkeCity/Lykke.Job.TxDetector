using System;
using System.Threading.Tasks;

namespace Lykke.Job.TxDetector.Core.Services.Clients
{
    public interface IClientAccount
    {
        string Email { get; }
        string NotificationsId { get; }
    }

    public interface IClientAccounts
    {
        Task<IClientAccount> GetByIdAsync(string id);
    }
}