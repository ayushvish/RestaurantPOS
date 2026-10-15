using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.Models;

namespace RestaurantPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly RestaurantPosContext _context;

    public ReportsController(RestaurantPosContext context)
    {
        _context = context;
    }

    [HttpGet("outlet/{outletId}/dashboard")]
    public async Task<IActionResult> GetDashboardStats(Guid outletId)
    {
        var today = DateTime.UtcNow.Date;

        var todaysOrders = await _context.Orders
            .Where(o => o.OutletId == outletId && o.CreatedAt >= today && o.Status == OrderStatus.Paid)
            .Include(o => o.Items)
            .ToListAsync();

        var totalSales = todaysOrders.Sum(o => o.GrandTotal);
        var directMaterialSales = todaysOrders
            .SelectMany(o => o.Items)
            .Where(i => i.RawMaterialId != null && !i.IsVoided)
            .Sum(i => i.TotalPrice);

        var totalOrders = todaysOrders.Count;
        var totalGuests = todaysOrders.Sum(o => o.GuestCount);
        var averageTicketSize = totalOrders > 0 ? Math.Round(totalSales / totalOrders, 2) : 0;

        // Payment mode breakdown
        var paymentBreakdown = todaysOrders
            .GroupBy(o => o.PaymentMode?.ToString() ?? "Other")
            .Select(g => new { PaymentMode = g.Key, Total = g.Sum(x => x.GrandTotal), Count = g.Count() })
            .ToList();

        // Top selling items
        var topItems = todaysOrders
            .SelectMany(o => o.Items.Where(i => !i.IsVoided))
            .GroupBy(i => i.ItemName)
            .Select(g => new { ItemName = g.Key, QuantitySold = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToList();

        return Ok(new
        {
            totalSales,
            directMaterialSales,
            totalOrders,
            totalGuests,
            averageTicketSize,
            paymentBreakdown,
            topItems
        });
    }

    [HttpGet("outlet/{outletId}/sales-summary")]
    public async Task<IActionResult> GetSalesSummary(Guid outletId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var start = fromDate ?? DateTime.UtcNow.Date.AddDays(-7);
        var end = toDate ?? DateTime.UtcNow;

        var orders = await _context.Orders
            .Where(o => o.OutletId == outletId && o.CreatedAt >= start && o.CreatedAt <= end && o.Status == OrderStatus.Paid)
            .ToListAsync();

        var summary = orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Sales = g.Sum(x => x.GrandTotal),
                Tax = g.Sum(x => x.TaxAmount),
                Discounts = g.Sum(x => x.DiscountAmount),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(summary);
    }
}
