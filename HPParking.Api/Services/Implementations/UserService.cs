using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Users;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Interfaces;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<AuditLog> _auditLogRepo;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IRepository<User> userRepo,
            IRepository<AuditLog> auditLogRepo,
            ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _auditLogRepo = auditLogRepo;
            _logger = logger;
        }

        public async Task<PagedResult<UserDto>> GetUsersPagedAsync(UserFilterQuery query, CancellationToken cancellationToken = default)
        {
            var builder = Builders<User>.Filter;
            var filters = new List<FilterDefinition<User>>();

            if (query.IsActive.HasValue)
            {
                filters.Add(builder.Eq(u => u.IsActive, query.IsActive.Value));
            }

            if (query.Role.HasValue)
            {
                filters.Add(builder.Eq(u => u.Role, query.Role.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var cleanKw = Regex.Escape(query.Keyword.Trim());
                var regex = new BsonRegularExpression(cleanKw, "i");
                filters.Add(builder.Or(
                    builder.Regex(u => u.Username, regex),
                    builder.Regex(u => u.FullName, regex),
                    builder.Regex(u => u.Email, regex),
                    builder.Regex(u => u.PhoneNumber, regex)
                ));
            }

            var filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
            var sort = query.SortOrder?.ToLower() == "asc"
                ? Builders<User>.Sort.Ascending(u => u.CreatedAt)
                : Builders<User>.Sort.Descending(u => u.CreatedAt);

            var totalCount = await _userRepo.CountAsync(filter, onlyDeleted: query.OnlyDeleted, cancellationToken);
            var users = await _userRepo.FindAsync(filter, sort, query.Skip, query.PageSize, onlyDeleted: query.OnlyDeleted, cancellationToken);

            var dtos = users.Adapt<List<UserDto>>();
            return new PagedResult<UserDto>(dtos, query.PageIndex, query.PageSize, totalCount);
        }

        public async Task<UserDto> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy tài khoản người dùng với Id đã chỉ định.", ErrorCodes.USER_NOT_FOUND);
            }

            return user.Adapt<UserDto>();
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = request.Username.Trim().ToLowerInvariant();

            // Kiểm tra trùng lặp tên đăng nhập (chỉ kiểm tra các tài khoản chưa bị xóa mềm)
            var existing = await _userRepo.FindOneAsync(
                u => u.Username.ToLower() == normalizedUsername && !u.IsDeleted,
                cancellationToken);

            if (existing != null)
            {
                throw new ConflictException(
                    $"Tên đăng nhập '{request.Username}' đã tồn tại trong hệ thống.",
                    ErrorCodes.USER_DUPLICATE_USERNAME);
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username.Trim(),
                PasswordHash = passwordHash,
                FullName = request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                Role = request.Role,
                IsActive = request.IsActive,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user, cancellationToken);
            _logger.LogInformation("Đã tạo mới tài khoản người dùng: {Username} ({Role})", user.Username, user.Role);

            // Ghi nhật ký kiểm toán
            var auditLog = CreateAudit(
                user.Username,
                user.Role.ToString(),
                AuditActionType.Create,
                user.Id,
                user.FullName,
                $"Tạo mới tài khoản người dùng '{user.Username}' ({user.FullName}) với vai trò {user.Role}.");
            await _auditLogRepo.AddAsync(auditLog, cancellationToken);

            return user.Adapt<UserDto>();
        }

        public async Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy tài khoản người dùng để cập nhật.", ErrorCodes.USER_NOT_FOUND);
            }

            user.FullName = request.FullName.Trim();
            user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.Note = request.Note;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("Đã cập nhật thông tin tài khoản người dùng: {Username} ({Id})", user.Username, user.Id);

            var auditLog = CreateAudit(
                user.Username,
                user.Role.ToString(),
                AuditActionType.Update,
                user.Id,
                user.FullName,
                $"Cập nhật thông tin tài khoản '{user.Username}': Vai trò={user.Role}, Trạng thái={user.IsActive}.");
            await _auditLogRepo.AddAsync(auditLog, cancellationToken);

            return user.Adapt<UserDto>();
        }

        public async Task<bool> DeleteUserAsync(string id, string? currentUserId = null, bool permanent = false, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                if (permanent)
                {
                    user = await _userRepo.GetDeletedByIdAsync(id, cancellationToken);
                }
                if (user == null)
                {
                    throw new NotFoundException("Không tìm thấy tài khoản người dùng để xóa.", ErrorCodes.USER_NOT_FOUND);
                }
            }

            // Chặn người dùng tự xóa tài khoản của chính mình
            if (!string.IsNullOrWhiteSpace(currentUserId) && string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Bạn không thể tự xóa tài khoản đang đăng nhập của chính mình.", ErrorCodes.BAD_REQUEST);
            }

            // Chặn xóa tài khoản Quản trị viên (Admin) duy nhất còn hoạt động
            if (user.Role == UserRole.Admin)
            {
                var otherAdmins = await _userRepo.FindAsync(
                    u => u.Role == UserRole.Admin && u.Id != id && !u.IsDeleted && u.IsActive,
                    cancellationToken);

                if (otherAdmins.Count == 0)
                {
                    throw new BadRequestException(
                        "Không thể xóa tài khoản Quản trị viên (Admin) duy nhất đang hoạt động trong hệ thống.",
                        ErrorCodes.BAD_REQUEST);
                }
            }

            if (permanent)
            {
                await _userRepo.DeleteAsync(id, softDelete: false, cancellationToken: cancellationToken);
                _logger.LogWarning("Đã XÓA VĨNH VIỄN tài khoản người dùng: {Username} ({Id})", user.Username, id);
            }
            else
            {
                user.MarkDeleted();
                await _userRepo.UpdateAsync(user, cancellationToken);
                _logger.LogInformation("Đã chuyển tài khoản người dùng {Username} ({Id}) vào thùng rác.", user.Username, id);
            }

            var auditLog = CreateAudit(
                user.Username,
                user.Role.ToString(),
                permanent ? AuditActionType.PermanentDelete : AuditActionType.Delete,
                user.Id,
                user.FullName,
                $"Xóa tài khoản người dùng '{user.Username}' (Vĩnh viễn: {permanent}).");
            await _auditLogRepo.AddAsync(auditLog, cancellationToken);

            return true;
        }

        public async Task<bool> RestoreUserAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetDeletedByIdAsync(id, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Không tìm thấy tài khoản trong thùng rác để khôi phục.", ErrorCodes.USER_NOT_FOUND);
            }

            // Kiểm tra xem tên đăng nhập đã bị một tài khoản khác đang hoạt động sử dụng lại chưa
            var conflict = await _userRepo.FindOneAsync(
                u => u.Username.ToLower() == user.Username.ToLower() && !u.IsDeleted,
                cancellationToken);

            if (conflict != null)
            {
                throw new ConflictException(
                    $"Không thể khôi phục: Tên đăng nhập '{user.Username}' đã được sử dụng bởi một tài khoản khác.",
                    ErrorCodes.USER_DUPLICATE_USERNAME);
            }

            var success = await _userRepo.RestoreAsync(id, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Đã khôi phục tài khoản người dùng {Username} ({Id}) từ thùng rác.", user.Username, id);

                var auditLog = CreateAudit(
                    user.Username,
                    user.Role.ToString(),
                    AuditActionType.Restore,
                    user.Id,
                    user.FullName,
                    $"Khôi phục tài khoản người dùng '{user.Username}' từ thùng rác.");
                await _auditLogRepo.AddAsync(auditLog, cancellationToken);
            }

            return success;
        }

        public async Task<bool> ResetPasswordAsync(string id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy tài khoản người dùng để đặt lại mật khẩu.", ErrorCodes.USER_NOT_FOUND);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("Quản trị viên đã đặt lại mật khẩu cho tài khoản {Username} ({Id}).", user.Username, user.Id);

            var auditLog = CreateAudit(
                user.Username,
                user.Role.ToString(),
                AuditActionType.ChangePassword,
                user.Id,
                user.FullName,
                $"Đặt lại mật khẩu cho tài khoản người dùng '{user.Username}'.");
            await _auditLogRepo.AddAsync(auditLog, cancellationToken);

            return true;
        }

        public async Task<UserDto> ToggleStatusAsync(string id, string? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(id, cancellationToken);
            if (user == null || user.IsDeleted)
            {
                throw new NotFoundException("Không tìm thấy tài khoản người dùng để chuyển trạng thái.", ErrorCodes.USER_NOT_FOUND);
            }

            // Chặn người dùng tự khóa tài khoản của chính mình
            if (user.IsActive && !string.IsNullOrWhiteSpace(currentUserId) && string.Equals(id, currentUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Bạn không thể tự vô hiệu hóa tài khoản của chính mình.", ErrorCodes.BAD_REQUEST);
            }

            // Chặn khóa tài khoản Admin duy nhất
            if (user.IsActive && user.Role == UserRole.Admin)
            {
                var otherActiveAdmins = await _userRepo.FindAsync(
                    u => u.Role == UserRole.Admin && u.Id != id && !u.IsDeleted && u.IsActive,
                    cancellationToken);

                if (otherActiveAdmins.Count == 0)
                {
                    throw new BadRequestException(
                        "Không thể vô hiệu hóa tài khoản Quản trị viên (Admin) duy nhất đang hoạt động trong hệ thống.",
                        ErrorCodes.BAD_REQUEST);
                }
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("Đã chuyển đổi trạng thái tài khoản {Username} ({Id}) thành: {Status}.",
                user.Username, user.Id, user.IsActive ? "Kích hoạt" : "Vô hiệu hóa");

            var auditLog = CreateAudit(
                user.Username,
                user.Role.ToString(),
                AuditActionType.Update,
                user.Id,
                user.FullName,
                $"Chuyển trạng thái hoạt động tài khoản '{user.Username}' thành {(user.IsActive ? "Đang hoạt động" : "Ngừng hoạt động")}.");
            await _auditLogRepo.AddAsync(auditLog, cancellationToken);

            return user.Adapt<UserDto>();
        }

        private static AuditLog CreateAudit(
            string username,
            string role,
            AuditActionType actionType,
            string targetId,
            string? targetDisplay,
            string reason)
        {
            return new AuditLog
            {
                ActorUsername = username,
                ActorRole = role,
                ActionType = actionType,
                TargetEntity = "User",
                TargetId = targetId,
                TargetDisplay = targetDisplay,
                Reason = reason,
                IsSuccess = true,
                Source = "WebAdmin"
            };
        }
    }
}
