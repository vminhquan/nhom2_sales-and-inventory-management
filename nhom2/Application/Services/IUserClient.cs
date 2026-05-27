using nhom2.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nhom2.Application.Services
{
    public interface IUserClient
    {
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<List<UserDto>> GetUsersAsync();
    }
}