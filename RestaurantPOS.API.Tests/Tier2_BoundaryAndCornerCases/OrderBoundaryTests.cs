using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier2_BoundaryAndCornerCases;

public class OrderBoundaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public OrderBoundaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentOutlet_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var request = new CreateOrderRequest(
            OutletId: Guid.NewGuid(),
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Ghost Customer",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 1, 250.0m, null)
            }
        );

        var response = await client.PostAsJsonAsync("/api/Orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_WithNonExistentOrderId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentOrderId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/Orders/{nonExistentOrderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FireKot_WhenAllItemsAlreadyFired_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        // 1. Create order
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Fire Twice Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 1, 250.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // 2. First fire succeeds
        var fire1 = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, fire1.StatusCode);

        // 3. Second fire has no new items -> should return 400 BadRequest
        var fire2 = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.BadRequest, fire2.StatusCode);
    }

    [Fact]
    public async Task FireKot_ForNonExistentOrder_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentOrderId = Guid.NewGuid();

        var response = await client.PostAsync($"/api/Orders/{nonExistentOrderId}/fire-kot", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleKotItem_ForNonExistentItemId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentItemId = Guid.NewGuid();

        var response = await client.PostAsync($"/api/Orders/kots/items/{nonExistentItemId}/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateKotStatus_ForNonExistentKot_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentKotId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/Orders/kots/{nonExistentKotId}/status", new UpdateKotStatusRequest(KotStatus.Preparing));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AppendItems_ToNonExistentOrder_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentOrderId = Guid.NewGuid();
        var items = new List<OrderItemRequest>
        {
            new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, null)
        };

        var response = await client.PostAsJsonAsync($"/api/Orders/{nonExistentOrderId}/items", items);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
