using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier1_FeatureCoverage;

public class OrderKotFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public OrderKotFeatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrder_DineIn_CalculatesTotalsAndMarksTableOccupied()
    {
        var client = _factory.CreateClient();

        var request = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Alice Wonderland",
            CustomerPhone: "+919988776655",
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Captain Jack",
            SpecialNotes: "Mild spice please",
            Items: new List<OrderItemRequest>
            {
                new(
                    MenuItemId: TestDataConstants.PaneerTikkaId,
                    RawMaterialId: null,
                    ItemName: "Paneer Tikka",
                    VariantName: null,
                    Quantity: 2,
                    UnitPrice: 250.0m,
                    Notes: "Crispy"
                )
            }
        );

        var response = await client.PostAsJsonAsync("/api/Orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.Equal(500.0m, order.SubTotal);
        Assert.Equal(25.0m, order.TaxAmount); // 5% GST
        Assert.Equal(525.0m, order.GrandTotal);

        // Verify table status is now Occupied
        var tableResponse = await client.GetAsync($"/api/Floor/outlets/{TestDataConstants.OutletId}/floors");
        var floors = await tableResponse.Content.ReadFromJsonAsync<List<Floor>>(JsonOptions);
        var table = floors?.SelectMany(f => f.Tables).FirstOrDefault(t => t.Id == TestDataConstants.Table1Id);
        Assert.NotNull(table);
        Assert.Equal(TableStatus.Occupied, table.Status);
    }

    [Fact]
    public async Task GetOrderById_ReturnsOrderDetailsWithItems()
    {
        var client = _factory.CreateClient();

        // Create order first
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table2Id,
            CustomerName: "Bob Smith",
            CustomerPhone: "+919988112233",
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, null)
            }
        );
        var createRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var createdOrder = await createRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(createdOrder);

        var response = await client.GetAsync($"/api/Orders/{createdOrder.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetchedOrder = await response.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(fetchedOrder);
        Assert.Equal(createdOrder.Id, fetchedOrder.Id);
        Assert.NotEmpty(fetchedOrder.Items);
    }

    [Fact]
    public async Task AppendItems_ToRunningOrder_AddsItemsAndRecalculatesTotals()
    {
        var client = _factory.CreateClient();

        // 1. Create order with 1 item
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table3Id,
            CustomerName: "Charlie Brown",
            CustomerPhone: "+919988000000",
            CustomerAddress: null,
            GuestCount: 3,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 1, 250.0m, null)
            }
        );
        var createRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await createRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(250.0m, order.SubTotal);

        // 2. Append Butter Chicken
        var appendItems = new List<OrderItemRequest>
        {
            new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, "Less spicy")
        };
        var appendRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/items", appendItems);

        Assert.Equal(HttpStatusCode.OK, appendRes.StatusCode);
        var updatedOrder = await appendRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(updatedOrder);
        Assert.Equal(2, updatedOrder.Items.Count);
        Assert.Equal(600.0m, updatedOrder.SubTotal);
    }

    [Fact]
    public async Task FireKot_WhenUnfiredItemsExist_CreatesKitchenOrderTicket()
    {
        var client = _factory.CreateClient();

        // 1. Create order
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "David Miller",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 2, 250.0m, "Crispy")
            }
        );
        var createRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await createRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // 2. Fire KOT
        var fireRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);

        Assert.Equal(HttpStatusCode.OK, fireRes.StatusCode);
        var kot = await fireRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot);
        Assert.Equal(order.Id, kot.OrderId);
        Assert.StartsWith("KOT-", kot.KotNumber);
        Assert.Equal(KotStatus.Pending, kot.Status);
        Assert.NotEmpty(kot.Items);
    }

    [Fact]
    public async Task GetActiveOrders_ForOutlet_ReturnsUnpaidOrders()
    {
        var client = _factory.CreateClient();

        // Create an order
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table2Id,
            CustomerName: "Eva Green",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 2, 120.0m, null)
            }
        );
        await client.PostAsJsonAsync("/api/Orders", createReq);

        var response = await client.GetAsync($"/api/Orders/outlet/{TestDataConstants.OutletId}/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var activeOrders = await response.Content.ReadFromJsonAsync<List<Order>>(JsonOptions);
        Assert.NotNull(activeOrders);
        Assert.NotEmpty(activeOrders);
        Assert.All(activeOrders, o => Assert.False(o.IsPaid));
    }
}
