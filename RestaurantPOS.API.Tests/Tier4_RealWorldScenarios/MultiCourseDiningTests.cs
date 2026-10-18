using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier4_RealWorldScenarios;

public class MultiCourseDiningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public MultiCourseDiningTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MultiCourseDining_SequentialKots_FullInventoryDeductionOnSettle()
    {
        var client = _factory.CreateClient();

        // Record initial inventory levels
        var matResInitial = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsInitial = await matResInitial.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var initialPaneer = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;
        var initialChicken = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialChickenId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;
        var initialMawa = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialMawaId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        // --- COURSE 1: Starters ---
        var course1Req = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "The Gourmand Family",
            CustomerPhone: "+919876543210",
            CustomerAddress: null,
            GuestCount: 4,
            WaiterName: "Captain Jack",
            SpecialNotes: "Fine dining multi-course service",
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 2, 250.0m, "Crisp edges")
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", course1Req);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // Fire KOT 1
        var kot1Res = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, kot1Res.StatusCode);
        var kot1 = await kot1Res.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot1);
        Assert.StartsWith("KOT-", kot1.KotNumber);
        Assert.Single(kot1.Items);

        // --- COURSE 2: Main Course ---
        var course2Items = new List<OrderItemRequest>
        {
            new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 2, 350.0m, "Medium spicy")
        };
        var append2Res = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/items", course2Items);
        Assert.Equal(HttpStatusCode.OK, append2Res.StatusCode);

        // Fire KOT 2
        var kot2Res = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, kot2Res.StatusCode);
        var kot2 = await kot2Res.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot2);
        Assert.Single(kot2.Items);
        Assert.Equal("Butter Chicken", kot2.Items.First().ItemName);

        // --- COURSE 3: Desserts ---
        var course3Items = new List<OrderItemRequest>
        {
            new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 4, 120.0m, "Served warm")
        };
        var append3Res = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/items", course3Items);
        Assert.Equal(HttpStatusCode.OK, append3Res.StatusCode);

        // Fire KOT 3
        var kot3Res = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.OK, kot3Res.StatusCode);
        var kot3 = await kot3Res.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot3);
        Assert.Single(kot3.Items);
        Assert.Equal("Gulab Jamun", kot3.Items.First().ItemName);

        // Total Courses Check:
        // Course 1: 2 * 250 = 500
        // Course 2: 2 * 350 = 700
        // Course 3: 4 * 120 = 480
        // SubTotal = 1680.00
        var fetchedOrderRes = await client.GetAsync($"/api/Orders/{order.Id}");
        var finalOrder = await fetchedOrderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(finalOrder);
        Assert.Equal(1680.0m, finalOrder.SubTotal);
        Assert.Equal(3, finalOrder.Kots.Count);

        // --- SETTLEMENT ---
        var settleReq = new SettleOrderRequest(
            PaymentMode: PaymentMode.Card,
            DiscountAmount: 80.0m,
            DiscountReason: "Family dining privilege discount",
            ServiceChargeAmount: 0.0m,
            PaymentTransactionRef: "TXN-MC-998811"
        );
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);

        // Verify Inventory Deductions across all 3 courses:
        // Course 1: 2 Paneer Tikka * 0.250 kg = 0.500 kg Paneer
        // Course 2: 2 Butter Chicken * 0.350 kg = 0.700 kg Raw Chicken
        // Course 3: 4 Gulab Jamun * 0.150 kg = 0.600 kg Mawa Khoya
        var matResFinal = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsFinal = await matResFinal.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var finalPaneer = matsFinal!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;
        var finalChicken = matsFinal!.First(m => m.Id == TestDataConstants.RawMaterialChickenId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;
        var finalMawa = matsFinal!.First(m => m.Id == TestDataConstants.RawMaterialMawaId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        Assert.Equal(initialPaneer - 0.500m, finalPaneer);
        Assert.Equal(initialChicken - 0.700m, finalChicken);
        Assert.Equal(initialMawa - 0.600m, finalMawa);
    }
}
