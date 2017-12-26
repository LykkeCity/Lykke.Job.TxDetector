using System;
using System.Threading.Tasks;
using AzureStorage;
using Common.PasswordTools;
using Lykke.Job.TxDetector.Core.Domain.Clients;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.TxDetector.AzureRepositories.Clients
{
    public class ClientAccountEntity : TableEntity, IClientAccount, IPasswordKeeping
    {
        public static string GeneratePartitionKey()
        {
            return "Trader";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }
        
        public DateTime Registered { get; set; }
        public string Id => RowKey;
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Pin { get; set; }
        public string NotificationsId { get; set; }
        public string Salt { get; set; }
        public string Hash { get; set; }
        public string PartnerId { get; set; }
        public bool IsReviewAccount { get; set; }
    }

    public class ClientsRepository : IClientAccountsRepository
    {
        private readonly INoSQLTableStorage<ClientAccountEntity> _clientsTablestorage;

        public ClientsRepository(INoSQLTableStorage<ClientAccountEntity> clientsTablestorage)
        {
            _clientsTablestorage = clientsTablestorage;
        }

        public async Task<IClientAccount> GetByIdAsync(string id)
        {
            var partitionKey = ClientAccountEntity.GeneratePartitionKey();
            var rowKey = ClientAccountEntity.GenerateRowKey(id);

            return await _clientsTablestorage.GetDataAsync(partitionKey, rowKey);
        }
    }
}