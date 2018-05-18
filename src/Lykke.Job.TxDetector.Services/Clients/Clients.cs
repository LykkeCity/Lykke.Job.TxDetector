using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.PasswordTools;
using Lykke.Job.TxDetector.Core.Services.Clients;
using Newtonsoft.Json;

namespace Lykke.Job.TxDetector.AzureRepositories.Clients
{
    public class ClientAccount : IClientAccount 
    {
        [JsonProperty("Email")]
        public string Email { get; set; }

        [JsonProperty("NotificationsId")]
        public string NotificationsId { get; set; }
    }

    public class Clients : IClientAccounts
    {

        private static HttpClient httpClient = new HttpClient();

        private readonly string _connectionString;

        public Clients(string connectionString)
        {
            _connectionString = connectionString;

            httpClient.BaseAddress = new Uri($"{_connectionString}/api/ClientAccountInformation/getClientById");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public async Task<IClientAccount> GetByIdAsync(string id)
        {
            IClientAccount client = null;

            HttpResponseMessage response = await httpClient.GetAsync("?id=" + id);
            if (response.IsSuccessStatusCode)
            {
                client = await response.Content.ReadAsAsync<ClientAccount>();
            }

            return client;
        }

    }
}