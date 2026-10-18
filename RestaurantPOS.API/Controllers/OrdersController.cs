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
public class OrdersController : ControllerBase
{
    private readonly RestaurantPosContext _context;
    private readonly IHubContext<PosHub> _hubContext;

    public OrdersController(RestaurantPosContext context, IHubContext<PosHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("outlet/{outletId}/active")]
    public async Task<IActionResult> GetActiveOrders(Guid outletId)
    {
        var orders = await _context.Orders
            .Where(o => o.OutletId == outletId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled)
            .Include(o => o.Items)
            .Include(o => o.Table)
            .Include(o => o.Kots).ThenInclude(k => k.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var outlet = await _context.Outlets
            .Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == req.OutletId);

        if (outlet == null) return NotFound(new ProblemDetails { Title = "Outlet not found", Detail = "Outlet not found." });

        var orderCountToday = await _context.Orders
            .CountAsync(o => o.OutletId == req.OutletId && o.CreatedAt.Date == DateTime.UtcNow.Date);

        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{(orderCountToday + 1):D3}";

        var order = new Order
        {
            OutletId = req.OutletId,
            OrderNumber = orderNumber,
            OrderType = req.OrderType,
            Status = OrderStatus.Active,
            TableId = req.TableId,
            CustomerName = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            CustomerAddress = req.CustomerAddress,
            GuestCount = req.GuestCount,
            WaiterName = req.WaiterName,
            SpecialNotes = req.SpecialNotes
        };

        decimal subtotal = 0;
        foreach (var itemDto in req.Items)
        {
            var total = itemDto.UnitPrice * itemDto.Quantity;
            subtotal += total;

            order.Items.Add(new OrderItem
            {
                MenuItemId = itemDto.MenuItemId,
                RawMaterialId = itemDto.RawMaterialId,
                ItemName = itemDto.ItemName,
                VariantName = itemDto.VariantName,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TotalPrice = total,
                Notes = itemDto.Notes
            });
        }

        order.SubTotal = subtotal;
        var gstRate = outlet.Restaurant?.DefaultGstPercent ?? 5.0m;
        order.TaxAmount = Math.Round(subtotal * (gstRate / 100m), 2);
        order.GrandTotal = Math.Round(order.SubTotal + order.TaxAmount, 2);

        // Update Table status if DineIn
        RestaurantTable? table = null;
        if (req.TableId.HasValue)
        {
            table = await _context.Tables.FindAsync(req.TableId.Value);
            if (table != null)
            {
                table.Status = TableStatus.Occupied;
                table.CurrentOrderId = order.Id;
            }
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        if (table != null)
        {
            await _hubContext.Clients.Group($"outlet-{req.OutletId}")
                .SendAsync("TableStatusChanged", table.Id.ToString(), TableStatus.Occupied.ToString(), order.Id.ToString());
        }

        // Broadcast to POS group
        await _hubContext.Clients.Group($"outlet-{req.OutletId}").SendAsync("NewOrderCreated", order);

        return Ok(order);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Table)
            .Include(o => o.Kots).ThenInclude(k => k.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound(new ProblemDetails { Title = "Order not found", Detail = $"Order with ID {orderId} was not found." });

        return Ok(order);
    }

    [HttpPost("{orderId}/items")]
    public async Task<IActionResult> AppendItems(Guid orderId, [FromBody] List<OrderItemRequest> newItems)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound(new ProblemDetails { Title = "Order not found", Detail = $"Order with ID {orderId} was not found." });

        decimal additionalSubtotal = 0;
        foreach (var itemDto in newItems)
        {
            var total = itemDto.UnitPrice * itemDto.Quantity;
            additionalSubtotal += total;

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                MenuItemId = itemDto.MenuItemId,
                RawMaterialId = itemDto.RawMaterialId,
                ItemName = itemDto.ItemName,
                VariantName = itemDto.VariantName,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                TotalPrice = total,
                Notes = itemDto.Notes,
                IsFired = false
            };
            _context.OrderItems.Add(orderItem);
        }

        order.SubTotal += additionalSubtotal;
        var gstRate = 5.0m;
        order.TaxAmount = Math.Round(order.SubTotal * (gstRate / 100m), 2);
        order.GrandTotal = Math.Round(order.SubTotal + order.TaxAmount, 2);

        await _context.SaveChangesAsync();
        return Ok(order);
    }

    [HttpPost("{orderId}/fire-kot")]
    public async Task<IActionResult> FireKot(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Kots)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound(new ProblemDetails { Title = "Order not found", Detail = $"Order with ID {orderId} was not found." });

        var itemsToFire = order.Items.Where(i => !i.IsVoided && !i.IsFired).ToList();
        if (!itemsToFire.Any())
        {
            return BadRequest(new ProblemDetails { Title = "Bad Request", Detail = "No new items to fire to kitchen." });
        }

        var kotNumber = $"KOT-{(order.Kots.Count + 1):D2}";

        var kot = new KitchenOrderTicket
        {
            OrderId = order.Id,
            KotNumber = kotNumber,
            KitchenStation = "Kitchen",
            Status = KotStatus.Pending,
            FiredAt = DateTime.UtcNow
        };

        foreach (var item in itemsToFire)
        {
            kot.Items.Add(new KotItem
            {
                ItemName = item.ItemName,
                VariantName = item.VariantName,
                Quantity = item.Quantity,
                CookingNotes = item.Notes
            });
            item.IsFired = true;
        }

        _context.KitchenOrderTickets.Add(kot);
        order.Status = OrderStatus.KotPlaced;

        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"outlet-{order.OutletId}").SendAsync("KotReceived", new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            tableNumber = order.Table?.TableNumber ?? "Takeaway",
            kot
        });

        return Ok(kot);
    }

    [HttpPost("{orderId}/settle")]
    public async Task<IActionResult> SettleOrder(Guid orderId, [FromBody] SettleOrderRequest req)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound(new ProblemDetails { Title = "Order not found", Detail = $"Order with ID {orderId} was not found." });

        if (order.Status == OrderStatus.Paid || order.IsPaid)
        {
            return BadRequest(new ProblemDetails { Title = "Order already settled", Detail = "This order has already been paid and settled." });
        }

        order.DiscountAmount = req.DiscountAmount;
        order.DiscountReason = req.DiscountReason;
        order.ServiceChargeAmount = req.ServiceChargeAmount;
        order.PaymentMode = req.PaymentMode;
        order.PaymentTransactionRef = req.PaymentTransactionRef;

        var netBeforeTax = order.SubTotal - req.DiscountAmount;
        order.TaxAmount = Math.Round(netBeforeTax * 0.05m, 2); // 5% GST
        var total = netBeforeTax + order.TaxAmount + req.ServiceChargeAmount;
        order.GrandTotal = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        order.RoundOff = order.GrandTotal - total;

        order.IsPaid = true;
        order.Status = OrderStatus.Paid;
        order.SettledAt = DateTime.UtcNow;

        // Release table if DineIn
        if (order.TableId.HasValue && order.Table != null)
        {
            order.Table.Status = TableStatus.Vacant;
            order.Table.CurrentOrderId = null;
        }

        // Auto Recipe & Direct Inventory Deduction
        var itemIds = order.Items.Where(i => !i.IsVoided && i.MenuItemId.HasValue).Select(i => i.MenuItemId!.Value).ToList();
        var recipes = await _context.RecipeItems
            .Where(r => itemIds.Contains(r.MenuItemId))
            .ToListAsync();

        foreach (var orderItem in order.Items.Where(i => !i.IsVoided))
        {
            if (orderItem.RawMaterialId.HasValue)
            {
                var stock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(s => s.OutletId == order.OutletId && s.RawMaterialId == orderItem.RawMaterialId.Value);

                if (stock != null)
                {
                    stock.CurrentStockQuantity -= orderItem.Quantity;
                    stock.LastUpdatedAt = DateTime.UtcNow;
                }
            }
            else if (orderItem.MenuItemId.HasValue)
            {
                var itemRecipes = recipes.Where(r => r.MenuItemId == orderItem.MenuItemId.Value);
                foreach (var recipe in itemRecipes)
                {
                    var stock = await _context.InventoryStocks
                        .FirstOrDefaultAsync(s => s.OutletId == order.OutletId && s.RawMaterialId == recipe.RawMaterialId);

                    if (stock != null)
                    {
                        stock.CurrentStockQuantity -= (recipe.QuantityRequired * orderItem.Quantity);
                        stock.LastUpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        if (order.TableId.HasValue && order.Table != null)
        {
            await _hubContext.Clients.Group($"outlet-{order.OutletId}")
                .SendAsync("TableStatusChanged", order.Table.Id.ToString(), TableStatus.Vacant.ToString(), (string?)null);
        }

        // Broadcast table and order update
        await _hubContext.Clients.Group($"outlet-{order.OutletId}").SendAsync("OrderSettled", new { orderId = order.Id, grandTotal = order.GrandTotal });

        return Ok(order);
    }

    [HttpGet("outlet/{outletId}/kots")]
    public async Task<IActionResult> GetActiveKots(Guid outletId)
    {
        var kots = await _context.KitchenOrderTickets
            .Include(k => k.Items)
            .Include(k => k.Order)
                .ThenInclude(o => o!.Table)
            .Where(k => k.Order != null && k.Order.OutletId == outletId && k.Status != KotStatus.Served && k.Status != KotStatus.Cancelled)
            .OrderBy(k => k.FiredAt)
            .ToListAsync();

        return Ok(kots.Select(k => new {
            id = k.Id,
            orderId = k.OrderId,
            orderNumber = k.Order != null ? k.Order.OrderNumber : string.Empty,
            tableNumber = k.Order?.Table != null ? k.Order.Table.TableNumber : "Takeaway",
            kotNumber = k.KotNumber,
            kitchenStation = k.KitchenStation,
            status = k.Status,
            firedAt = k.FiredAt,
            items = k.Items.Select(i => new {
                id = i.Id,
                itemName = i.ItemName,
                variantName = i.VariantName,
                quantity = i.Quantity,
                cookingNotes = i.CookingNotes,
                isPrepared = i.IsPrepared
            }).ToList()
        }));
    }

    [HttpPost("kots/{kotId}/status")]
    public async Task<IActionResult> UpdateKotStatus(Guid kotId, [FromBody] UpdateKotStatusRequest req)
    {
        var kot = await _context.KitchenOrderTickets
            .Include(k => k.Order)
            .FirstOrDefaultAsync(k => k.Id == kotId);

        if (kot == null) return NotFound(new ProblemDetails { Title = "KOT not found", Detail = $"KOT with ID {kotId} was not found." });

        kot.Status = req.Status;
        if (req.Status == KotStatus.Ready || req.Status == KotStatus.Served)
        {
            kot.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Broadcast KOT status change
        if (kot.Order != null)
        {
            await _hubContext.Clients.Group($"outlet-{kot.Order.OutletId}").SendAsync("KotStatusChanged", new { kotId = kot.Id, status = (int)kot.Status });
        }

        return Ok(kot);
    }

    [HttpPost("kots/items/{itemId}/toggle")]
    public async Task<IActionResult> ToggleKotItemPrepared(Guid itemId)
    {
        var item = await _context.KotItems
            .Include(i => i.KitchenOrderTicket)
                .ThenInclude(k => k!.Order)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null) return NotFound(new ProblemDetails { Title = "KOT item not found", Detail = $"KOT item with ID {itemId} was not found." });

        item.IsPrepared = !item.IsPrepared;
        await _context.SaveChangesAsync();

        if (item.KitchenOrderTicket?.Order != null)
        {
            await _hubContext.Clients.Group($"outlet-{item.KitchenOrderTicket.Order.OutletId}").SendAsync("KotItemToggled", new { itemId = item.Id, isPrepared = item.IsPrepared });
        }

        return Ok(item);
    }

    [HttpPost("reset-db")]
    public async Task<IActionResult> ResetDatabaseOrders()
    {
        var tables = await _context.Tables.ToListAsync();
        foreach (var t in tables)
        {
            t.Status = TableStatus.Vacant;
            t.CurrentOrderId = null;
        }

        var orders = await _context.Orders.ToListAsync();
        _context.Orders.RemoveRange(orders);

        var kots = await _context.KitchenOrderTickets.ToListAsync();
        _context.KitchenOrderTickets.RemoveRange(kots);

        await _context.SaveChangesAsync();
        return Ok(new { message = "All orders cleared and tables set to vacant successfully." });
    }
}
