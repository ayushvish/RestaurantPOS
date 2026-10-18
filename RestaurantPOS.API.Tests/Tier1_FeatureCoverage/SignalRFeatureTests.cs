using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Tier1_FeatureCoverage;

public class OrderSettledPayload
{
    public Guid OrderId { get; set; }
    public decimal GrandTotal { get; set; }
}

public class SignalRFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public SignalRFeatureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetActiveKots_ReturnsNonServedKitchenTickets()
    {
        var client = _factory.CreateClient();

        // 1. Create order and fire KOT
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "KDS Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 1, 250.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);

        // 2. Query active KOTs
        var kotsRes = await client.GetAsync($"/api/Orders/outlet/{TestDataConstants.OutletId}/kots");
        Assert.Equal(HttpStatusCode.OK, kotsRes.StatusCode);
        var kotsJson = await kotsRes.Content.ReadAsStringAsync();
        Assert.Contains(order.OrderNumber, kotsJson);
    }

    [Fact]
    public async Task ToggleKotItemPrepared_TogglesIsPreparedFlag()
    {
        var client = _factory.CreateClient();

        // 1. Create order & fire KOT
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table2Id,
            CustomerName: "Item Toggle Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        var kotRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        var kot = await kotRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot);
        var itemId = kot.Items.First().Id;

        // 2. Toggle item prepared
        var toggleRes = await client.PostAsync($"/api/Orders/kots/items/{itemId}/toggle", null);
        Assert.Equal(HttpStatusCode.OK, toggleRes.StatusCode);
        var toggledItem = await toggleRes.Content.ReadFromJsonAsync<KotItem>(JsonOptions);
        Assert.NotNull(toggledItem);
        Assert.True(toggledItem.IsPrepared);
    }

    [Fact]
    public async Task UpdateKotStatus_TransitionsFromPreparingToServed()
    {
        var client = _factory.CreateClient();

        // 1. Create order & fire KOT
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table3Id,
            CustomerName: "Status Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 2, 120.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        var kotRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        var kot = await kotRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(kot);

        // 2. Set to Preparing
        var preparingRes = await client.PostAsJsonAsync($"/api/Orders/kots/{kot.Id}/status", new UpdateKotStatusRequest(KotStatus.Preparing));
        Assert.Equal(HttpStatusCode.OK, preparingRes.StatusCode);
        var preparingKot = await preparingRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(preparingKot);
        Assert.Equal(KotStatus.Preparing, preparingKot.Status);

        // 3. Set to Served
        var servedRes = await client.PostAsJsonAsync($"/api/Orders/kots/{kot.Id}/status", new UpdateKotStatusRequest(KotStatus.Served));
        Assert.Equal(HttpStatusCode.OK, servedRes.StatusCode);
        var servedKot = await servedRes.Content.ReadFromJsonAsync<KitchenOrderTicket>(JsonOptions);
        Assert.NotNull(servedKot);
        Assert.Equal(KotStatus.Served, servedKot.Status);
    }

    [Fact]
    public async Task SignalR_ConnectToHub_AndJoinOutletGroup_Succeeds()
    {
        var hubConnection = _factory.CreateSignalRConnection();

        await hubConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        await hubConnection.InvokeAsync("JoinOutletGroup", TestDataConstants.OutletId.ToString());
        await hubConnection.InvokeAsync("LeaveOutletGroup", TestDataConstants.OutletId.ToString());

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task SignalR_ReceivesTableStatusChangedEvent_WhenStatusUpdated()
    {
        var hubConnection = _factory.CreateSignalRConnection();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        hubConnection.On<string, string, string?>("TableStatusChanged", (tableId, status, orderId) =>
        {
            if (tableId == TestDataConstants.Table1Id.ToString() && status == TableStatus.Reserved.ToString())
            {
                tcs.TrySetResult(true);
            }
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinOutletGroup", TestDataConstants.OutletId.ToString());

        var client = _factory.CreateClient();
        var patchRes = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Reserved);
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(10000));
        Assert.Equal(tcs.Task, completed);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task SignalR_ReceivesRealTimeEvent_WhenOrderCreatedOnTable()
    {
        var hubConnection = _factory.CreateSignalRConnection();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        hubConnection.On<string, string, string?>("TableStatusChanged", (tableId, status, orderId) =>
        {
            if (status == TableStatus.Occupied.ToString())
            {
                tcs.TrySetResult(true);
            }
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinOutletGroup", TestDataConstants.OutletId.ToString());

        // Allow group registration to complete
        await Task.Delay(100);

        // Create Dine-In order on Table 2
        var client = _factory.CreateClient();
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table2Id,
            CustomerName: "SignalR Table Order Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Floor Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 1, 120.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);

        var received = await Task.WhenAny(tcs.Task, Task.Delay(10000));
        Assert.Equal(tcs.Task, received);

        await hubConnection.StopAsync();
    }
}
