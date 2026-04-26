using mediconyx_api.Models;
using mediconyx_api.Models.DTOs;

namespace mediconyx_api.Services.Users;

public interface IUserService
{
    Task<UserDetailDto> GetCurrentUserAsync(int userId);
    Task<UserListResponse> GetUsersAsync(int? callerTenantId, UserQueryParams queryParams);
    Task<UserDetailDto> GetUserByIdAsync(int requestedUserId, int callerUserId, UserRole callerRole);
    Task<UserDetailDto> CreateUserAsync(CreateUserRequest request, int adminUserId);
    Task<UserDetailDto> UpdateUserAsync(int userId, UpdateUserRequest request, int callerUserId, UserRole callerRole);
    Task DeactivateUserAsync(int userId, int callerUserId);
    Task<UserDetailDto> ToggleActiveAsync(int userId, int callerUserId);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request, int adminUserId);
}
