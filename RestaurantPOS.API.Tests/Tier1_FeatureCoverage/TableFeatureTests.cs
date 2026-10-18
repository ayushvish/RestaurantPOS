using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier1_FeatureCoverage;

public class TableFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public TableFeatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFloorsAndTables_ForOutlet_ReturnsSeededLayout()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var floors = await response.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        Assert.NotNull(floors);
        Assert.NotEmpty(floors);
        var mainFloor = floors.FirstOrDefault(f => f.Id == TestDataConstants.FloorId);
        Assert.NotNull(mainFloor);
        Assert.True(mainFloor.Tables.Count >= 3);
    }

    [Fact]
    public async Task CreateFloor_WithValidData_ReturnsCreatedFloor()
    {
        var client = _factory.CreateClient();
        var floorName = "Rooftop_" + Guid.NewGuid().ToString("N")[..4];
        var request = new CreateFloorRequest(TestDataConstants.OutletId, floorName, 2);

        var response = await client.PostAsJsonAsync("/api/Floor/floors", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var floor = await response.Content.ReadFromJsonAsync<Floor>(JsonOptions);
        Assert.NotNull(floor);
        Assert.Equal(floorName, floor.Name);
        Assert.Equal(TestDataConstants.OutletId, floor.OutletId);
    }

    [Fact]
    public async Task CreateTable_WithValidData_AddsTableToFloor()
    {
        var client = _factory.CreateClient();
        var tableNum = "T-" + new Random().Next(100, 999);
        var request = new CreateTableRequest(TestDataConstants.FloorId, tableNum, 4, 150, 200);

        var response = await client.PostAsJsonAsync("/api/Floor/tables", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var table = await response.Content.ReadFromJsonAsync<RestaurantTable>(JsonOptions);
        Assert.NotNull(table);
        Assert.Equal(tableNum, table.TableNumber);
        Assert.Equal(TableStatus.Vacant, table.Status);
    }

    [Fact]
    public async Task UpdateTableStatus_ToReserved_UpdatesStatusSuccessfully()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table3Id}/status", TableStatus.Reserved);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var table = await response.Content.ReadFromJsonAsync<RestaurantTable>(JsonOptions);
        Assert.NotNull(table);
        Assert.Equal(TableStatus.Reserved, table.Status);
    }

    [Fact]
    public async Task UpdateTableStatus_ToVacant_ClearsCurrentOrderId()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table2Id}/status", TableStatus.Vacant);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var table = await response.Content.ReadFromJsonAsync<RestaurantTable>(JsonOptions);
        Assert.NotNull(table);
        Assert.Equal(TableStatus.Vacant, table.Status);
        Assert.Null(table.CurrentOrderId);
    }

    [Fact]
    public async Task UpdateTableStatus_ToOccupied_SetsTableOccupied()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Occupied);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var table = await response.Content.ReadFromJsonAsync<RestaurantTable>(JsonOptions);
        Assert.NotNull(table);
        Assert.Equal(TableStatus.Occupied, table.Status);
    }
}
