using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Core.Services.Pricing;
using NSubstitute;
using Xunit;

namespace HPParking.Tests.Core
{
    public class TariffCacheTests
    {
        private readonly IRepository<Tariff> _tariffRepo = Substitute.For<IRepository<Tariff>>();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetCachedTariffAsync_WhenTariffIdIsNullOrEmpty_ReturnsNullWithoutCallingRepo(string? tariffId)
        {
            var cache = new TariffCache(_tariffRepo);

            var result = await cache.GetCachedTariffAsync(tariffId);

            Assert.Null(result);
            await _tariffRepo.DidNotReceive().GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCachedTariffAsync_WhenRepoIsNull_ReturnsNull()
        {
            var cache = new TariffCache(tariffRepository: null);

            var result = await cache.GetCachedTariffAsync("tariff-1");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCachedTariffAsync_WhenCalledFirstTime_FetchesFromRepoAndCaches()
        {
            var expectedTariff = new Tariff
            {
                Id = "tariff-1",
                Code = "T01",
                Name = "Vé lượt",
                VehicleType = VehicleType.Car,
                IsActive = true
            };
            _tariffRepo.GetByIdAsync("tariff-1", Arg.Any<CancellationToken>()).Returns(expectedTariff);

            var cache = new TariffCache(_tariffRepo, TimeSpan.FromSeconds(60));

            // Lần 1: Gọi repo
            var result1 = await cache.GetCachedTariffAsync("tariff-1");
            // Lần 2: Lấy từ cache
            var result2 = await cache.GetCachedTariffAsync("tariff-1");

            Assert.NotNull(result1);
            Assert.Equal("tariff-1", result1.Id);
            Assert.Same(result1, result2);

            // Repository chỉ được gọi đúng 1 lần
            await _tariffRepo.Received(1).GetByIdAsync("tariff-1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetCachedTariffAsync_WhenTtlExpired_RefetchesFromRepo()
        {
            var tariff = new Tariff { Id = "tariff-1", Name = "Tariff 1", IsActive = true };
            _tariffRepo.GetByIdAsync("tariff-1", Arg.Any<CancellationToken>()).Returns(tariff);

            // TTL âm để cache hết hạn ngay lập tức
            var cache = new TariffCache(_tariffRepo, TimeSpan.FromMilliseconds(-1));

            var result1 = await cache.GetCachedTariffAsync("tariff-1");
            var result2 = await cache.GetCachedTariffAsync("tariff-1");

            Assert.NotNull(result1);
            Assert.NotNull(result2);

            // Vì TTL hết hạn, repository được gọi 2 lần
            await _tariffRepo.Received(2).GetByIdAsync("tariff-1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Invalidate_RemovesItemFromCache()
        {
            var tariff = new Tariff { Id = "tariff-1", Name = "Tariff 1", IsActive = true };
            _tariffRepo.GetByIdAsync("tariff-1", Arg.Any<CancellationToken>()).Returns(tariff);

            var cache = new TariffCache(_tariffRepo, TimeSpan.FromSeconds(60));

            await cache.GetCachedTariffAsync("tariff-1");
            cache.Invalidate("tariff-1");
            await cache.GetCachedTariffAsync("tariff-1");

            // Sau khi Invalidate, lần gọi tiếp theo phải query lại DB
            await _tariffRepo.Received(2).GetByIdAsync("tariff-1", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Clear_RemovesAllItemsFromCache()
        {
            var tariff1 = new Tariff { Id = "t-1", Name = "Tariff 1", IsActive = true };
            var tariff2 = new Tariff { Id = "t-2", Name = "Tariff 2", IsActive = true };
            _tariffRepo.GetByIdAsync("t-1", Arg.Any<CancellationToken>()).Returns(tariff1);
            _tariffRepo.GetByIdAsync("t-2", Arg.Any<CancellationToken>()).Returns(tariff2);

            var cache = new TariffCache(_tariffRepo, TimeSpan.FromSeconds(60));

            await cache.GetCachedTariffAsync("t-1");
            await cache.GetCachedTariffAsync("t-2");

            cache.Clear();

            await cache.GetCachedTariffAsync("t-1");
            await cache.GetCachedTariffAsync("t-2");

            await _tariffRepo.Received(2).GetByIdAsync("t-1", Arg.Any<CancellationToken>());
            await _tariffRepo.Received(2).GetByIdAsync("t-2", Arg.Any<CancellationToken>());
        }
    }
}
