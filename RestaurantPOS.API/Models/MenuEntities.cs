using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.API.Models;

public class Category
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Starters", "Main Course", "Beverages"

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

public class MenuItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? ShortCode { get; set; } // e.g. "BTR-PNNR"
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeliveryPrice { get; set; } // Aggregator or delivery price

    public bool IsVegetarian { get; set; } = true;
    public bool IsAvailable { get; set; } = true; // 86ing / quick stock out toggle
    public decimal GstPercent { get; set; } = 5.0m;

    public string? KitchenStation { get; set; } = "Kitchen"; // Kitchen, Bar, Tandoor, Bakery

    public ICollection<MenuItemVariant> Variants { get; set; } = new List<MenuItemVariant>();
    public ICollection<MenuItemModifierGroup> ModifierGroups { get; set; } = new List<MenuItemModifierGroup>();
    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}

public class MenuItemVariant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g. "Half", "Full", "Small", "Large"

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public bool IsDefault { get; set; } = false;
}

public class MenuItemModifierGroup
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty; // e.g. "Extra Toppings", "Spiciness Level"

    public int MinSelection { get; set; } = 0;
    public int MaxSelection { get; set; } = 5;

    public ICollection<MenuItemModifierOption> Options { get; set; } = new List<MenuItemModifierOption>();
}

public class MenuItemModifierOption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ModifierGroupId { get; set; }
    public MenuItemModifierGroup? ModifierGroup { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g. "Extra Cheese", "Mild Spicy"

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; } = 0.0m;
}
