using RestaurantPOS.API.Models;

namespace RestaurantPOS.API.DTOs;

public record LoginRequest(string Username, string Password);
public record PinLoginRequest(string Pin, Guid OutletId);
public record AuthResponse(string Token, Guid UserId, string FullName, string Role, Guid? RestaurantId, Guid? OutletId, string? RestaurantName, string? OutletName);

public record CreateRestaurantRequest(
    string Name,
    string? Tagline,
    string? GstNumber,
    string? FssaiNumber,
    string? Address,
    string? Phone,
    string? Email,
    string CurrencySymbol,
    decimal DefaultGstPercent,
    decimal ServiceChargePercent,
    string AdminUsername,
    string AdminPassword,
    string AdminFullName
);

public record CreateCategoryRequest(string Name, string? Description, string? ImageUrl, int DisplayOrder);

public record CreateMenuItemVariantDto(string Name, decimal Price, bool IsDefault);
public record CreateModifierOptionDto(string Name, decimal Price);
public record CreateModifierGroupDto(string Title, int MinSelection, int MaxSelection, List<CreateModifierOptionDto> Options);

public record CreateMenuItemRequest(
    Guid CategoryId,
    string Name,
    string? ShortCode,
    string? Description,
    string? ImageUrl,
    decimal BasePrice,
    decimal DeliveryPrice,
    bool IsVegetarian,
    decimal GstPercent,
    string KitchenStation,
    List<CreateMenuItemVariantDto>? Variants,
    List<CreateModifierGroupDto>? ModifierGroups
);

public record CreateFloorRequest(Guid OutletId, string Name, int DisplayOrder);
public record CreateTableRequest(Guid FloorId, string TableNumber, int SeatingCapacity, decimal PosX, decimal PosY);

public record OrderItemRequest(
    Guid? MenuItemId,
    Guid? RawMaterialId,
    string ItemName,
    string? VariantName,
    int Quantity,
    decimal UnitPrice,
    string? Notes
);

public record CreateOrderRequest(
    Guid OutletId,
    OrderType OrderType,
    Guid? TableId,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    int GuestCount,
    string? WaiterName,
    string? SpecialNotes,
    List<OrderItemRequest> Items
);

public record SettleOrderRequest(
    PaymentMode PaymentMode,
    decimal DiscountAmount,
    string? DiscountReason,
    decimal ServiceChargeAmount,
    string? PaymentTransactionRef
);

public record UpdateKotStatusRequest(KotStatus Status);
public record CreateRawMaterialRequest(string Name, string UnitOfMeasure, decimal CostPerUnit, decimal MinThresholdAlert);
public record UpdateStockRequest(Guid RawMaterialId, decimal QuantityChange, string Reason);
public record SuperAdminCreateRestaurantRequest(string Name, string? Tagline, string? OutletName, string? FloorName);
public record CreateUserRequest(Guid RestaurantId, Guid? OutletId, string FullName, string Username, string Password, UserRole Role, string? Pin);
