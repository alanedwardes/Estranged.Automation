using System.Security.Cryptography;

namespace Estranged.Automation.Runner.Discord
{
    public static class RandomExtensions
    {
        public static bool PercentChance(int percent)
        {
            return RandomNumberGenerator.GetInt32(0, 101) < percent;
        }
    }
}
