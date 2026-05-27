using Microsoft.AspNetCore.Mvc;
using nhom2.Application.Mock;

namespace nhom2.Api.Mock
{
    /// <summary>
    /// Mock API Controller cho User
    /// Được sử dụng để test khi User Service từ nhóm 3 chưa sẵn sàng
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MockUserController : ControllerBase
    {
        /// <summary>
        /// GET: api/mockuser/{id}
        /// Lấy user theo ID từ mock data
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = MockUserData.GetUserById(id);
            if (user == null)
                return NotFound(new { message = $"User với ID {id} không tìm thấy" });

            return Ok(new { success = true, data = user });
        }

        /// <summary>
        /// GET: api/mockuser
        /// Lấy tất cả users từ mock data
        /// </summary>
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = MockUserData.GetAllUsers();
            return Ok(new { success = true, data = users });
        }
    }
}
