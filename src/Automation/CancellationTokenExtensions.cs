using Discord;
using System.Threading;

namespace Estranged.Automation
{
    public static class CancellationTokenExtensions
    {
        public static RequestOptions ToRequestOptions(this CancellationToken token) => new RequestOptions
        {
            CancelToken = token
        };
    }
}
