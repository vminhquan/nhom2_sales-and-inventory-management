using Microsoft.AspNetCore.Mvc;
using nhom2.Application.Services;
using System;
using System.Threading.Tasks;

namespace nhom2.Api.User
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserClient _userClient;

        public UserController(IUserClient userClient)
        {
            _userClient = userClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userClient.GetUsersAsync();
                return Ok(new { success = true, data = users });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(503, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
