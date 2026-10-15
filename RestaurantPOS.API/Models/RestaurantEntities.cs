using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.API.Models;

public class Restaurant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Tagline { get; set; }
    public string? GstNumber { get; set; }
    public string? FssaiNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string CurrencySymbol { get; set; } = "₹";
    public decimal DefaultGstPercent { get; set; } = 5.0m;
    public decimal ServiceChargePercent { get; set; } = 0.0m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? OwnerId { get; set; }

    public ICollection<Outlet> Outlets { get; set; } = new List<Outlet>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

public class Outlet
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<InventoryStock> InventoryStocks { get; set; } = new List<InventoryStock>();
}

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public Guid? OutletId { get; set; }
    public Outlet? Outlet { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? Pin { get; set; } // Quick 4-digit PIN for quick cashier/waiter punch

    public UserRole Role { get; set; } = UserRole.Cashier;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
