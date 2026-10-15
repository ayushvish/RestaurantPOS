using Microsoft.AspNetCore.SignalR;

namespace RestaurantPOS.API.Hubs;

public class PosHub : Hub
{
    // Group clients by OutletId so multi-branch terminals only receive their outlet events
    public async Task JoinOutletGroup(string outletId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
    }

    public async Task LeaveOutletGroup(string outletId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
    }

    public async Task JoinKitchenStation(string outletId, string station)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{outletId}-{station}");
    }
}
