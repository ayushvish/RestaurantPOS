using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier1_FeatureCoverage;

public class InventoryFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public InventoryFeatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRawMaterials_ForRestaurant_ReturnsMaterialsWithStock()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var materials = await response.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        Assert.NotNull(materials);
        Assert.True(materials.Count >= 3);
        var paneer = materials.FirstOrDefault(m => m.Id == TestDataConstants.RawMaterialPaneerId);
        Assert.NotNull(paneer);
        Assert.Equal("Paneer Block", paneer.Name);
    }

    [Fact]
    public async Task CreateRawMaterial_WithValidData_ReturnsCreatedMaterial()
    {
        var client = _factory.CreateClient();
        var matName = "Cooking Oil " + Guid.NewGuid().ToString("N")[..4];
        var request = new CreateRawMaterialRequest(matName, "ltr", 150.0m, 10.0m);

        var response = await client.PostAsJsonAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RawMaterial>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(matName, created.Name);
        Assert.Equal("ltr", created.UnitOfMeasure);
    }

    [Fact]
    public async Task AdjustStock_IncreasesOutletStockQuantity()
    {
        var client = _factory.CreateClient();

        // Add 5.0 kg of Paneer Block
        var request = new UpdateStockRequest(TestDataConstants.RawMaterialPaneerId, 5.0m, "Restock delivery batch");

        var response = await client.PostAsJsonAsync($"/api/Inventory/stock/{TestDataConstants.OutletId}/adjust", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = await response.Content.ReadFromJsonAsync<InventoryStock>(JsonOptions);
        Assert.NotNull(stock);
        Assert.True(stock.CurrentStockQuantity >= 25.0m);
    }

    [Fact]
    public async Task SetRecipeBom_ForMenuItem_ReplacesRecipeItems()
    {
        var client = _factory.CreateClient();

        var recipe = new List<RecipeItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                MenuItemId = TestDataConstants.PaneerTikkaId,
                RawMaterialId = TestDataConstants.RawMaterialPaneerId,
                QuantityRequired = 0.300m
            }
        };

        var response = await client.PostAsJsonAsync($"/api/Inventory/recipes/{TestDataConstants.PaneerTikkaId}", recipe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var savedRecipes = await response.Content.ReadFromJsonAsync<List<RecipeItem>>(JsonOptions);
        Assert.NotNull(savedRecipes);
        Assert.Single(savedRecipes);
        Assert.Equal(0.300m, savedRecipes[0].QuantityRequired);
    }

    [Fact]
    public async Task SettleOrder_DeductsRecipeIngredientsFromInventory()
    {
        var client = _factory.CreateClient();

        // 1. Check current stock of Raw Chicken
        var matResBefore = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsBefore = await matResBefore.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var chickenBefore = matsBefore!.First(m => m.Id == TestDataConstants.RawMaterialChickenId);
        var stockBefore = chickenBefore.Stocks.FirstOrDefault(s => s.OutletId == TestDataConstants.OutletId)?.CurrentStockQuantity ?? 0m;

        // 2. Create order with 2 Butter Chicken (each requires 0.350 kg => 0.700 kg total)
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Inventory Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 2, 350.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // 3. Settle Order
        var settleReq = new SettleOrderRequest(PaymentMode.Card, 0.0m, null, 0.0m, "TXN-998877");
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);

        // 4. Verify Stock Deduction
        var matResAfter = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsAfter = await matResAfter.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var chickenAfter = matsAfter!.First(m => m.Id == TestDataConstants.RawMaterialChickenId);
        var stockAfter = chickenAfter.Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        Assert.Equal(stockBefore - 0.700m, stockAfter);
    }
}
