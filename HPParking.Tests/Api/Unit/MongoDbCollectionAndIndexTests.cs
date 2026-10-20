using FluentAssertions;
using HPParking.Core.Data;
using HPParking.Core.Models.Entities;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class MongoDbCollectionAndIndexTests
    {
        [Fact]
        public void ResolveCollectionName_ForClientApprovalRequest_ShouldReturnSnakeCaseCollectionName()
        {
            var collectionName = MongoDbContext.ResolveCollectionName<ClientApprovalRequest>();

            collectionName.Should().Be("client_approval_requests");
            collectionName.Should().Be(ClientApprovalRequest.CollectionName);
        }

        [Fact]
        public void ResolveCollectionName_ForEntitiesWithoutAttribute_ShouldFallbackToPluralizedName()
        {
            var clientCol = MongoDbContext.ResolveCollectionName<Client>();
            var vehicleCol = MongoDbContext.ResolveCollectionName<Vehicle>();

            clientCol.Should().Be("Clients");
            vehicleCol.Should().Be("Vehicles");
        }

        [Fact]
        public void GetTokenUniqueIndexModel_ShouldHaveCorrectUniqueAndSparseConfiguration()
        {
            var indexModel = MongoDbIndexConfigurator.GetTokenUniqueIndexModel();

            indexModel.Should().NotBeNull();
            indexModel.Options.Should().NotBeNull();
            indexModel.Options.Unique.Should().BeTrue();
            indexModel.Options.Sparse.Should().BeTrue();
            indexModel.Options.Name.Should().Be("UX_client_approval_requests_token");
        }
    }
}
