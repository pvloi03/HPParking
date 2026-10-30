using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        /// <summary>
        /// [LƯU DB] ID Bảng LED hiển thị ngoại vi
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string? LedBoardDeviceId { get; set; }

        public int OutputRelay { get; set; }

        public int InputReader { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Kiểm tra làn có được trang bị hoặc kích hoạt thiết bị/camera nhận diện khuôn mặt (FaceID) hay không.
        /// </summary>
        public bool HasFaceDevice() => UseFaceCam || !string.IsNullOrWhiteSpace(FaceDeviceId);

        /// <summary>
        /// Nguồn chân lý duy nhất (SSOT): Lấy danh sách ID của toàn bộ các thiết bị ngoại vi gắn vào làn.
        /// Tự động lọc bỏ các giá trị null hoặc khoảng trắng.
        /// </summary>
        public IEnumerable<string> GetAllDeviceIds()
        {
            var ids = new[]
            {
                OverviewCameraDeviceId,
                PlateCameraDeviceId,
                ControllerDeviceId,
                FaceDeviceId,
                LedBoardDeviceId
            };

            return ids.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!.Trim()).Distinct();
        }

        /// <summary>
        /// Kiểm tra làn xe có đang sử dụng thiết bị với ID chỉ định hay không (in-memory).
        /// </summary>
        public bool UsesDevice(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) return false;
            var trimmed = deviceId.Trim();
            return GetAllDeviceIds().Contains(trimmed);
        }

        /// <summary>
        /// Biểu thức truy vấn LINQ/MongoDB: Tìm các làn xe chưa bị xóa có gắn thiết bị chỉ định.
        /// </summary>
        public static Expression<Func<Lane, bool>> HasDeviceAssigned(string deviceId)
        {
            return l => !l.IsDeleted &&
                        (l.OverviewCameraDeviceId == deviceId ||
                         l.PlateCameraDeviceId == deviceId ||
                         l.ControllerDeviceId == deviceId ||
                         l.FaceDeviceId == deviceId ||
                         l.LedBoardDeviceId == deviceId);
        }

        /// <summary>
        /// Biểu thức truy vấn LINQ/MongoDB: Tìm các làn xe đang hoạt động và chưa bị xóa có gắn thiết bị chỉ định.
        /// </summary>
        public static Expression<Func<Lane, bool>> HasActiveDeviceAssigned(string deviceId)
        {
            return l => l.IsActive && !l.IsDeleted &&
                        (l.OverviewCameraDeviceId == deviceId ||
                         l.PlateCameraDeviceId == deviceId ||
                         l.ControllerDeviceId == deviceId ||
                         l.FaceDeviceId == deviceId ||
                         l.LedBoardDeviceId == deviceId);
        }
    }
}