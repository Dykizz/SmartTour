using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartTour.API.Data;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;

namespace SmartTour.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ICloudStorageService _cloudStorageService;

    public UserService(AppDbContext context, ICloudStorageService cloudStorageService)
    {
        _context = context;
        _cloudStorageService = cloudStorageService;
    }

    public async Task<PagedResponse<UserDto>> GetUsersPagedAsync(int roleId = 0, string? searchTerm = null, int? packageId = null, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Users
            .Include(u => u.Role)
            .AsQueryable();

        if (roleId > 0)
        {
            query = query.Where(u => u.RoleId == roleId);
        }
        else
        {
            // Mặc định không lấy ADMIN ra danh sách (tuỳ chỉnh nếu cần)
            query = query.Where(u => u.RoleId != 1);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        }

        if (packageId.HasValue)
        {
            if (packageId.Value == 0)
            {
                var allActiveUserIds = _context.Subscriptions
                    .Where(s => s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
                    .Select(s => s.UserId);
                query = query.Where(u => !allActiveUserIds.Contains(u.Id));
            }
            else
            {
                var activePackageUserIds = _context.Subscriptions
                    .Where(s => s.PackageId == packageId.Value && s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
                    .Select(s => s.UserId);
                query = query.Where(u => activePackageUserIds.Contains(u.Id));
            }
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip(Math.Max(0, (pageNumber - 1) * pageSize))
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = string.IsNullOrEmpty(u.FullName) ? u.Username : u.FullName,
                Email = u.Email,
                RoleName = u.Role != null ? u.Role.Name : "Không xác định",
                RoleId = u.RoleId,
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive
            })
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var activeSubscriptions = await _context.Subscriptions
            .Include(s => s.ServicePackage)
            .Where(s => userIds.Contains(s.UserId) && s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
            .ToListAsync();

        foreach (var user in users)
        {
            // Update FullName fallbacks 
            if (string.IsNullOrEmpty(user.FullName)) user.FullName = user.Email;
            
            var sub = activeSubscriptions
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefault();
            
            user.CurrentPackageName = sub?.ServicePackage?.Name ?? "Chưa có gói";
        }

        return new PagedResponse<UserDto>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<Dictionary<int, int>> GetUserRoleCountsAsync(string? searchTerm = null, int? packageId = null)
    {
        // Exclude admin (roleId = 1) from general counts as done in GetUsersPagedAsync
        var query = _context.Users.AsNoTracking().Where(u => u.RoleId != 1); 

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        }

        if (packageId.HasValue)
        {
            if (packageId.Value == 0)
            {
                var allActiveUserIds = _context.Subscriptions
                    .Where(s => s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
                    .Select(s => s.UserId);
                query = query.Where(u => !allActiveUserIds.Contains(u.Id));
            }
            else
            {
                var activePackageUserIds = _context.Subscriptions
                    .Where(s => s.PackageId == packageId.Value && s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
                    .Select(s => s.UserId);
                query = query.Where(u => activePackageUserIds.Contains(u.Id));
            }
        }

        var roleCounts = await query
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        var allCount = roleCounts.Values.Sum();

        return new Dictionary<int, int>
        {
            { 0, allCount },
            { 2, roleCounts.GetValueOrDefault(2, 0) },
            { 3, roleCounts.GetValueOrDefault(3, 0) }
        };
    }

    public async Task<bool> UpdateUserAsync(int id, UserDto userDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.FullName = userDto.FullName;
        // Email is not allowed to be updated per user request, but we could add it if needed.
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> UpdateAvatarAsync(int id, Stream fileStream, string fileName, string contentType)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) throw new Exception("User not found");

        // 1. Upload mới lên GCS
        var newFileName = $"avatars/user_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
        var avatarUrl = await _cloudStorageService.UploadFileAsync(fileStream, newFileName, contentType);

        // 2. Xóa ảnh cũ trên GCS (nếu có và không phải ảnh mặc định)
        if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.Contains("storage.googleapis.com"))
        {
            try { await _cloudStorageService.DeleteFileAsync(user.AvatarUrl); } catch { }
        }

        // 3. Cập nhật DB
        user.AvatarUrl = avatarUrl;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return avatarUrl;
    }
}
