using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantPOS.API.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OutletId { get; set; }
    public Outlet? Outlet { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty; // e.g. "ORD-20260827-001"

    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public OrderStatus Status { get; set; } = OrderStatus.Active;

    public Guid? TableId { get; set; }
    public RestaurantTable? Table { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    public int GuestCount { get; set; } = 1;
    public string? WaiterName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; } = 0.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0.0m;
    public string? DiscountReason { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ServiceChargeAmount { get; set; } = 0.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundOff { get; set; } = 0.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; } = 0.0m;

    public PaymentMode? PaymentMode { get; set; }
    public string? PaymentTransactionRef { get; set; }
    public bool IsPaid { get; set; } = false;

    public string? SpecialNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<KitchenOrderTicket> Kots { get; set; } = new List<KitchenOrderTicket>();
}

public class OrderItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public Guid? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Required]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public string? Notes { get; set; } // Special cooking instructions
    public bool IsVoided { get; set; } = false;
    public string? VoidReason { get; set; }
    public bool IsFired { get; set; } = false;
}

public class KitchenOrderTicket
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    [Required]
    [MaxLength(50)]
    public string KotNumber { get; set; } = string.Empty; // e.g. "KOT-101"

    public string KitchenStation { get; set; } = "Kitchen";
    public KotStatus Status { get; set; } = KotStatus.Pending;

    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<KotItem> Items { get; set; } = new List<KotItem>();
}

public class KotItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid KitchenOrderTicketId { get; set; }
    public KitchenOrderTicket? KitchenOrderTicket { get; set; }

    [Required]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public string? CookingNotes { get; set; }
    public bool IsPrepared { get; set; } = false;
}
