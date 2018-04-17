using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Shared
{
    public interface ISeenItemRepository
    {
        Task<string[]> GetSeenItems(string[] items, CancellationToken token);
        Task SetItemSeen(string item, CancellationToken token);
    }
}