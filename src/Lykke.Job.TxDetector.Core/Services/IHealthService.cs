
namespace Lykke.Job.TxDetector.Core.Services
{
    public interface IHealthService
    {
        string GetHealthViolationMessage();
        string GetHealthWarningMessage();
    }
}