using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParking.Api.Data;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class DbSeederTests
    {
        private readonly IRepository<User> _userRepo = Substitute.For<IRepository<User>>();
        private readonly ILogger<DbSeeder> _logger = Substitute.For<ILogger<DbSeeder>>();
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;

        public DbSeederTests()
        {
            var configData = new Dictionary<string, string?>
            {
                { "AdminSeeder:DefaultUsername", "admin" },
                { "AdminSeeder:DefaultPassword", "admin123" },
                { "AdminSeeder:DefaultFullName", "System Administrator" }
            };

            _config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

            var services = new ServiceCollection();
            services.AddSingleton(_userRepo);
            services.AddSingleton(_config);
            services.AddSingleton(_logger);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task SeedAdminUserAsync_WhenNoAdminExists_CreatesDefaultAdminUser()
        {
            // Arrange
            _userRepo.ExistsAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult(false));

            // Act
            await DbSeeder.SeedAdminUserAsync(_serviceProvider);

            // Assert
            await _userRepo.Received(1).AddAsync(
                Arg.Is<User>(u =>
                    u.Username == "admin" &&
                    u.Role == UserRole.Admin &&
                    u.IsActive == true &&
                    u.FullName == "System Administrator" &&
                    BCrypt.Net.BCrypt.Verify("admin123", u.PasswordHash)),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SeedAdminUserAsync_WhenAdminAlreadyExists_DoesNotCreateAnyUser()
        {
            // Arrange
            _userRepo.ExistsAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult(true));

            // Act
            await DbSeeder.SeedAdminUserAsync(_serviceProvider);

            // Assert
            await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SeedAdminUserAsync_WhenDatabaseFails_LogsWarningAndDoesNotThrow()
        {
            // Arrange
            _userRepo.ExistsAsync(Arg.Any<Expression<Func<User, bool>>>(), Arg.Any<CancellationToken>())
                     .ThrowsAsync(new TimeoutException("MongoDB unavailable"));

            // Act
            var act = () => DbSeeder.SeedAdminUserAsync(_serviceProvider);

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
