using FluentAssertions;
using HPParking.Core.Data;
using HPParking.Core.Models.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class MongoDbCollectionAndIndexTests
    {
        [Fact]
        public void ResolveCollectionName_ForClientApprovalRequest_ShouldFollowStandardPluralizedConvention()
        {
            var collectionName = MongoDbContext.ResolveCollectionName<ClientApprovalRequest>();

            collectionName.Should().Be("ClientApprovalRequests");
        }

        [Fact]
        public void ResolveCollectionName_ForEntities_ShouldReturnPluralizedNames()
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
            indexModel.Options.Name.Should().Be(MongoDbIndexConfigurator.TokenIndexName);
        }

        [Fact]
        public void GetTariffCodeUniqueIndexModel_ShouldHaveCorrectUniqueAndPartialFilter()
        {
            var indexModel = MongoDbIndexConfigurator.GetTariffCodeUniqueIndexModel();

            indexModel.Should().NotBeNull();
            indexModel.Options.Should().NotBeNull();
            indexModel.Options.Unique.Should().BeTrue();
            indexModel.Options.Name.Should().Be(MongoDbIndexConfigurator.TariffCodeActiveIndexName);
            indexModel.Options.PartialFilterExpression.Should().NotBeNull();
        }

        [Fact]
        public void GetParkingSessionActiveCardUniqueIndexModel_ShouldHaveCorrectUniqueAndPartialFilter()
        {
            var indexModel = MongoDbIndexConfigurator.GetParkingSessionActiveCardUniqueIndexModel();

            indexModel.Should().NotBeNull();
            indexModel.Options.Should().NotBeNull();
            indexModel.Options.Unique.Should().BeTrue();
            indexModel.Options.Name.Should().Be(MongoDbIndexConfigurator.ParkingSessionActiveCardIndexName);
            indexModel.Options.PartialFilterExpression.Should().NotBeNull();
        }

        [Fact]
        public void GetParkingSessionInOrderIndexModel_ShouldHaveCorrectNameAndKeys()
        {
            var indexModel = MongoDbIndexConfigurator.GetParkingSessionInOrderIndexModel();

            indexModel.Should().NotBeNull();
            indexModel.Options.Should().NotBeNull();
            indexModel.Options.Name.Should().Be(MongoDbIndexConfigurator.ParkingSessionInOrderIndexName);
        }

        [Fact]
        public void ParkingSession_FeeAmount_ShouldSerializeAsBsonDecimal128()
        {
            var session = new ParkingSession
            {
                FeeAmount = 25000.50m
            };

            var doc = session.ToBsonDocument();
            doc.Contains("FeeAmount").Should().BeTrue();
            doc["FeeAmount"].BsonType.Should().Be(BsonType.Decimal128);
            doc["FeeAmount"].AsDecimal128.Should().Be(new Decimal128(25000.50m));

            var deserialized = BsonSerializer.Deserialize<ParkingSession>(doc);
            deserialized.FeeAmount.Should().Be(25000.50m);
        }

        [Fact]
        public void Tariff_MonetaryFields_ShouldSerializeAsBsonDecimal128()
        {
            var tariff = new Tariff
            {
                FlatPrice = 10000m,
                BasePrice = 15000m,
                AdditionalBlockPrice = 5000m,
                OvernightSurcharge = 30000m,
                MaxDailyCap = 100000m
            };

            var doc = tariff.ToBsonDocument();

            doc["FlatPrice"].BsonType.Should().Be(BsonType.Decimal128);
            doc["BasePrice"].BsonType.Should().Be(BsonType.Decimal128);
            doc["AdditionalBlockPrice"].BsonType.Should().Be(BsonType.Decimal128);
            doc["OvernightSurcharge"].BsonType.Should().Be(BsonType.Decimal128);
            doc["MaxDailyCap"].BsonType.Should().Be(BsonType.Decimal128);

            doc["FlatPrice"].AsDecimal128.Should().Be(new Decimal128(10000m));
            doc["BasePrice"].AsDecimal128.Should().Be(new Decimal128(15000m));
            doc["AdditionalBlockPrice"].AsDecimal128.Should().Be(new Decimal128(5000m));
            doc["OvernightSurcharge"].AsDecimal128.Should().Be(new Decimal128(30000m));
            doc["MaxDailyCap"].AsDecimal128.Should().Be(new Decimal128(100000m));

            var deserialized = BsonSerializer.Deserialize<Tariff>(doc);
            deserialized.FlatPrice.Should().Be(10000m);
            deserialized.BasePrice.Should().Be(15000m);
            deserialized.AdditionalBlockPrice.Should().Be(5000m);
            deserialized.OvernightSurcharge.Should().Be(30000m);
            deserialized.MaxDailyCap.Should().Be(100000m);
        }
    }
}
