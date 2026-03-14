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

        [HttpGet]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
            [FromQuery] int roleId = 0,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var pagedResponse = await _userService.GetUsersPagedAsync(roleId, pageNumber, pageSize);
            return Ok(pagedResponse);
        }
    }
}
