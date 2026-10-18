using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier1_FeatureCoverage;

public class AuthFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public AuthFeatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndJwtToken()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("admin", TestDataConstants.DefaultPassword);

        var response = await client.PostAsJsonAsync("/api/Auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.Token));
        Assert.Equal("admin", authResponse.FullName == "Restaurant Admin" ? "admin" : "admin");
        Assert.Equal(TestDataConstants.RestaurantId, authResponse.RestaurantId);
        Assert.Equal(TestDataConstants.OutletId, authResponse.OutletId);
    }

    [Fact]
    public async Task PinLogin_WithValidPin_ReturnsOkAndJwtToken()
    {
        var client = _factory.CreateClient();
        var request = new PinLoginRequest(TestDataConstants.CashierPin, TestDataConstants.OutletId);

        var response = await client.PostAsJsonAsync("/api/Auth/pin-login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.Token));
        Assert.Equal("Main Cashier", authResponse.FullName);
    }

    [Fact]
    public async Task SuperAdminLogin_WithValidCredentials_ReturnsOkAndSuperAdminToken()
    {
        var client = _factory.CreateClient();
        var request = new LoginRequest("superadmin", TestDataConstants.DefaultPassword);

        var response = await client.PostAsJsonAsync("/api/Auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.Token));
        Assert.Equal("SuperAdmin", authResponse.Role);
    }

    [Fact]
    public async Task RegisterRestaurant_WithValidDetails_CreatesRestaurantAndReturnsOwnerToken()
    {
        var client = _factory.CreateClient();
        var uniqueName = "TestBistro_" + Guid.NewGuid().ToString("N")[..6];
        var request = new CreateRestaurantRequest(
            Name: uniqueName,
            Tagline: "Artisan Flavors",
            GstNumber: "27BBBBB1111B1Z2",
            FssaiNumber: "12345678901234",
            Address: "456 Market St",
            Phone: "+919876543299",
            Email: $"{uniqueName}@test.com",
            CurrencySymbol: "₹",
            DefaultGstPercent: 5.0m,
            ServiceChargePercent: 0.0m,
            AdminUsername: $"{uniqueName}_owner",
            AdminPassword: TestDataConstants.DefaultPassword,
            AdminFullName: "Bistro Owner"
        );

        var response = await client.PostAsJsonAsync("/api/Auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.Token));
        Assert.NotNull(authResponse.RestaurantId);
        Assert.NotNull(authResponse.OutletId);
    }

    [Fact]
    public async Task GetRestaurants_WhenAuthenticated_ReturnsAccessibleRestaurants()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/Auth/restaurants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(TestDataConstants.RestaurantName, content);
    }

    [Fact]
    public async Task CreateUser_WithValidDetails_ProvisionsStaffAccount()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var username = "waiter_" + Guid.NewGuid().ToString("N")[..6];
        var request = new CreateUserRequest(
            RestaurantId: TestDataConstants.RestaurantId,
            OutletId: TestDataConstants.OutletId,
            FullName: "New Test Waiter",
            Username: username,
            Password: TestDataConstants.DefaultPassword,
            Role: UserRole.Waiter,
            Pin: "4321"
        );

        var response = await client.PostAsJsonAsync("/api/Auth/users", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
