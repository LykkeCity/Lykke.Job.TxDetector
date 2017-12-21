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

    public class ClientAccount : IClientAccount
    {
        public DateTime Registered { get; set; }
        public string Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Pin { get; set; }
        public string NotificationsId { get; set; }
        public string PartnerId { get; set; }
        public bool IsReviewAccount { get; set; }

        public static ClientAccount Create(string email, string phone, string partnerId)
        {
            return new ClientAccount
            {
                Email = email,
                Registered = DateTime.UtcNow,
                Phone = phone,
                PartnerId = partnerId
            };
        }
    }

    public interface IClientAccountsRepository
    {
        Task<IClientAccount> GetByIdAsync(string id);
    }
}