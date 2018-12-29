using System.Threading.Tasks;

namespace Estranged.Automation.Shared
{
    public interface IRateLimitingRepository
    {
        Task<bool> IsWithinLimit(string resourceId, int limit);
    }
}