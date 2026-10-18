using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;
using Xunit;

namespace RestaurantPOS.API.Tests.Adversarial;

public class BackendAdversarialChallengerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Options;

    public BackendAdversarialChallengerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // =========================================================================
    // 1. SUPERADMIN LOGIN & NULL RESTAURANT ID CHECKS
    // =========================================================================

    [Fact]
    public async Task SuperAdmin_Login_GeneratesValidToken_AndAllowsDirectoryAccessWithoutNRE()
    {
        var client = _factory.CreateClient();
        var loginReq = new LoginRequest("superadmin", TestDataConstants.DefaultPassword);

        var loginRes = await client.PostAsJsonAsync("/api/Auth/login", loginReq);
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var authRes = await loginRes.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(authRes);
        Assert.Null(authRes.RestaurantId);
        Assert.Null(authRes.OutletId);
        Assert.Equal("SuperAdmin", authRes.Role);

        // Authenticate with SuperAdmin token
        AuthHelper.SetBearerToken(client, authRes.Token);

        // Verify SuperAdmin can query restaurants
        var restRes = await client.GetAsync("/api/Auth/restaurants");
        Assert.Equal(HttpStatusCode.OK, restRes.StatusCode);

        // Verify SuperAdmin can query users across platform
        var usersRes = await client.GetAsync($"/api/Auth/users/{TestDataConstants.RestaurantId}");
        Assert.Equal(HttpStatusCode.OK, usersRes.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_CreateRestaurant_WithEmptyName_ReturnsBadRequest()
    {
        var client = await _factory.CreateSuperAdminClientAsync();
        var emptyNameReq = new SuperAdminCreateRestaurantRequest("", "Tagline", "Outlet", "Floor");

        var response = await client.PostAsJsonAsync("/api/Auth/restaurants/admin", emptyNameReq);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =========================================================================
    // 2. DOUBLE SETTLEMENT & INVENTORY IDEMPOTENCY CHECKS
    // =========================================================================

    [Fact]
    public async Task DoubleSettlement_SequentialAttempts_ReturnsBadRequestAndDoesNotDeductStockTwice()
    {
        var client = _factory.CreateClient();

        // Check initial Paneer stock
        var matResInitial = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsInitial = await matResInitial.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var initialStock = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        // Create Dine-In order with 2x Paneer Tikka (0.250 kg Paneer per unit -> 0.500 kg total)
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table1Id,
            CustomerName: "Double Settle Adversarial",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 2,
            WaiterName: "Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 2, 250.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // 1st Settlement
        var settleReq = new SettleOrderRequest(PaymentMode.Cash, 0.0m, null, 0.0m, null);
        var settle1Res = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.OK, settle1Res.StatusCode);

        // Check stock after 1st settlement
        var matRes1 = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var mats1 = await matRes1.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var stockAfter1st = mats1!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        Assert.Equal(initialStock - 0.500m, stockAfter1st);

        // 2nd Settlement attempt - must be rejected
        var settle2Res = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        Assert.Equal(HttpStatusCode.BadRequest, settle2Res.StatusCode);

        var probDetails = await settle2Res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(probDetails);
        Assert.Equal("Order already settled", probDetails.Title);

        // Check stock after 2nd settlement attempt - must remain EXACTLY stockAfter1st
        var matRes2 = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var mats2 = await matRes2.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var stockAfter2nd = mats2!.First(m => m.Id == TestDataConstants.RawMaterialPaneerId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        Assert.Equal(stockAfter1st, stockAfter2nd);
    }

    [Fact]
    public async Task DoubleSettlement_ConcurrentRequests_DoesNotThrow500_AndStockDeductedCorrectly()
    {
        var client = _factory.CreateClient();

        // Check initial Butter Chicken (Raw Chicken) stock
        var matResInitial = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsInitial = await matResInitial.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var initialChicken = matsInitial!.First(m => m.Id == TestDataConstants.RawMaterialChickenId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        // Create Dine-In order with 1x Butter Chicken (0.350 kg Chicken)
        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table2Id,
            CustomerName: "Concurrent Settle Adversarial",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: "Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.ButterChickenId, null, "Butter Chicken", null, 1, 350.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // Fire 2 concurrent settle requests
        var settleReq = new SettleOrderRequest(PaymentMode.Card, 0.0m, null, 0.0m, "TXN-CONCURRENT");
        var task1 = client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);
        var task2 = client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", settleReq);

        var responses = await Task.WhenAll(task1, task2);

        // Neither response should be 500 Internal Server Error
        Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.InternalServerError);

        // At least one should be OK
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);

        // Verify stock was not deducted twice (should be initial - 0.350m)
        var matResFinal = await client.GetAsync($"/api/Inventory/materials/{TestDataConstants.RestaurantId}");
        var matsFinal = await matResFinal.Content.ReadFromJsonAsync<List<RawMaterial>>(JsonOptions);
        var finalChicken = matsFinal!.First(m => m.Id == TestDataConstants.RawMaterialChickenId)
            .Stocks.First(s => s.OutletId == TestDataConstants.OutletId).CurrentStockQuantity;

        Assert.Equal(initialChicken - 0.350m, finalChicken);
    }

    // =========================================================================
    // 3. CONCURRENT OR OUT-OF-ORDER KOT FIRING CHECKS
    // =========================================================================

    [Fact]
    public async Task FireKot_ConcurrentRequests_DoesNotThrow500_AndHandlesGracefully()
    {
        var client = _factory.CreateClient();

        var createReq = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.DineIn,
            TableId: TestDataConstants.Table3Id,
            CustomerName: "Concurrent KOT Adversarial",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: "Waiter",
            SpecialNotes: null,
            Items: new List<OrderItemRequest>
            {
                new(TestDataConstants.GulabJamunId, null, "Gulab Jamun", null, 2, 120.0m, null)
            }
        );
        var orderRes = await client.PostAsJsonAsync("/api/Orders", createReq);
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // Send two concurrent fire-kot requests
        var task1 = client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        var task2 = client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);

        var responses = await Task.WhenAll(task1, task2);

        // Neither response should be 500
        Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.InternalServerError);

        // At least one must be OK (or one OK, one BadRequest because items already fired)
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateKotStatus_NonExistentKot_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        var res = await client.PostAsJsonAsync($"/api/Orders/kots/{nonExistentId}/status", new UpdateKotStatusRequest(KotStatus.Served));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("KOT not found", prob.Title);
    }

    [Fact]
    public async Task ToggleKotItem_NonExistentItem_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        var res = await client.PostAsync($"/api/Orders/kots/items/{nonExistentId}/toggle", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("KOT item not found", prob.Title);
    }

    // =========================================================================
    // 4. EMPTY CART & INVALID IDS (RFC 7807 PROBLEM DETAILS CHECKS)
    // =========================================================================

    [Fact]
    public async Task CreateOrder_WithNonExistentOutlet_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var badOutletId = Guid.NewGuid();

        var req = new CreateOrderRequest(
            OutletId: badOutletId,
            OrderType: OrderType.Takeaway,
            TableId: null,
            CustomerName: "Ghost Outlet Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>()
        );

        var res = await client.PostAsJsonAsync("/api/Orders", req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("Outlet not found", prob.Title);
    }

    [Fact]
    public async Task GetOrder_WithNonExistentOrderId_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var badOrderId = Guid.NewGuid();

        var res = await client.GetAsync($"/api/Orders/{badOrderId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("Order not found", prob.Title);
    }

    [Fact]
    public async Task SettleOrder_WithNonExistentOrderId_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var badOrderId = Guid.NewGuid();
        var req = new SettleOrderRequest(PaymentMode.Cash, 0m, null, 0m, null);

        var res = await client.PostAsJsonAsync($"/api/Orders/{badOrderId}/settle", req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("Order not found", prob.Title);
    }

    [Fact]
    public async Task AppendItems_WithNonExistentOrderId_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var badOrderId = Guid.NewGuid();
        var items = new List<OrderItemRequest>
        {
            new(TestDataConstants.PaneerTikkaId, null, "Paneer Tikka", null, 1, 250m, null)
        };

        var res = await client.PostAsJsonAsync($"/api/Orders/{badOrderId}/items", items);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("Order not found", prob.Title);
    }

    [Fact]
    public async Task UpdateTableStatus_WithNonExistentTableId_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateClient();
        var badTableId = Guid.NewGuid();

        var res = await client.PatchAsJsonAsync($"/api/Floor/tables/{badTableId}/status", TableStatus.Occupied);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var prob = await res.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);
        Assert.NotNull(prob);
        Assert.Equal("Table not found", prob.Title);
    }

    // =========================================================================
    // 5. SIGNALR HUB GROUP SUBSCRIPTION & TENANCY ISOLATION CHECKS
    // =========================================================================

    [Fact]
    public async Task SignalR_TenantIsolation_ClientInDifferentOutlet_DoesNotReceiveBroadcasts()
    {
        var hubConn1 = _factory.CreateSignalRConnection();
        var hubConn2 = _factory.CreateSignalRConnection();

        var conn1Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var conn2Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        hubConn1.On<string, string, string?>("TableStatusChanged", (tableId, status, orderId) =>
        {
            if (tableId == TestDataConstants.Table1Id.ToString())
            {
                conn1Received.TrySetResult(true);
            }
        });

        hubConn2.On<string, string, string?>("TableStatusChanged", (tableId, status, orderId) =>
        {
            if (tableId == TestDataConstants.Table1Id.ToString())
            {
                conn2Received.TrySetResult(true);
            }
        });

        await hubConn1.StartAsync();
        await hubConn2.StartAsync();

        // HubConn1 joins valid outlet
        await hubConn1.InvokeAsync("JoinOutletGroup", TestDataConstants.OutletId.ToString());

        // HubConn2 joins a completely different outlet
        var otherOutletId = Guid.NewGuid().ToString();
        await hubConn2.InvokeAsync("JoinOutletGroup", otherOutletId);

        // Allow SignalR group memberships to register
        await Task.Delay(100);

        // Trigger TableStatusChanged in valid outlet
        var client = _factory.CreateClient();
        var patchRes = await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Reserved);
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);

        // HubConn1 MUST receive the event
        var conn1Task = await Task.WhenAny(conn1Received.Task, Task.Delay(3000));
        Assert.Equal(conn1Received.Task, conn1Task);

        // HubConn2 MUST NOT receive the event
        var conn2Task = await Task.WhenAny(conn2Received.Task, Task.Delay(500));
        Assert.NotEqual(conn2Received.Task, conn2Task);

        await hubConn1.StopAsync();
        await hubConn2.StopAsync();
    }

    [Fact]
    public async Task SignalR_LeaveOutletGroup_StopsReceivingEvents()
    {
        var hubConn = _factory.CreateSignalRConnection();
        var eventCount = 0;

        hubConn.On<string, string, string?>("TableStatusChanged", (tableId, status, orderId) =>
        {
            if (tableId == TestDataConstants.Table1Id.ToString())
            {
                eventCount++;
            }
        });

        await hubConn.StartAsync();
        await hubConn.InvokeAsync("JoinOutletGroup", TestDataConstants.OutletId.ToString());
        await Task.Delay(100);

        // First status change - must be received
        var client = _factory.CreateClient();
        await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Occupied);
        await Task.Delay(200);
        Assert.Equal(1, eventCount);

        // Now leave group
        await hubConn.InvokeAsync("LeaveOutletGroup", TestDataConstants.OutletId.ToString());
        await Task.Delay(100);

        // Second status change - should NOT be received
        await client.PatchAsJsonAsync($"/api/Floor/tables/{TestDataConstants.Table1Id}/status", TableStatus.Vacant);
        await Task.Delay(200);
        Assert.Equal(1, eventCount);

        await hubConn.StopAsync();
    }

    // =========================================================================
    // 6. DEEP ADVERSARIAL STRESS PROBES: PAID ORDER & EMPTY CART
    // =========================================================================

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ReturnsOkAndZeroTotalWithoutCrashing()
    {
        var client = _factory.CreateClient();
        var req = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.Takeaway,
            TableId: null,
            CustomerName: "Empty Cart Tester",
            CustomerPhone: null,
            CustomerAddress: null,
            GuestCount: 1,
            WaiterName: null,
            SpecialNotes: null,
            Items: new List<OrderItemRequest>()
        );

        var res = await client.PostAsJsonAsync("/api/Orders", req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var order = await res.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(0m, order.SubTotal);
        Assert.Equal(0m, order.GrandTotal);

        // Firing KOT on an empty order should return 400 BadRequest ("No new items to fire to kitchen.")
        var fireRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);
        Assert.Equal(HttpStatusCode.BadRequest, fireRes.StatusCode);
    }

    [Fact]
    public async Task FireKot_OnPaidOrder_DoesNotThrow500_ObservesContract()
    {
        var client = _factory.CreateClient();
        var req = new CreateOrderRequest(
            OutletId: TestDataConstants.OutletId,
            OrderType: OrderType.Takeaway,
            TableId: null,
            CustomerName: "Settle First Tester",
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

        var orderRes = await client.PostAsJsonAsync("/api/Orders", req);
        var order = await orderRes.Content.ReadFromJsonAsync<Order>(JsonOptions);
        Assert.NotNull(order);

        // Settle immediately before firing KOT
        var settleRes = await client.PostAsJsonAsync($"/api/Orders/{order.Id}/settle", new SettleOrderRequest(PaymentMode.Cash, 0m, null, 0m, null));
        Assert.Equal(HttpStatusCode.OK, settleRes.StatusCode);

        // Now attempt to fire KOT
        var fireRes = await client.PostAsync($"/api/Orders/{order.Id}/fire-kot", null);

        // Verify it does NOT crash with 500
        Assert.NotEqual(HttpStatusCode.InternalServerError, fireRes.StatusCode);
    }
}

