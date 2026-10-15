using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Hubs;
using RestaurantPOS.API.Models;

namespace RestaurantPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FloorController : ControllerBase
{
    private readonly RestaurantPosContext _context;
    private readonly IHubContext<PosHub> _hubContext;

    public FloorController(RestaurantPosContext context, IHubContext<PosHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("outlets/{outletId}/floors")]
    public async Task<IActionResult> GetFloorsWithTables(Guid outletId)
    {
        var floors = await _context.Floors
            .Where(f => f.OutletId == outletId && f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .Include(f => f.Tables.Where(t => t.IsActive))
            .ToListAsync();

        return Ok(floors);
    }

    [HttpPost("floors")]
    public async Task<IActionResult> CreateFloor([FromBody] CreateFloorRequest req)
    {
        var floor = new Floor
        {
            OutletId = req.OutletId,
            Name = req.Name,
            DisplayOrder = req.DisplayOrder
        };

        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();
        return Ok(floor);
    }

    [HttpPost("tables")]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest req)
    {
        var table = new RestaurantTable
        {
            FloorId = req.FloorId,
            TableNumber = req.TableNumber,
            SeatingCapacity = req.SeatingCapacity,
            PosX = req.PosX,
            PosY = req.PosY,
            Status = TableStatus.Vacant
        };

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();
        return Ok(table);
    }

    [HttpPatch("tables/{tableId}/status")]
    public async Task<IActionResult> UpdateTableStatus(Guid tableId, [FromBody] TableStatus status)
    {
        var table = await _context.Tables
            .Include(t => t.Floor)
            .FirstOrDefaultAsync(t => t.Id == tableId);

        if (table == null) return NotFound();

        table.Status = status;
        if (status == TableStatus.Vacant)
        {
            table.CurrentOrderId = null;
        }

        await _context.SaveChangesAsync();

        if (table.Floor != null)
        {
            await _hubContext.Clients.Group($"outlet-{table.Floor.OutletId}")
                .SendAsync("TableStatusUpdated", new { tableId = table.Id, status = table.Status, currentOrderId = table.CurrentOrderId });
        }

        return Ok(table);
    }
}
