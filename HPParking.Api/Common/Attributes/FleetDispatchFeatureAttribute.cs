namespace HPParking.Api.Common.Attributes
{
    /// <summary>
    /// Đánh dấu Controller hoặc Endpoint thuộc về phân hệ Điều vận phương tiện nội bộ (Fleet Dispatch).
    /// Khi cờ FeatureOptions.EnableFleetDispatch = false, FleetDispatchControllerConvention sẽ tự động
    /// vô hiệu hóa và ẩn các endpoint này khỏi Swagger/ApiExplorer mà không cần khớp chuỗi tên Controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class FleetDispatchFeatureAttribute : Attribute
    {
    }
}
