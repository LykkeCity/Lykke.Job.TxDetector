using Lykke.Job.TxDetector.Core.Services;

namespace Lykke.Job.TxDetector.Services
{
    public class HealthService : IHealthService
    {
        public string GetHealthViolationMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health violation message, or NULL if job hasn't critical errors
            return null;
        }

        public string GetHealthWarningMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health warning message, or NULL if job is ok
            return null;
        }

    }
}