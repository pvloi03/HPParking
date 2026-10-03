using HPParking.Core.Models.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CardFilterTests
    {
        [Fact]
        public void CardFilter_UnassignedWithEmptyString_ThrowsFormatException()
        {
            // Tái hiện chính xác lỗi: khi so sánh ClientId (BsonType.ObjectId) với string.Empty
            // MongoDB Driver sẽ gọi ObjectId.Parse("") dẫn đến System.FormatException: '' is not a valid 24 digit hex string.
            var filter = Builders<Card>.Filter.Eq(x => x.ClientId, string.Empty);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<Card>();

            Assert.Throws<FormatException>(() =>
            {
                filter.Render(new RenderArgs<Card>(documentSerializer, serializerRegistry));
            });
        }

        [Fact]
        public void CardFilter_UnassignedWithNull_RendersCorrectly()
        {
            // Kiểm chứng cách sửa chuẩn: so sánh ClientId với null
            var filter = Builders<Card>.Filter.Eq(x => x.ClientId, null);
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<Card>();

            var rendered = filter.Render(new RenderArgs<Card>(documentSerializer, serializerRegistry));
            Assert.NotNull(rendered);
            Assert.True(rendered.Contains("ClientId"));
        }
    }
}
