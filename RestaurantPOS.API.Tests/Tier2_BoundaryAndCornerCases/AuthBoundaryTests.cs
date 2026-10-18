using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier2_BoundaryAndCornerCases;

public class AuthBoundaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public AuthBoundaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("admin", "WrongPassword999!");

        var response = await client.PostAsJsonAsync("/api/Auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("non_existent_user_999", "AnyPassword!");

        var response = await client.PostAsJsonAsync("/api/Auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PinLogin_WithInvalidPin_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new PinLoginRequest("9999", TestDataConstants.OutletId);

        var response = await client.PostAsJsonAsync("/api/Auth/pin-login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PinLogin_WithNonExistentOutletId_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new PinLoginRequest(TestDataConstants.CashierPin, Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/Auth/pin-login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterRestaurant_WithExistingUsername_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = new CreateRestaurantRequest(
            Name: "Conflict Cafe",
            Tagline: "Duplicate User Test",
            GstNumber: "27XXXXX0000X1Z1",
            FssaiNumber: null,
            Address: null,
            Phone: null,
            Email: null,
            CurrencySymbol: "₹",
            DefaultGstPercent: 5.0m,
            ServiceChargePercent: 0.0m,
            AdminUsername: "admin", // Already seeded in DB!
            AdminPassword: TestDataConstants.DefaultPassword,
            AdminFullName: "Duplicate Admin"
        );

        var response = await client.PostAsJsonAsync("/api/Auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdminLogin_WithNullRestaurantId_GeneratesTokenWithoutException()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("superadmin", TestDataConstants.DefaultPassword);

        var response = await client.PostAsJsonAsync("/api/Auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.Token));
        Assert.Null(authResponse.RestaurantId);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsBadRequest()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var request = new CreateUserRequest(
            RestaurantId: TestDataConstants.RestaurantId,
            OutletId: TestDataConstants.OutletId,
            FullName: "Duplicate Cashier",
            Username: "cashier1", // Already exists!
            Password: TestDataConstants.DefaultPassword,
            Role: UserRole.Cashier,
            Pin: "9876"
        );

        var response = await client.PostAsJsonAsync("/api/Auth/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
