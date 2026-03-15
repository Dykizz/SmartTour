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

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<UserDto>> GetUsersPagedAsync(int roleId = 0, int pageNumber = 1, int pageSize = 10)
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

    public async Task<Dictionary<int, int>> GetUserRoleCountsAsync()
    {
        // Exclude admin (roleId = 1) from general counts as done in GetUsersPagedAsync
        var query = _context.Users.AsNoTracking().Where(u => u.RoleId != 1); 

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
}
