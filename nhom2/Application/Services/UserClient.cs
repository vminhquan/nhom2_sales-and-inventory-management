using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public class UserClient : IUserClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public UserClient(HttpClient http)
    {
        _http = http;
    }
    // lấy user bằng api endpoint sv user và trả về lỗi 
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var response = await _http.GetAsync($"/api/User/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await ReadUserAsync(response);
    }
    // lấy all user khi connected
    public async Task<List<UserDto>> GetUsersAsync()
    {
        var response = await _http.GetAsync("/api/User");
        response.EnsureSuccessStatusCode();
        return await ReadUserListAsync(response);
    }

    private static async Task<UserDto?> ReadUserAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var wrapped = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, JsonOptions);
        if (wrapped?.Data != null)
            return wrapped.Data;

        return JsonSerializer.Deserialize<UserDto>(content, JsonOptions);
    }

    private static async Task<List<UserDto>> ReadUserListAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var wrapped = JsonSerializer.Deserialize<ApiResponse<List<UserDto>>>(content, JsonOptions);
        if (wrapped?.Data != null)
            return wrapped.Data;

        return JsonSerializer.Deserialize<List<UserDto>>(content, JsonOptions) ?? new List<UserDto>();
    }
}
