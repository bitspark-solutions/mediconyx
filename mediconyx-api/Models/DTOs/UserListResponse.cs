namespace mediconyx_api.Models.DTOs;

public class UserListResponse
{
    public List<UserDetailDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
