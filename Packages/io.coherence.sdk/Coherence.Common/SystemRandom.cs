// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;

    public class SystemRandom : IRandom
    {
        private readonly Random random;

        public SystemRandom(Random random = null)
        {
            this.random = random ?? new Random();
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }

        public double NextNormalDistribution(double mean, double deviation)
        {
            double u1 = 1.0 - random.NextDouble(); //uniform (0, 1] random doubles
            double u2 = 1.0 - random.NextDouble();

            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal (0, 1)

            double randNormal = (mean + deviation * randStdNormal); //random normal (mean, stdDev^2)

            return randNormal;
        }
    }
}
