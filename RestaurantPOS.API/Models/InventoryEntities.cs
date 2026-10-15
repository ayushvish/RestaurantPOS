using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.API.Models;

public class RawMaterial
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty; // e.g., "Paneer", "Cooking Oil", "Basmati Rice"

    [Required]
    [MaxLength(50)]
    public string UnitOfMeasure { get; set; } = "kg"; // kg, gm, ltr, ml, pcs

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPerUnit { get; set; } = 0.0m;

    public decimal MinThresholdAlert { get; set; } = 5.0m;
    public bool IsActive { get; set; } = true;

    public ICollection<InventoryStock> Stocks { get; set; } = new List<InventoryStock>();
    public ICollection<RecipeItem> RecipeUsages { get; set; } = new List<RecipeItem>();
}

public class InventoryStock
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OutletId { get; set; }
    public Outlet? Outlet { get; set; }

    public Guid RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal CurrentStockQuantity { get; set; } = 0.0m;

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RecipeItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public Guid RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityRequired { get; set; } // e.g. 0.200 kg for 1 Paneer dish
}

public class Supplier
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    [Required]
    [MaxLength(150)]
    public string SupplierName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GstNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
