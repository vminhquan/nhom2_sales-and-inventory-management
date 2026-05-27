using System.Text.Json.Serialization;

namespace nhom2.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; } = UserRole.User;
    }

    public enum UserRole
    {
        User = 0,
        Admin = 1
    }
}