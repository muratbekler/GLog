using System;
using System.Threading.Tasks;

namespace GLog.Extensions.Logging
{
    public interface IGLogClient : IDisposable
    {
        Task SendMessageAsync(GMessage message);
    }
}
