using System;

namespace Lykke.Job.TxDetector.Sagas
{
    public static class ChaosKitty
    {
        private static readonly Random Randmom = new Random();

        public static double StateOfChaos = 0.0;

        public static void Meow()
        {
            if (Randmom.NextDouble() < StateOfChaos)
                throw new Exception("Meow");
        }
    }
}
