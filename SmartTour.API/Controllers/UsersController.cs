using Microsoft.AspNetCore.Mvc;
using SmartTour.API.Interfaces;
using SmartTour.Shared.Models;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet("debug-all")]
        public async Task<ActionResult> DebugAll([FromServices] SmartTour.API.Data.AppDbContext context)
        {
            var list = await context.Users.Select(u => new { u.Id, u.Username }).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id, [FromServices] SmartTour.API.Data.AppDbContext context)
        {
            var user = await context.Users.FindAsync(id);
            if (user == null) 
            {
                var count = await context.Users.CountAsync();
                var dbName = context.Database.GetDbConnection().Database;
                System.Console.WriteLine($"[DEBUG] User {id} NOT FOUND in DB '{dbName}'. Total users: {count}");
                return NotFound();
            }
            return Ok(new UserDto {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                AvatarUrl = user.AvatarUrl
            });
        }

        [HttpPost("{id}/avatar")]
        public async Task<ActionResult<string>> PostAvatar(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            try 
            {
                using var stream = file.OpenReadStream();
                var url = await _userService.UpdateAvatarAsync(id, stream, file.FileName, file.ContentType);
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UserDto userDto)
        {
            if (id != userDto.Id) return BadRequest();

            var result = await _userService.UpdateUserAsync(id, userDto);
            if (!result) return NotFound();

            return NoContent();
        }
    }
}
