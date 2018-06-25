using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.TxDetector.Core.Services.ChainalysisStore;
using Lykke.Job.TxDetector.Events;
using Lykke.Job.TxDetector.Utils;

namespace Lykke.Job.TxDetector.Projections
{
    public class ChainalysisStoreProjection
    {
		private readonly ILog _log;
		private readonly IChainalysisStoreService _chainalysisStoreService;

		public ChainalysisStoreProjection(
            [NotNull] ILog log,
			[NotNull] IChainalysisStoreService chainalysisStoreService)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
			_chainalysisStoreService = chainalysisStoreService ?? throw new ArgumentNullException(nameof(chainalysisStoreService));
        }
        
		public async Task Handle(ConfirmationSavedEvent evt)
        {
			
			await _log.WriteInfoAsync(nameof(ChainalysisStoreProjection), nameof(ConfirmationSavedEvent), evt.ToJson(), "");

            ChaosKitty.Meow();

			await _chainalysisStoreService.ProccedAsync(evt.BlockchainTransaction, evt.ClientId, evt.Multisig);
        }
    }
}
