using System;
using System.Security.Cryptography;
using System.Text;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.Models;
using RestaurantPOS.API.Tests.Helpers;

namespace RestaurantPOS.API.Tests;

public static class TestDatabaseSeeder
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "RestaurantPOS_Salt_2026"));
        return Convert.ToBase64String(bytes);
    }

    public static void SeedData(RestaurantPosContext context)
    {
        // 1. Restaurant
        var restaurant = new Restaurant
        {
            Id = TestDataConstants.RestaurantId,
            Name = TestDataConstants.RestaurantName,
            Tagline = "Finest Dining Experience",
            GstNumber = "27AAAAA0000A1Z5",
            Address = "123 Culinary Boulevard",
            Phone = "+919876543210",
            Email = "contact@gourmetpalace.com",
            CurrencySymbol = "₹",
            DefaultGstPercent = 5.0m,
            ServiceChargePercent = 0.0m,
            OwnerId = TestDataConstants.AdminUserId,
            CreatedAt = DateTime.UtcNow
        };
        context.Restaurants.Add(restaurant);

        // 2. Outlet
        var outlet = new Outlet
        {
            Id = TestDataConstants.OutletId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = TestDataConstants.OutletName,
            Location = "Downtown Plaza",
            ContactPhone = "+919876543211",
            IsActive = true
        };
        context.Outlets.Add(outlet);

        // 3. Floor
        var floor = new Floor
        {
            Id = TestDataConstants.FloorId,
            OutletId = TestDataConstants.OutletId,
            Name = TestDataConstants.FloorName,
            DisplayOrder = 1,
            IsActive = true
        };
        context.Floors.Add(floor);

        // 4. Tables
        var table1 = new RestaurantTable
        {
            Id = TestDataConstants.Table1Id,
            FloorId = TestDataConstants.FloorId,
            TableNumber = TestDataConstants.Table1Number,
            SeatingCapacity = 4,
            Status = TableStatus.Vacant,
            PosX = 100,
            PosY = 100,
            IsActive = true
        };
        var table2 = new RestaurantTable
        {
            Id = TestDataConstants.Table2Id,
            FloorId = TestDataConstants.FloorId,
            TableNumber = TestDataConstants.Table2Number,
            SeatingCapacity = 4,
            Status = TableStatus.Vacant,
            PosX = 200,
            PosY = 100,
            IsActive = true
        };
        var table3 = new RestaurantTable
        {
            Id = TestDataConstants.Table3Id,
            FloorId = TestDataConstants.FloorId,
            TableNumber = TestDataConstants.Table3Number,
            SeatingCapacity = 6,
            Status = TableStatus.Vacant,
            PosX = 300,
            PosY = 100,
            IsActive = true
        };
        context.Tables.AddRange(table1, table2, table3);

        // 5. Menu Categories
        var catStarters = new Category
        {
            Id = TestDataConstants.CategoryStartersId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = "Starters",
            Description = "Appetizers and quick bites",
            DisplayOrder = 1,
            IsActive = true
        };
        var catMains = new Category
        {
            Id = TestDataConstants.CategoryMainsId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = "Main Course",
            Description = "Hearty entrees",
            DisplayOrder = 2,
            IsActive = true
        };
        var catDesserts = new Category
        {
            Id = TestDataConstants.CategoryDessertsId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = "Desserts",
            Description = "Sweet delicacies",
            DisplayOrder = 3,
            IsActive = true
        };
        context.Categories.AddRange(catStarters, catMains, catDesserts);

        // 6. Menu Items
        var paneerTikka = new MenuItem
        {
            Id = TestDataConstants.PaneerTikkaId,
            RestaurantId = TestDataConstants.RestaurantId,
            CategoryId = TestDataConstants.CategoryStartersId,
            Name = "Paneer Tikka",
            ShortCode = "PTIK",
            Description = "Marinated grilled cottage cheese cubes",
            BasePrice = 250.0m,
            DeliveryPrice = 270.0m,
            IsVegetarian = true,
            IsAvailable = true,
            GstPercent = 5.0m,
            KitchenStation = "Tandoor"
        };
        var butterChicken = new MenuItem
        {
            Id = TestDataConstants.ButterChickenId,
            RestaurantId = TestDataConstants.RestaurantId,
            CategoryId = TestDataConstants.CategoryMainsId,
            Name = "Butter Chicken",
            ShortCode = "BCHK",
            Description = "Tender chicken cooked in rich tomato butter gravy",
            BasePrice = 350.0m,
            DeliveryPrice = 380.0m,
            IsVegetarian = false,
            IsAvailable = true,
            GstPercent = 5.0m,
            KitchenStation = "Kitchen"
        };
        var gulabJamun = new MenuItem
        {
            Id = TestDataConstants.GulabJamunId,
            RestaurantId = TestDataConstants.RestaurantId,
            CategoryId = TestDataConstants.CategoryDessertsId,
            Name = "Gulab Jamun",
            ShortCode = "GJAM",
            Description = "Warm milk-solid dumplings soaked in rose syrup",
            BasePrice = 120.0m,
            DeliveryPrice = 130.0m,
            IsVegetarian = true,
            IsAvailable = true,
            GstPercent = 5.0m,
            KitchenStation = "Kitchen"
        };
        context.MenuItems.AddRange(paneerTikka, butterChicken, gulabJamun);

        // 7. Raw Materials
        var rawPaneer = new RawMaterial
        {
            Id = TestDataConstants.RawMaterialPaneerId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = "Paneer Block",
            UnitOfMeasure = "kg",
            CostPerUnit = 250.0m,
            MinThresholdAlert = 2.0m,
            IsActive = true
        };
        var rawChicken = new RawMaterial
        {
            Id = TestDataConstants.RawMaterialChickenId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = "Raw Chicken",
            UnitOfMeasure = "kg",
            CostPerUnit = 200.0m,
            MinThresholdAlert = 5.0m,
            IsActive = true
        };
        var rawMawa = new RawMaterial
        {
            Id = TestDataConstants.RawMaterialMawaId,
            RestaurantId = TestDataConstants.RestaurantId,
            Name = "Mawa Khoya",
            UnitOfMeasure = "kg",
            CostPerUnit = 300.0m,
            MinThresholdAlert = 2.0m,
            IsActive = true
        };
        context.RawMaterials.AddRange(rawPaneer, rawChicken, rawMawa);

        // 8. Inventory Stock
        var stockPaneer = new InventoryStock
        {
            Id = Guid.NewGuid(),
            OutletId = TestDataConstants.OutletId,
            RawMaterialId = TestDataConstants.RawMaterialPaneerId,
            CurrentStockQuantity = 20.0m,
            LastUpdatedAt = DateTime.UtcNow
        };
        var stockChicken = new InventoryStock
        {
            Id = Guid.NewGuid(),
            OutletId = TestDataConstants.OutletId,
            RawMaterialId = TestDataConstants.RawMaterialChickenId,
            CurrentStockQuantity = 30.0m,
            LastUpdatedAt = DateTime.UtcNow
        };
        var stockMawa = new InventoryStock
        {
            Id = Guid.NewGuid(),
            OutletId = TestDataConstants.OutletId,
            RawMaterialId = TestDataConstants.RawMaterialMawaId,
            CurrentStockQuantity = 15.0m,
            LastUpdatedAt = DateTime.UtcNow
        };
        context.InventoryStocks.AddRange(stockPaneer, stockChicken, stockMawa);

        // 9. Recipes
        var recipePaneer = new RecipeItem
        {
            Id = Guid.NewGuid(),
            MenuItemId = TestDataConstants.PaneerTikkaId,
            RawMaterialId = TestDataConstants.RawMaterialPaneerId,
            QuantityRequired = 0.250m
        };
        var recipeChicken = new RecipeItem
        {
            Id = Guid.NewGuid(),
            MenuItemId = TestDataConstants.ButterChickenId,
            RawMaterialId = TestDataConstants.RawMaterialChickenId,
            QuantityRequired = 0.350m
        };
        var recipeMawa = new RecipeItem
        {
            Id = Guid.NewGuid(),
            MenuItemId = TestDataConstants.GulabJamunId,
            RawMaterialId = TestDataConstants.RawMaterialMawaId,
            QuantityRequired = 0.150m
        };
        context.RecipeItems.AddRange(recipePaneer, recipeChicken, recipeMawa);

        // 10. Users
        var hash = HashPassword(TestDataConstants.DefaultPassword);
        var superAdmin = new User
        {
            Id = TestDataConstants.SuperAdminUserId,
            RestaurantId = null,
            OutletId = null,
            FullName = "System SuperAdmin",
            Username = "superadmin",
            PasswordHash = hash,
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var adminUser = new User
        {
            Id = TestDataConstants.AdminUserId,
            RestaurantId = TestDataConstants.RestaurantId,
            OutletId = TestDataConstants.OutletId,
            FullName = "Restaurant Admin",
            Username = "admin",
            PasswordHash = hash,
            Role = UserRole.RestaurantUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var cashierUser = new User
        {
            Id = TestDataConstants.CashierUserId,
            RestaurantId = TestDataConstants.RestaurantId,
            OutletId = TestDataConstants.OutletId,
            FullName = "Main Cashier",
            Username = "cashier1",
            PasswordHash = hash,
            Pin = TestDataConstants.CashierPin,
            Role = UserRole.Cashier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var waiterUser = new User
        {
            Id = TestDataConstants.WaiterUserId,
            RestaurantId = TestDataConstants.RestaurantId,
            OutletId = TestDataConstants.OutletId,
            FullName = "Floor Waiter",
            Username = "waiter1",
            PasswordHash = hash,
            Pin = TestDataConstants.WaiterPin,
            Role = UserRole.Waiter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(superAdmin, adminUser, cashierUser, waiterUser);

        context.SaveChanges();
    }
}
