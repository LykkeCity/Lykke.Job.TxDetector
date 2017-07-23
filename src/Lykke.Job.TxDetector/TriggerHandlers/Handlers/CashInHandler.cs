using System;
using System.Threading.Tasks;
using Common;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.CashOperations;
using Lykke.Job.TxDetector.Core.Domain.Clients;
using Lykke.Job.TxDetector.Core.Services.Messages;
using Lykke.Job.TxDetector.Core.Services.Notifications;
using Lykke.Job.TxDetector.Resources;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Job.TxDetector.TriggerHandlers.Handlers
{
    public class CashInHandler
    {
        private readonly IAppNotifications _appNotifications;
        private readonly IMatchingEngineConnector _matchingEngineConnector;
        private readonly ICashOperationsRepository _cashOperationsRepository;
        private readonly IClientAccountsRepository _clientAccountsRepository;
        private readonly ISrvEmailsFacade _srvEmailsFacade;
        private readonly IClientSettingsRepository _clientSettingsRepository;

        public CashInHandler(
            IAppNotifications appNotifications,
            IMatchingEngineConnector matchingEngineConnector,
            ICashOperationsRepository cashOperationsRepository,
            IClientAccountsRepository clientAccountsRepository,
            ISrvEmailsFacade srvEmailsFacade,
            IClientSettingsRepository clientSettingsRepository)
        {
            _appNotifications = appNotifications;
            _matchingEngineConnector = matchingEngineConnector;
            _cashOperationsRepository = cashOperationsRepository;
            _clientAccountsRepository = clientAccountsRepository;
            _srvEmailsFacade = srvEmailsFacade;
            _clientSettingsRepository = clientSettingsRepository;
        }

        public async Task<double> HandleCashInOperation(IBalanceChangeTransaction balanceChangeTx, IAsset asset, double amount)
        {
            var id = Guid.NewGuid().ToString("N");

            await _matchingEngineConnector.CashInOutAsync(id, balanceChangeTx.ClientId, asset.Id, amount);

            await _cashOperationsRepository.RegisterAsync(new CashInOutOperation
            {
                Id = id,
                ClientId = balanceChangeTx.ClientId,
                Multisig = balanceChangeTx.Multisig,
                AssetId = asset.Id,
                Amount = amount,
                BlockChainHash = balanceChangeTx.Hash,
                DateTime = DateTime.UtcNow,
                AddressTo = balanceChangeTx.Multisig,
                State = TransactionStates.SettledOnchain
            });

            var clientAcc = await _clientAccountsRepository.GetByIdAsync(balanceChangeTx.ClientId);
            await _srvEmailsFacade.SendNoRefundDepositDoneMail(clientAcc.Email, amount, asset.Id);

            var pushSettings = await _clientSettingsRepository.GetSettings<PushNotificationsSettings>(balanceChangeTx.ClientId);
            if (pushSettings.Enabled)
                await
                    _appNotifications.SendTextNotificationAsync(new[] { clientAcc.NotificationsId },
                        NotificationType.TransactionConfirmed,
                        string.Format(TextResources.CashInSuccessText,
                            amount.GetFixedAsString(asset.Accuracy), asset.Id));

            return amount;
        }

    }
}