using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Core.Services.Pricing
{
    /// <summary>
    /// Triển khai bộ nhớ đệm an toàn đa luồng (thread-safe in-memory cache) cho biểu phí với thời gian sống mặc định 60 giây
    /// </summary>
    public class TariffCache : ITariffCache
    {
        private readonly IRepository<Tariff>? _tariffRepository;
        private readonly ConcurrentDictionary<string, (Tariff Tariff, DateTime ExpiresAt)> _cache = new();
        private readonly TimeSpan _ttl;

        public TariffCache(IRepository<Tariff>? tariffRepository = null, TimeSpan? ttl = null)
        {
            _tariffRepository = tariffRepository;
            _ttl = ttl ?? TimeSpan.FromSeconds(60);
        }

        public async Task<Tariff?> GetCachedTariffAsync(string? tariffId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tariffId)) return null;

            if (_cache.TryGetValue(tariffId, out var cached) && DateTime.UtcNow < cached.ExpiresAt)
            {
                return cached.Tariff;
            }

            if (_tariffRepository == null) return null;

            var tariff = await _tariffRepository.GetByIdAsync(tariffId, cancellationToken);
            if (tariff != null)
            {
                _cache[tariffId] = (tariff, DateTime.UtcNow.Add(_ttl));
            }

            return tariff;
        }

        public void Invalidate(string tariffId)
        {
            if (!string.IsNullOrEmpty(tariffId))
            {
                _cache.TryRemove(tariffId, out _);
            }
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
