using HPParking.Core.Models.Common;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IServerHealthService
    {
        Task<ServerHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
    }
}
