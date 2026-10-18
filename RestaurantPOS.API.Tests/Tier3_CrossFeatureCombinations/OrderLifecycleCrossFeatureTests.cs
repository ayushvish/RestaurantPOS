using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier3_CrossFeatureCombinations;

public class OrderLifecycleCrossFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public OrderLifecycleCrossFeatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullOrderLifecycle_TableOccupied_KotFired_ItemToggled_BillSettled_TableVacant_StockDeducted()
    {
        var client = _factory.CreateClient();

        // Check initial Paneer & Mawa stock
        var matResInitial = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsInitial = await matResInitial.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var initialPaneer = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;
        var initialMawa = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialMawaId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        // 1. Create Dine-In Order on Table 1
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Lifecycle VIP",
            CustomerPhone: "+919876500000",
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Floor Waiter",
            SpecialNotes: "Window seating",
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 2, 250.0m, "Crispy"),
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 1, 120.0m, "Warm")
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Active, order.Status);

        // Verify Table 1 is now Occupied
        var floorsRes = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var floors = await floorsRes.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var table = floors?.SelectMany(f => f.Tables).First(t => t.Id == TestDataConstants.Table1Id);
        Assert.NotNull(table);
        Assert.Equal(TableStatus.Occupied, table.Status);

        // 2. Fire KOT
        var fireRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, fireRes.StatusCode);
        var kot = await fireRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot);
        Assert.Equal(KotStatus.Pending, kot.Status);
        Assert.Equal(2, kot.Items.Count);

        // 3. Kitchen toggles first item as Prepared
        var itemToToggle = kot.Items.First();
        var toggleRes = await client.PostAsync($"/api/Orders/kots/items/{itemToToggle.Id}/toggle", null);
        Assert.Equal(HttpStatusCode.OK, toggleRes.StatusCode);
        var toggled = await toggleRes.Content.ReadFromJsonAsync<KotItem>(JsonOptions);
        Assert.NotNull(toggled);
        Assert.True(toggled.IsPrepared);

        // 4. Mark KOT as Served
        var kotStatusRes = await client.PostAsJsonAsync($"/api/Orders/kots/{kot.Id}/status", new UpdateKotStatusRequest(KotStatus.Served));
        Assert.Equal(HttpStatusCode.OK, kotStatusRes.StatusCode);

        // 5. Settle the Bill with Cash & 20 discount
        var settleReq = new SettleOrderRequest(PaymentMode.Cash, 20.0m, "Loyalty coupon", 0.0m, null);
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);
        var settledOrder = await settleRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(settledOrder);
        Assert.True(settledOrder.IsPaid);
        Assert.Equal(OrderStatus.Paid, settledOrder.Status);

        // 6. Verify Table 1 released to Vacant
        var floorsResAfter = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var floorsAfter = await floorsResAfter.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var tableAfter = floorsAfter?.SelectMany(f => f.Tables).First(t => t.Id == TestDataConstants.Table1Id);
        Assert.NotNull(tableAfter);
        Assert.Equal(TableStatus.Vacant, tableAfter.Status);
        Assert.Null(tableAfter.CurrentOrderId);

        // 7. Verify Inventory Deductions:
        // 2 Paneer Tikka * 0.250 kg = 0.500 kg Paneer
        // 1 Gulab Jamun * 0.150 kg = 0.150 kg Mawa
        var matResFinal = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsFinal = await matResFinal.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var finalPaneer = matsFinal!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;
        var finalMawa = matsFinal!.First(m => m.Id == TestDataConstants.RawMaterialMawaId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        Assert.Equal(initialPaneer - 0.500m, finalPaneer);
        Assert.Equal(initialMawa - 0.150m, finalMawa);
    }

    [Fact]
    public async Task MultipleOrders_OnSeparateTables_DoNotInterfere()
    {
        var client = _factory.CreateClient();

        // 1. Create Order A on Table 2
        var orderAReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table2Id,
            CustomerName: "Customer Table 2",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Waiter A",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, null)
            }
        );
        var orderARes = await client.PostAsJsonAsync("/api/Orders", orderAReq);
        var orderA = await orderARes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(orderA);

        // 2. Create Order B on Table 3
        var orderBReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table3Id,
            CustomerName: "Customer Table 3",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 4,
            WaiterName: "Waiter B",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 2, 250.0m, null)
            }
        );
        var orderBRes = await client.PostAsJsonAsync("/api/Orders", orderBReq);
        var orderB = await orderBRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(orderB);

        // 3. Fire KOT for Order A only
        var fireARes = await client.PostAsync($"/api/Orders/{orderA.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, fireARes.StatusCode);

        // Check active KOTs - should contain Order A, but NOT Order B
        var kotsRes = await client.GetAsync($"/api/Orders/outlet/{TestDataConstants.OutletId}/kots");
        var kots = await kotsRes.Content.ReadAsStringAsync();
        Assert.Contains(orderA.OrderNumber, kots);
        Assert.DoesNotContain(orderB.OrderNumber, kots);

        // 4. Settle Order A - Table 2 should become Vacant, while Table 3 remains Occupied
        var settleAReq = new SettleOrderRequest(PaymentMode.Cash, 0.0m, null, 0.0m, null);
        await client.PostAsJsonAsync($"/api/Orders/{orderA.Id}/settle", settleAReq);

        var floorsRes = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var floors = await floorsRes.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var table2 = floors?.SelectMany(f => f.Tables).First(t => t.Id == TestDataConstants.Table2Id);
        var table3 = floors?.SelectMany(f => f.Tables).First(t => t.Id == TestDataConstants.Table3Id);

        Assert.NotNull(table2);
        Assert.NotNull(table3);
        Assert.Equal(TableStatus.Vacant, table2.Status);
        Assert.Equal(TableStatus.Occupied, table3.Status);

        // 5. Cleanly settle Order B
        var settleBReq = new SettleOrderRequest(PaymentMode.Card, 0.0m, null, 0.0m, "TXN-TB3");
        var settleBRes = await client.PostAsJsonAsync($"/api/Orders/{orderB.Id}/settle", settleBReq);
        Assert.Equal(HttpStatusCode.OK, settleBRes.StatusCode);
    }
}
