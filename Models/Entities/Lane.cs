using HPParking.Services.Camera;
using HPParking.Services.Controller;
using HPParking.UI;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.Json;

namespace HPParking.Models.Entities
{
    public class Lane
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; }
        public string Gate_Code { get; set; } = "";

        public string Name { get; set; }

        public int Type { get; set; }

        public string Controller { get; set; }

        public string CameraLicensePlate { get; set; }

        public string CameraClient { get; set; }

        public string CameraLicensePlateOto { get; set; } = "";

        public string FaceId { get; set; } = "";

        public int OutputRelay { get; set; }

        public int InputReader { get; set; }

        public int InputLoop { get; set; } = 0;

        public DateTime CreatDay { get; set; } = DateTime.UtcNow;

        public string CreatUser { get; set; } = "";

        public DateTime UpdateDay { get; set; } = DateTime.UtcNow;

        public string UpdateUser { get; set; } = "";

        public bool IsDelete { get; set; } = false;

        // ============================
        // Các property chỉ dùng trong code
        // Không lưu xuống MongoDB
        // ============================

        [BsonIgnore]
        public DeviceConfig ControllerConfig =>
    string.IsNullOrWhiteSpace(Controller)
        ? null
        : JsonSerializer.Deserialize<DeviceConfig>(Controller);

        [BsonIgnore]
        public DeviceConfig CameraLicensePlateConfig =>
            string.IsNullOrWhiteSpace(CameraLicensePlate)
                ? null
                : JsonSerializer.Deserialize<DeviceConfig>(CameraLicensePlate);

        [BsonIgnore]
        public DeviceConfig CameraClientConfig =>
            string.IsNullOrWhiteSpace(CameraClient)
                ? null
                : JsonSerializer.Deserialize<DeviceConfig>(CameraClient);

        [BsonIgnore]
        public DeviceConfig CameraLicensePlateOtoConfig =>
            string.IsNullOrWhiteSpace(CameraLicensePlateOto)
                ? null
                : JsonSerializer.Deserialize<DeviceConfig>(CameraLicensePlateOto);

        [BsonIgnore]
        public DeviceConfig FaceIdConfig =>
            string.IsNullOrWhiteSpace(FaceId)
                ? null
                : JsonSerializer.Deserialize<DeviceConfig>(FaceId);

        //================ Runtime =================

        [BsonIgnore]
        public LaneCamera Cameras { get; set; }

        [BsonIgnore]
        public OverviewCameraService FaceIds { get; set; }

        [BsonIgnore]
        public ControllerService Ctrl { get; set; }

        [BsonIgnore]
        public VehicleUI UI { get; set; }

    }

    public class LaneCamera
    {
        public PlateCameraService LicensePlateCamera { get; set; }

        public OverviewCameraService OverviewCamera { get; set; }
    }
}