using Microsoft.EntityFrameworkCore;
using RestaurantPOS.API.Models;

namespace RestaurantPOS.API.Data;

public class RestaurantPosContext : DbContext
{
    public RestaurantPosContext(DbContextOptions<RestaurantPosContext> options) : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemVariant> MenuItemVariants => Set<MenuItemVariant>();
    public DbSet<MenuItemModifierGroup> ModifierGroups => Set<MenuItemModifierGroup>();
    public DbSet<MenuItemModifierOption> ModifierOptions => Set<MenuItemModifierOption>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<KitchenOrderTicket> KitchenOrderTickets => Set<KitchenOrderTicket>();
    public DbSet<KotItem> KotItems => Set<KotItem>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<InventoryStock> InventoryStocks => Set<InventoryStock>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure cascade deletes or relationships
        modelBuilder.Entity<Restaurant>()
            .HasMany(r => r.Outlets)
            .WithOne(o => o.Restaurant)
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Floor>()
            .HasMany(f => f.Tables)
            .WithOne(t => t.Floor)
            .HasForeignKey(t => t.FloorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.MenuItems)
            .WithOne(m => m.Category)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MenuItem>()
            .HasMany(m => m.Variants)
            .WithOne(v => v.MenuItem)
            .HasForeignKey(v => v.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MenuItem>()
            .HasMany(m => m.ModifierGroups)
            .WithOne(g => g.MenuItem)
            .HasForeignKey(g => g.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MenuItemModifierGroup>()
            .HasMany(g => g.Options)
            .WithOne(o => o.ModifierGroup)
            .HasForeignKey(o => o.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitchenOrderTicket>()
            .HasMany(k => k.Items)
            .WithOne(i => i.KitchenOrderTicket)
            .HasForeignKey(i => i.KitchenOrderTicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
