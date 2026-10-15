namespace RestaurantPOS.API.Models;

public enum OrderType
{
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3,
    OnlineAggregator = 4
}

public enum OrderStatus
{
    Active = 1,
    KotPlaced = 2,
    Preparing = 3,
    Ready = 4,
    Billed = 5,
    Paid = 6,
    Cancelled = 7
}

public enum KotStatus
{
    Pending = 1,
    Preparing = 2,
    Ready = 3,
    Served = 4,
    Cancelled = 5
}

public enum TableStatus
{
    Vacant = 1,
    Occupied = 2,
    Billed = 3,
    Reserved = 4
}

public enum PaymentMode
{
    Cash = 1,
    Card = 2,
    UPI = 3,
    Split = 4,
    DueCustomerLedger = 5
}

public enum UserRole
{
    SuperAdmin = 1,
    RestaurantUser = 2,
    Manager = 3,
    Cashier = 4,
    Waiter = 5,
    Chef = 6,
    InventoryUser = 7
}
