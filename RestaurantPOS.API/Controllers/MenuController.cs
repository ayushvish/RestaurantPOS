using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;

namespace RestaurantPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly RestaurantPosContext _context;

    public MenuController(RestaurantPosContext context)
    {
        _context = context;
    }

    [HttpGet("categories/{restaurantId}")]
    public async Task<IActionResult> GetCategories(Guid restaurantId)
    {
        var categories = await _context.Categories
            .Where(c => c.RestaurantId == restaurantId && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.MenuItems.Where(m => m.IsAvailable))
                .ThenInclude(m => m.Variants)
            .Include(c => c.MenuItems)
                .ThenInclude(m => m.ModifierGroups)
                .ThenInclude(g => g.Options)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("categories/{restaurantId}")]
    public async Task<IActionResult> CreateCategory(Guid restaurantId, [FromBody] CreateCategoryRequest req)
    {
        var category = new Category
        {
            RestaurantId = restaurantId,
            Name = req.Name,
            Description = req.Description,
            ImageUrl = req.ImageUrl,
            DisplayOrder = req.DisplayOrder
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpGet("items/{restaurantId}")]
    public async Task<IActionResult> GetMenuItems(Guid restaurantId)
    {
        var items = await _context.MenuItems
            .Where(m => m.RestaurantId == restaurantId)
            .Include(m => m.Category)
            .Include(m => m.Variants)
            .Include(m => m.ModifierGroups)
                .ThenInclude(g => g.Options)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("items/{restaurantId}")]
    public async Task<IActionResult> CreateMenuItem(Guid restaurantId, [FromBody] CreateMenuItemRequest req)
    {
        var item = new MenuItem
        {
            RestaurantId = restaurantId,
            CategoryId = req.CategoryId,
            Name = req.Name,
            ShortCode = req.ShortCode,
            Description = req.Description,
            ImageUrl = req.ImageUrl,
            BasePrice = req.BasePrice,
            DeliveryPrice = req.DeliveryPrice > 0 ? req.DeliveryPrice : req.BasePrice,
            IsVegetarian = req.IsVegetarian,
            GstPercent = req.GstPercent,
            KitchenStation = req.KitchenStation ?? "Kitchen"
        };

        if (req.Variants != null && req.Variants.Any())
        {
            foreach (var v in req.Variants)
            {
                item.Variants.Add(new MenuItemVariant
                {
                    Name = v.Name,
                    Price = v.Price,
                    IsDefault = v.IsDefault
                });
            }
        }

        if (req.ModifierGroups != null && req.ModifierGroups.Any())
        {
            foreach (var g in req.ModifierGroups)
            {
                var group = new MenuItemModifierGroup
                {
                    Title = g.Title,
                    MinSelection = g.MinSelection,
                    MaxSelection = g.MaxSelection
                };

                foreach (var opt in g.Options)
                {
                    group.Options.Add(new MenuItemModifierOption
                    {
                        Name = opt.Name,
                        Price = opt.Price
                    });
                }

                item.ModifierGroups.Add(group);
            }
        }

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPatch("items/{itemId}/toggle-availability")]
    public async Task<IActionResult> ToggleItemAvailability(Guid itemId)
    {
        var item = await _context.MenuItems.FindAsync(itemId);
        if (item == null) return NotFound();

        item.IsAvailable = !item.IsAvailable;
        await _context.SaveChangesAsync();
        return Ok(new { itemId = item.Id, isAvailable = item.IsAvailable });
    }
}
