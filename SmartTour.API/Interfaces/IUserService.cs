using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartTour.Shared.Models;

namespace SmartTour.API.Interfaces;

public interface IUserService
{
    Task<PagedResponse<UserDto>> GetUsersPagedAsync(int roleId = 0, string? searchTerm = null, int? packageId = null, int pageNumber = 1, int pageSize = 10);
    Task<Dictionary<int, int>> GetUserRoleCountsAsync(string? searchTerm = null, int? packageId = null);
    Task<bool> UpdateUserAsync(int id, UserDto userDto);
    Task<string> UpdateAvatarAsync(int id, Stream fileStream, string fileName, string contentType);
}
