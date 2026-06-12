using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using nhom2.Application.DTOs;

namespace nhom2.Application.Services;

public class UserClient : IUserClient
{
    private readonly HttpClient _http;
    private readonly string _internalApiKey;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public UserClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _internalApiKey = configuration["Services:InternalApiKey"]
            ?? throw new InvalidOperationException("Services:InternalApiKey is not configured.");
    }
    // lấy user bằng api endpoint sv user và trả về lỗi 
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/users/{id}");
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        var response = await _http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await ReadUserAsync(response);
    }
    // lấy all user khi connected
    public async Task<List<UserDto>> GetUsersAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/users");
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        var response = await _http.SendAsync(request);
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
