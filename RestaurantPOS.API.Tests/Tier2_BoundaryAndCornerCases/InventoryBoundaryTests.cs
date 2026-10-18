using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier2_BoundaryAndCornerCases;

public class InventoryBoundaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public InventoryBoundaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SettleOrder_WhenAlreadySettled_ReturnsBadRequestIdempotencyError()
    {
        var client = _factory.CreateClient();

        // 1. Create order
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Double Settle Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 1, 120.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // 2. First settle succeeds
        var settleReq = new SettleOrderRequest(PaymentMode.Cash, 0.0m, null, 0.0m, null);
        var settle1 = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.OK, settle1.StatusCode);

        // 3. Second settle must be rejected with 400 BadRequest
        var settle2 = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.BadRequest, settle2.StatusCode);
    }

    [Fact]
    public async Task SettleOrder_ForNonExistentOrderId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var nonExistentOrderId = Guid.NewGuid();
        var settleReq = new SettleOrderRequest(PaymentMode.Cash, 0.0m, null, 0.0m, null);

        var response = await client.PostAsJsonAsync($"/api/Orders/{nonExistentOrderId}/settle", settleReq);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SettleOrder_WhenMenuItemHasNoRecipe_SettlesWithoutStockError()
    {
        var client = _factory.CreateClient();

        // 1. Create menu item without recipe
        var itemReq = new CreateMenuItemRequest(
            CategoryId: TestDataConstants.CategoryStartersId,
            Name: "No Recipe Dish",
            ShortCode: "NRD",
            Description: null,
            ImageUrl: null,
            BasePrice: 100.0m,
            DeliveryPrice: 100.0m,
            IsVegetarian: true,
            GstPercent: 5.0m,
            KitchenStation: "Kitchen",
            Variants: null,
            ModifierGroups: null
        );
        var itemRes = await client.PostAsJsonAsync($"/api/Menu/items/{TestDataConstants.RestaurantId}", itemReq);
        var createdItem = await itemRes.Content.ReadFromJsonAsync<MenuItem>(JsonOptions);
        Assert.NotNull(createdItem);

        // 2. Order it
        var orderReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.Takeaway,
            TableId: null,
            CustomerName: "No Recipe Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(createdItem.Id, null, createdItem.Name, null, 1, 100.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", orderReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // 3. Settle it - must succeed without crashing
        var settleReq = new SettleOrderRequest(PaymentMode.UPI, 0.0m, null, 0.0m, "UPI-REF-1234");
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);

        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);
    }

    [Fact]
    public async Task SettleOrder_WithDiscount_AppliesDiscountAndRecalculatesGrandTotal()
    {
        var client = _factory.CreateClient();

        var orderReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.Takeaway,
            TableId: null,
            CustomerName: "Discount Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", orderReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // Settle with 50.0 discount
        var settleReq = new SettleOrderRequest(PaymentMode.Cash, 50.0m, "Loyalty Discount", 0.0m, null);
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);

        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);
        var settledOrder = await settleRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(settledOrder);
        Assert.Equal(50.0m, settledOrder.DiscountAmount);
        Assert.True(settledOrder.GrandTotal < 350.0m * 1.05m);
    }
}
