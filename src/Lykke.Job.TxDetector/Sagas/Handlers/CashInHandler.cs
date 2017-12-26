using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Sagas.Commands;
using Lykke.Job.TxDetector.Sagas.Events;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;

namespace Lykke.Job.TxDetector.Sagas.Handlers
{
    public class CashInHandler
    {
        [NotNull] private readonly ILog _log;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ICashOperationsRepositoryClient _cashOperationsRepositoryClient;

        public CashInHandler(
            [NotNull] ILog log,
            IMatchingEngineClient matchingEngineClient,
            ICashOperationsRepositoryClient cashOperationsRepositoryClient)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _matchingEngineClient = matchingEngineClient;
            _cashOperationsRepositoryClient = cashOperationsRepositoryClient;
        }

        public async Task Handle(ProcessCashInCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(CashInHandler), nameof(ProcessCashInCommand), command.ToJson(), "");
            var id = command.CommandId;
            var asset = command.Asset;
            var amount = command.Amount;
            var transaction = command.Transaction;

            ChaosKitty.Meow();

            await _cashOperationsRepositoryClient.RegisterAsync(new CashInOutOperation
            {
                Id = id,
                ClientId = transaction.ClientId,
                Multisig = transaction.Multisig,
                AssetId = asset.Id,
                Amount = amount,
                BlockChainHash = transaction.Hash,
                DateTime = DateTime.UtcNow,
                AddressTo = transaction.Multisig,
                State = TransactionStates.SettledOnchain
            });

            ChaosKitty.Meow();

            var responseModel = await _matchingEngineClient.CashInOutAsync(id, transaction.ClientId, asset.Id, amount);
            if(responseModel.Status != MeStatusCodes.Ok)
            {
                // todo: handle ME error
            }

            ChaosKitty.Meow();

            eventPublisher.PublishEvent(new TransactionProcessedEvent { ClientId = command.Transaction.ClientId, Asset = command.Asset, Amount = command.Amount });
        }
    }
}