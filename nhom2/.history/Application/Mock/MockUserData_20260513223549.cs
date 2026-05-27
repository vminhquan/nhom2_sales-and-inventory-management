using nhom2.Application.DTOs;

namespace nhom2.Application.Mock
{
    /// <summary>
    /// Mock dữ liệu User - dùng để test khi User Service chưa sẵn sàng
    /// </summary>
    public class MockUserData
    {
        // Dữ liệu giả lập User
        private static readonly List<UserMockDto> Users = new()
        {
            new UserMockDto { Id = 1, Name = "Trần Nam Khánh", Email = "tran.nam@example.com" },
            new UserMockDto { Id = 2, Name = "Đặng Tuấn Anh", Email = "dang.tuan@example.com" },
            new UserMockDto { Id = 3, Name = "Kiều Quang Trường", Email = "kieu.quang@example.com" },
            new UserMockDto { Id = 4, Name = "Võ Minh Quân", Email = "vo.minh@example.com" },
            new UserMockDto { Id = 5, Name = "Tô Vĩ Đức", Email = "to.vi@example.com" },
        };

        /// <summary>
        /// Lấy user theo ID
        /// </summary>
        public static UserMockDto? GetUserById(int userId)
        {
            return Users.FirstOrDefault(u => u.Id == userId);
        }

        /// <summary>
        /// Lấy tất cả users
        /// </summary>
        public static List<UserMockDto> GetAllUsers()
        {
            return Users;
        }
    }

    /// <summary>
    /// DTO để represent User trong mock data
    /// </summary>
    public class UserMockDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
