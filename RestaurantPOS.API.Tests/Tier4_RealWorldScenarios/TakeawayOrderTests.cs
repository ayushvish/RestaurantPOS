using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier4_RealWorldScenarios;

public class TakeawayOrderTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public TakeawayOrderTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TakeawayOrder_WithoutTable_LeavesTablesUntouchedAndProcessesCleanly()
    {
        var client = _factory.CreateClient();

        // 1. Check all tables initial status
        var initialFloorsRes = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var initialFloors = await initialFloorsRes.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var initialTables = initialFloors!.SelectMany(f => f.Tables).ToList();
        var vacantCountBefore = initialTables.Count(t => t.Status == TableStatus.Vacant);

        // 2. Create Takeaway Order (tableId: null)
        var takeawayReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.Takeaway,
            TableId: null,
            CustomerName: "Express Pickup Guest",
            CustomerPhone: "+919123456789",
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: "Pack with cutlery",
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, "Spicy"),
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 2, 120.0m, null)
            }
        );

        var orderRes = await client.PostAsJsonAsync("/api/Orders", takeawayReq);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(OrderType.Takeaway, order.OrderType);
        Assert.Null(order.TableId);

        // 3. Verify tables remain unchanged
        var afterFloorsRes = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var afterFloors = await afterFloorsRes.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var vacantCountAfter = afterFloors!.SelectMany(f => f.Tables).Count(t => t.Status == TableStatus.Vacant);
        Assert.Equal(vacantCountBefore, vacantCountAfter);

        // 4. Fire KOT
        var fireRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, fireRes.StatusCode);
        var kot = await fireRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot);
        Assert.Equal(2, kot.Items.Count);

        // 5. Verify KDS query lists tableNumber as "Takeaway"
        var kdsRes = await client.GetAsync($"/api/Orders/outlet/{TestDataConstants.OutletId}/kots");
        Assert.Equal(HttpStatusCode.OK, kdsRes.StatusCode);
        var kdsJson = await kdsRes.Content.ReadAsStringAsync();
        Assert.Contains("Takeaway", kdsJson);

        // 6. Settle Order via UPI
        var settleReq = new SettleOrderRequest(
            PaymentMode: PaymentMode.UPI,
            DiscountAmount: 0.0m,
            DiscountReason: null,
            ServiceChargeAmount: 0.0m,
            PaymentTransactionRef: "UPI-TAKEAWAY-556677"
        );
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);
        var settledOrder = await settleRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(settledOrder);
        Assert.True(settledOrder.IsPaid);
        Assert.Equal(OrderStatus.Paid, settledOrder.Status);

        // 7. Verify tables are still unchanged
        var finalFloorsRes = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var finalFloors = await finalFloorsRes.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var vacantCountFinal = finalFloors!.SelectMany(f => f.Tables).Count(t => t.Status == TableStatus.Vacant);
        Assert.Equal(vacantCountBefore, vacantCountFinal);
    }
}
