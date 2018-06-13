using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.TxDetector.Commands;
using Lykke.Job.TxDetector.Core;
using Lykke.Job.TxDetector.Core.Domain.BitCoin;
using Lykke.Job.TxDetector.Core.Domain.BitCoin.Ninja;
using Lykke.Job.TxDetector.Core.Services.BitCoin;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Handlers
{
    public class TransactionHandler
    {
        private static readonly TimeSpan RetryTimeoutForTransactionConfirmations = TimeSpan.FromSeconds(10);

        private readonly ILog _log;
        private readonly AppSettings.TxDetectorSettings _settings;
        private readonly IBalanceChangeTransactionsRepository _balanceChangeTransactionsRepository;
        private readonly IConfirmedTransactionsRepository _confirmedTransactionsRepository;
        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;

        public TransactionHandler(
            [NotNull] ILog log,
            [NotNull] IBalanceChangeTransactionsRepository balanceChangeTransactionsRepository,
            [NotNull] IConfirmedTransactionsRepository confirmedTransactionsRepository,
            [NotNull] AppSettings.TxDetectorSettings settings,
            [NotNull] IQBitNinjaApiCaller qBitNinjaApiCaller)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller ?? throw new ArgumentNullException(nameof(qBitNinjaApiCaller));
            _log = log.CreateComponentScope(nameof(TransactionHandler));
            _balanceChangeTransactionsRepository = balanceChangeTransactionsRepository ?? throw new ArgumentNullException(nameof(balanceChangeTransactionsRepository));
            _confirmedTransactionsRepository = confirmedTransactionsRepository ?? throw new ArgumentNullException(nameof(confirmedTransactionsRepository));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        // entry point
        public async Task<CommandHandlingResult> Handle(ProcessTransactionCommand command, IEventPublisher eventPublisher)
        {
            var confirmations = (await _qBitNinjaApiCaller.GetTransaction(command.TransactionHash))?.Block?.Confirmations;

            var isConfirmed = confirmations >= _settings.TxDetectorConfirmationsLimit;
            if (!isConfirmed)
            {
                //put back if not confirmed yet
                return new CommandHandlingResult { Retry = true, RetryDelay = (long)RetryTimeoutForTransactionConfirmations.TotalMilliseconds };
            }

            ChaosKitty.Meow();

            var hash = command.TransactionHash;
            var balanceChangeTransactions = await _balanceChangeTransactionsRepository.GetAsync(hash);

            foreach (var tx in balanceChangeTransactions)
            {
                var alreadyProcessed = !await _confirmedTransactionsRepository.SaveConfirmedIfNotExist(hash, tx.ClientId);
                if (alreadyProcessed)
                {
                    _log.WriteInfo(nameof(ProcessTransactionCommand), command,
                        $"Transaction with hash {hash} for client {tx.ClientId} is already processed; ignoring it.");
                    continue;
                }

                eventPublisher.PublishEvent(new ConfirmationSavedEvent { TransactionHash = hash, ClientId = tx.ClientId });
            }
            return CommandHandlingResult.Ok();
        }
    }
}