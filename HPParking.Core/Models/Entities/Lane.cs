using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParking.Core.Models.Entities
{
    [BsonIgnoreExtraElements]
    public class Lane : BaseEntity
    {
        public string Code { get; set; } = "";

        public string Name { get; set; } = "";

        /// <summary>
        /// ID của Cổng (Gate.Id) mà làn này trực thuộc
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? GateId { get; set; }

        /// <summary>
        /// Hướng di chuyển của làn xe (Vào / Ra / Hai chiều)
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public LaneDirection Direction { get; set; } = LaneDirection.In;

        /// <summary>
        /// Phân loại đối tượng kiểm soát của làn (Xe cơ giới / Người đi bộ)
        /// </summary>
        [BsonRepresentation(BsonType.String)]
        public LaneTargetType TargetType { get; set; } = LaneTargetType.Vehicle;

        /// <summary>
        /// Kích hoạt camera chụp ảnh toàn cảnh
        /// </summary>
        public bool UseOverviewCam { get; set; } = true;

        /// <summary>
        /// Kích hoạt camera nhận diện biển số
        /// </summary>
        public bool UsePlateCam { get; set; } = true;

        /// <summary>
        /// Kích hoạt camera/thiết bị nhận diện khuôn mặt
        /// </summary>
        public bool UseFaceCam { get; set; } = false;

        /// <summary>
        /// [LƯU DB] ID Camera chụp ảnh toàn cảnh
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OverviewCameraDeviceId { get; set; }

        /// <summary>
        /// [LƯU DB] ID Camera chụp ảnh biển số
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PlateCameraDeviceId { get; set; }

        /// <summary>
        /// [LƯU DB] ID Bộ điều khiển
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ControllerDeviceId { get; set; }

        /// <summary>
        /// [LƯU DB] ID Thiết bị nhận diện FaceID
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? FaceDeviceId { get; set; }

        public int OutputRelay { get; set; }

        public int InputReader { get; set; }

        public bool IsActive { get; set; } = true;
    }
}