using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Departments;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DepartmentsController : BaseApiController
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentsController(
            IDepartmentService departmentService,
            ILogger<DepartmentsController> logger)
            : base(logger)
        {
            _departmentService = departmentService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ phòng ban có phân trang, hỗ trợ lọc theo Công ty hoặc từ khóa tìm kiếm
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DepartmentDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetDepartments([FromQuery] DepartmentFilterQuery query)
        {
            var result = await _departmentService.GetDepartmentsPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách phòng ban thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin một phòng ban theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetDepartmentById(string id)
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);
            return OkApiResponse(department, "Lấy thông tin phòng ban thành công.");
        }

        /// <summary>
        /// Tạo mới phòng ban, kiểm tra CompanyId tồn tại và mã Code duy nhất
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
        {
            var created = await _departmentService.CreateDepartmentAsync(request);
            return CreatedApiResponse($"/api/v1/departments/{created.Id}", created, "Tạo mới phòng ban thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin phòng ban, kiểm tra trùng mã Code nếu thay đổi
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateDepartment(string id, [FromBody] UpdateDepartmentRequest request)
        {
            var updated = await _departmentService.UpdateDepartmentAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin phòng ban thành công.");
        }

        /// <summary>
        /// Xóa phòng ban (Mặc định xóa mềm, chặn xóa nếu còn nhân sự/khách hàng trực thuộc theo ADR 0030)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteDepartment(string id, [FromQuery] bool hardDelete = false)
        {
            await _departmentService.DeleteDepartmentAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn phòng ban khỏi cơ sở dữ liệu."
                : "Đã xóa mềm phòng ban thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục phòng ban từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreDepartment(string id)
        {
            var restored = await _departmentService.RestoreDepartmentAsync(id);
            return OkApiResponse(restored, "Khôi phục phòng ban thành công.");
        }
    }
}
