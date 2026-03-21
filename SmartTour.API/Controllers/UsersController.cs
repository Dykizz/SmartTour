using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;
using System.Threading.Tasks;

namespace SmartTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id, [FromServices] SmartTour.API.Data.AppDbContext context)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(new UserDto {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            });
        }

        [HttpGet("counts")]
        public async Task<ActionResult<Dictionary<int, int>>> GetCounts([FromQuery] string? searchTerm = null, [FromQuery] int? packageId = null)
        {
            var counts = await _userService.GetUserRoleCountsAsync(searchTerm, packageId);
            return Ok(counts);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
            [FromQuery] int roleId = 0,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? packageId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var pagedResponse = await _userService.GetUsersPagedAsync(roleId, searchTerm, packageId, pageNumber, pageSize);
            return Ok(pagedResponse);
        }
    }
}
