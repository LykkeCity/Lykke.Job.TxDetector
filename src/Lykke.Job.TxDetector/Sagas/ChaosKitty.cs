using System;

namespace Lykke.Job.TxDetector.Sagas
{
    public static class ChaosKitty
    {
        private static readonly Random Randmom = new Random();
        private const double StateOfChaos = 0.2;

        public static void Meow()
        {
#if DEBUG
            if (Randmom.NextDouble() < StateOfChaos)
                throw new PlatformNotSupportedException();
#endif
        }
    }
}
