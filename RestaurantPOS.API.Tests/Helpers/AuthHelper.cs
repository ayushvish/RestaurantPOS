using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;

namespace RestaurantPOS.API.Tests.Helpers;

public static class AuthHelper
{
    public static async Task<AuthResponse> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Options);
        return authResponse!;
    }

    public static async Task<AuthResponse> PinLoginAsync(HttpClient client, string pin, Guid outletId)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/pin-login", new PinLoginRequest(pin, outletId));
        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(TestJsonOptions.Options);
        return authResponse!;
    }

    public static void SetBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
