using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.API.Models;

public class Floor
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OutletId { get; set; }
    public Outlet? Outlet { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g. "Ground Floor", "AC Hall", "Rooftop"

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
}

public class RestaurantTable
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FloorId { get; set; }
    public Floor? Floor { get; set; }

    [Required]
    [MaxLength(50)]
    public string TableNumber { get; set; } = string.Empty;

    public int SeatingCapacity { get; set; } = 4;
    public TableStatus Status { get; set; } = TableStatus.Vacant;

    public Guid? CurrentOrderId { get; set; }
    public decimal PosX { get; set; } = 0; // Visual floor designer coordinates
    public decimal PosY { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
