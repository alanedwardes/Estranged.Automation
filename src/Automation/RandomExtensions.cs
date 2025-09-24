using System;
using System.Security.Cryptography;

namespace Estranged.Automation
{
    public static class RandomExtensions
    {
        public static bool PercentChance(float percent)
        {
            return RandomPercent() < percent;
        }

        private static uint RandomUint()
        {
            byte[] random = new byte[4];
            RandomNumberGenerator.Fill(random);
            return BitConverter.ToUInt32(random);
        }

        private static float RandomPercent()
        {
            return RandomUint() / (float)uint.MaxValue * 100f;
        }
    }
}
