using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Utils;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Messaging;
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
        private readonly IBitcoinCashinRepository _bitcoinCashinRepository;
        private readonly IPostponedCashInRepository _postponedCashInRepository;

        public CashInHandler(
            [NotNull] ILog log,
            [NotNull] IMatchingEngineClient matchingEngineClient,
            [NotNull] ICashOperationsRepositoryClient cashOperationsRepositoryClient,
            [NotNull] IBitcoinCashinRepository bitcoinCashinRepository,
            [NotNull] IPostponedCashInRepository postponedCashInRepository)
        {
            _log = log.CreateComponentScope(nameof(CashInHandler));
            _matchingEngineClient = matchingEngineClient ?? throw new ArgumentNullException(nameof(matchingEngineClient));
            _cashOperationsRepositoryClient = cashOperationsRepositoryClient ?? throw new ArgumentNullException(nameof(cashOperationsRepositoryClient));
            _bitcoinCashinRepository = bitcoinCashinRepository ?? throw new ArgumentNullException(nameof(bitcoinCashinRepository));
            _postponedCashInRepository = postponedCashInRepository ?? throw new ArgumentNullException(nameof(postponedCashInRepository));
        }

        public async Task<CommandHandlingResult> Handle(RegisterCashInOutCommand command, IEventPublisher eventPublisher)
        {
            _log.WriteInfo(nameof(RegisterCashInOutCommand), command, "");
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

        public async Task<CommandHandlingResult> Handle(RegisterBitcoinCashInCommand command, IEventPublisher eventPublisher)
        {
            _log.WriteInfo(nameof(RegisterBitcoinCashInCommand), command, "");
            var id = command.CommandId;
            var transaction = command.Transaction;

            ChaosKitty.Meow();

            await _bitcoinCashinRepository.InsertOrReplaceAsync(id, transaction.ClientId, transaction.Multisig, transaction.Hash, transaction.IsSegwit);

            eventPublisher.PublishEvent(new BitcoinCashInRegisteredEvent
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
            _log.WriteInfo(nameof(ProcessCashInCommand), command, "");
            var id = command.CommandId;
            var asset = command.Asset;
            var amount = command.Amount;
            var transaction = command.Transaction;

            ChaosKitty.Meow();

            var responseModel = await _matchingEngineClient.CashInOutAsync(id, transaction.ClientId, asset.Id, amount);
            if (responseModel.Status != MeStatusCodes.Ok && responseModel.Status != MeStatusCodes.AlreadyProcessed && responseModel.Status != MeStatusCodes.Duplicate)
            {
                _log.WriteInfo(nameof(ProcessCashInCommand), command, responseModel.ToJson());
                throw new ProcessingException(responseModel.ToJson());
            }

            ChaosKitty.Meow();

            eventPublisher.PublishEvent(new TransactionProcessedEvent { ClientId = command.Transaction.ClientId, Asset = command.Asset, Amount = command.Amount });

            return CommandHandlingResult.Ok();
        }


        public async Task<CommandHandlingResult> Handle(SavePostponedCashInCommand command)
        {
            _log.WriteInfo(nameof(SavePostponedCashInCommand), command, "");

            await _postponedCashInRepository.SaveAsync(command.TransactionHash);

            ChaosKitty.Meow();

            return CommandHandlingResult.Ok();
        }
    }
}