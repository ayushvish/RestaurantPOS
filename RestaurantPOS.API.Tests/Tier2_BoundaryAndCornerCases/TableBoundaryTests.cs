using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier2_BoundaryAndCornerCases;

public class TableBoundaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public TableBoundaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateTableStatus_ForNonExistentTable_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentTableId = Guid.NewGuid();

        var response = await client.PatchAsJsonAsync($"/api/Floor/tables/{nonExistentTableId}/status", TableStatus.Occupied);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFloors_ForNonExistentOutlet_ReturnsEmptyList()
    {
        var client = _factory.CreateClient();
        var nonExistentOutletId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/Floor/outlets/{nonExistentOutletId}/floors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var floors = await response.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        Assert.NotNull(floors);
        Assert.Empty(floors);
    }

    [Fact]
    public async Task UpdateTableStatus_ToSameStatus_IsIdempotent()
    {
        var client = _factory.CreateClient();

        // Table 1 set to Occupied twice
        var res1 = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Occupied);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        var res2 = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Occupied);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var table = await res2.Content.ReadFromJsonAsync<RestaurantTable>(JsonOptions);
        Assert.NotNull(table);
        Assert.Equal(TableStatus.Occupied, table.Status);
    }
}
