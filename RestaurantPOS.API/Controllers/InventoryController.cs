using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;

namespace RestaurantPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly RestaurantPosContext _context;

    public InventoryController(RestaurantPosContext context)
    {
        _context = context;
    }

    [HttpGet("materials/{restaurantId}")]
    public async Task<IActionResult> GetRawMaterials(Guid restaurantId)
    {
        var materials = await _context.RawMaterials
            .Where(r => r.RestaurantId == restaurantId && r.IsActive)
            .Include(r => r.Stocks)
            .ToListAsync();

        return Ok(materials);
    }

    [HttpPost("materials/{restaurantId}")]
    public async Task<IActionResult> CreateRawMaterial(Guid restaurantId, [FromBody] CreateRawMaterialRequest req)
    {
        var material = new RawMaterial
        {
            RestaurantId = restaurantId,
            Name = req.Name,
            UnitOfMeasure = req.UnitOfMeasure,
            CostPerUnit = req.CostPerUnit,
            MinThresholdAlert = req.MinThresholdAlert
        };

        _context.RawMaterials.Add(material);
        await _context.SaveChangesAsync();
        return Ok(material);
    }

    [HttpPost("stock/{outletId}/adjust")]
    public async Task<IActionResult> AdjustStock(Guid outletId, [FromBody] UpdateStockRequest req)
    {
        var stock = await _context.InventoryStocks
            .FirstOrDefaultAsync(s => s.OutletId == outletId && s.RawMaterialId == req.RawMaterialId);

        if (stock == null)
        {
            stock = new InventoryStock
            {
                OutletId = outletId,
                RawMaterialId = req.RawMaterialId,
                CurrentStockQuantity = req.QuantityChange,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.InventoryStocks.Add(stock);
        }
        else
        {
            stock.CurrentStockQuantity += req.QuantityChange;
            stock.LastUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(stock);
    }

    [HttpPost("recipes/{menuItemId}")]
    public async Task<IActionResult> SetRecipe(Guid menuItemId, [FromBody] List<RecipeItem> recipeItems)
    {
        var existing = await _context.RecipeItems.Where(r => r.MenuItemId == menuItemId).ToListAsync();
        _context.RecipeItems.RemoveRange(existing);

        foreach (var item in recipeItems)
        {
            item.Id = Guid.NewGuid();
            item.MenuItemId = menuItemId;
            _context.RecipeItems.Add(item);
        }

        await _context.SaveChangesAsync();
        return Ok(recipeItems);
    }
}
