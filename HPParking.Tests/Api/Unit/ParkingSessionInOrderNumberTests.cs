using FluentAssertions;
using HPParking.Api.DTOs.Excel.Reports;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Core.Data;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Mapster;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class ParkingSessionInOrderNumberTests
    {
        [Fact]
        public void MapToDto_WhenSessionHasInOrderNumber_ShouldCorrectlyMapToDtoAndDetailDto()
        {
            // Arrange
            var session = new ParkingSession
            {
                Id = "sess-123",
                PlateNumber = "30A-999.88",
                TargetType = LaneTargetType.Vehicle,
                InTime = DateTime.UtcNow,
                InOrderNumber = 137
            };

            // Act
            var dto = session.Adapt<ParkingSessionDto>();
            var detailDto = session.Adapt<ParkingSessionDetailDto>();

            // Assert
            dto.InOrderNumber.Should().Be(137);
            detailDto.InOrderNumber.Should().Be(137);
        }

        [Fact]
        public void ParkingSessionExcelProfile_WhenInitialized_ShouldHaveInOrderDisplayAsFirstColumn()
        {
            // Arrange
            var profile = new ParkingSessionExcelProfile();

            // Act
            var columns = profile.Columns;
            var firstCol = columns.FirstOrDefault(c => c.Order == 1);

            // Assert
            firstCol.Should().NotBeNull();
            firstCol!.ColumnName.Should().Be("Lượt vào");
            firstCol.Property.Name.Should().Be(nameof(ParkingSessionExcelDto.InOrderDisplay));
        }

        [Fact]
        public void PedestrianSessionExcelProfile_WhenInitialized_ShouldHaveInOrderDisplayAsFirstColumn()
        {
            // Arrange
            var profile = new PedestrianSessionExcelProfile();

            // Act
            var columns = profile.Columns;
            var firstCol = columns.FirstOrDefault(c => c.Order == 1);

            // Assert
            firstCol.Should().NotBeNull();
            firstCol!.ColumnName.Should().Be("Lượt vào");
            firstCol.Property.Name.Should().Be(nameof(PedestrianSessionExcelDto.InOrderDisplay));
        }

        [Fact]
        public void MongoDbIndexConfigurator_WhenCreatingParkingSessionInOrderIndex_ShouldHaveCorrectNameAndKeys()
        {
            // Arrange
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();

            // Act
            var model = MongoDbIndexConfigurator.GetParkingSessionInOrderIndexModel();
            var renderedKeys = model.Keys.Render(new RenderArgs<ParkingSession>(documentSerializer, serializerRegistry));

            // Assert
            model.Should().NotBeNull();
            model.Options.Name.Should().Be(MongoDbIndexConfigurator.ParkingSessionInOrderIndexName);
            renderedKeys.Should().NotBeNull();
            renderedKeys.ElementCount.Should().Be(2);
            renderedKeys.Contains("TargetType").Should().BeTrue();
            renderedKeys["TargetType"].AsInt32.Should().Be(1);
            renderedKeys.Contains("InOrderNumber").Should().BeTrue();
            renderedKeys["InOrderNumber"].AsInt32.Should().Be(-1);
        }
    }
}
