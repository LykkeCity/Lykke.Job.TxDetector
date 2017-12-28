using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Inceptum.Messaging;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Utils;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Microsoft.Rest;

namespace Lykke.Job.TxDetector.Handlers
{
    public class CashInHandler
    {
        [NotNull] private readonly ILog _log;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ICashOperationsRepositoryClient _cashOperationsRepositoryClient;

        public CashInHandler(
            [NotNull] ILog log,
            [NotNull] IMatchingEngineClient matchingEngineClient,
            [NotNull] ICashOperationsRepositoryClient cashOperationsRepositoryClient)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _matchingEngineClient = matchingEngineClient ?? throw new ArgumentNullException(nameof(matchingEngineClient));
            _cashOperationsRepositoryClient = cashOperationsRepositoryClient ?? throw new ArgumentNullException(nameof(cashOperationsRepositoryClient));
        }

        public async Task<CommandHandlingResult> Handle(RegisterCashInOutCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(CashInHandler), nameof(RegisterCashInOutCommand), command.ToJson(), "");
            var id = command.CommandId;
            var asset = command.Asset;
            var amount = command.Amount;
            var transaction = command.Transaction;

            ChaosKitty.Meow();

            try
            {
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
            }
            catch (HttpOperationException)
            {
                var persistedOperation = await _cashOperationsRepositoryClient.GetAsync(transaction.ClientId, id);
                if (persistedOperation == null)
                    throw;
                // else assuming that operation was correctly persisted before
            }

            eventPublisher.PublishEvent(new CashInOutOperationRegisteredEvent
            {
                CommandId = command.CommandId,
                Asset = command.Asset,
                Amount = command.Amount,
                Transaction = command.Transaction
            });

            return CommandHandlingResult.Ok();
        }

        public async Task<CommandHandlingResult> Handle(ProcessCashInCommand command, IEventPublisher eventPublisher)
        {
            await _log.WriteInfoAsync(nameof(CashInHandler), nameof(ProcessCashInCommand), command.ToJson(), "");
            var id = command.CommandId;
            var asset = command.Asset;
            var amount = command.Amount;
            var transaction = command.Transaction;

            ChaosKitty.Meow();

            try
            {
                var responseModel = await _matchingEngineClient.CashInOutAsync(id, transaction.ClientId, asset.Id, amount);
                if (responseModel.Status != MeStatusCodes.Ok && responseModel.Status != MeStatusCodes.AlreadyProcessed && responseModel.Status != MeStatusCodes.Duplicate)
                {
                    await _log.WriteWarningAsync(nameof(CashInHandler), nameof(ProcessCashInCommand), command.ToJson(), responseModel.ToJson());
                    throw new ProcessingException(responseModel.ToJson());
                }
            }
            catch (ArgumentException ex)
            {
                // assuming that ArgumentException is an exception than should be converted into MeStatusCodes.Duplicate
                await _log.WriteWarningAsync(nameof(CashInHandler), nameof(ProcessCashInCommand), command.ToJson(), $"ArgumentException from ME. Treating this as a MeStatusCodes.Duplicate: {ex.Message}");
            }

            ChaosKitty.Meow();

            eventPublisher.PublishEvent(new TransactionProcessedEvent { ClientId = command.Transaction.ClientId, Asset = command.Asset, Amount = command.Amount });

            return CommandHandlingResult.Ok();
        }
    }
}