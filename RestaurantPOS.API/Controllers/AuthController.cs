using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.DTOs;
using RestaurantPOS.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RestaurantPosContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(RestaurantPosContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        EnsureDefaultUsers();
    }

    private void EnsureDefaultUsers()
    {
        bool changed = false;

        if (!_context.Users.Any(u => u.Username == "superadmin"))
        {
            _context.Users.Add(new User
            {
                FullName = "Super Admin User",
                Username = "superadmin",
                PasswordHash = HashPassword("Password123!"),
                Role = UserRole.SuperAdmin,
                Pin = "9999",
                RestaurantId = null,
                OutletId = null
            });
            changed = true;
        }

        if (changed)
        {
            _context.SaveChanges();
        }
    }

    private void SeedMenuItems(Restaurant restaurant)
    {
        if (_context.Categories.Any(c => c.RestaurantId == restaurant.Id))
        {
            return;
        }

        var starters = new Category
        {
            RestaurantId = restaurant.Id,
            Name = "Starters",
            Description = "Delicious Appetizers",
            DisplayOrder = 1
        };
        starters.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Paneer Tikka", ShortCode = "PNT", BasePrice = 280, IsVegetarian = true, KitchenStation = "Tandoor" });
        starters.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Chicken Seekh Kebab", ShortCode = "CSK", BasePrice = 340, IsVegetarian = false, KitchenStation = "Tandoor" });
        starters.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Crispy Corn Salt & Pepper", ShortCode = "CRC", BasePrice = 220, IsVegetarian = true, KitchenStation = "Kitchen" });
        starters.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Chilli Chicken Dry", ShortCode = "CCD", BasePrice = 320, IsVegetarian = false, KitchenStation = "Kitchen" });

        var mainCourse = new Category
        {
            RestaurantId = restaurant.Id,
            Name = "Main Course",
            Description = "Main dishes",
            DisplayOrder = 2
        };
        mainCourse.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Butter Chicken", ShortCode = "BTC", BasePrice = 420, IsVegetarian = false, KitchenStation = "Kitchen" });
        mainCourse.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Dal Makhani", ShortCode = "DLM", BasePrice = 290, IsVegetarian = true, KitchenStation = "Kitchen" });
        mainCourse.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Paneer Butter Masala", ShortCode = "PBM", BasePrice = 360, IsVegetarian = true, KitchenStation = "Kitchen" });
        mainCourse.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Kadai Murgh", ShortCode = "KDM", BasePrice = 390, IsVegetarian = false, KitchenStation = "Kitchen" });

        var breadsRice = new Category
        {
            RestaurantId = restaurant.Id,
            Name = "Breads & Rice",
            Description = "Assorted breads and rice dishes",
            DisplayOrder = 3
        };
        breadsRice.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Butter Naan", ShortCode = "BTN", BasePrice = 60, IsVegetarian = true, KitchenStation = "Tandoor" });
        breadsRice.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Garlic Naan", ShortCode = "GLN", BasePrice = 80, IsVegetarian = true, KitchenStation = "Tandoor" });
        breadsRice.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Jeera Rice", ShortCode = "JRR", BasePrice = 180, IsVegetarian = true, KitchenStation = "Kitchen" });
        breadsRice.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Dum Biryani (Chicken)", ShortCode = "CKB", BasePrice = 380, IsVegetarian = false, KitchenStation = "Kitchen" });

        var beverages = new Category
        {
            RestaurantId = restaurant.Id,
            Name = "Beverages & Mocktails",
            Description = "Refreshing drinks",
            DisplayOrder = 4
        };
        beverages.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Virgin Mojito", ShortCode = "VMJ", BasePrice = 160, IsVegetarian = true, KitchenStation = "Bar" });
        beverages.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Cold Coffee with Ice Cream", ShortCode = "CCI", BasePrice = 190, IsVegetarian = true, KitchenStation = "Bar" });
        beverages.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = "Fresh Lime Soda (Sweet/Salt)", ShortCode = "FLS", BasePrice = 90, IsVegetarian = true, KitchenStation = "Bar" });

        _context.Categories.Add(starters);
        _context.Categories.Add(mainCourse);
        _context.Categories.Add(breadsRice);
        _context.Categories.Add(beverages);
        _context.SaveChanges();
    }

    [HttpGet("restaurants")]
    public async Task<IActionResult> GetRestaurants()
    {
        var roleClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role"))?.Value;
        var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        IQueryable<Restaurant> query = _context.Restaurants.Include(r => r.Outlets);

        if (roleClaim == UserRole.RestaurantUser.ToString() && Guid.TryParse(userIdClaim, out var ownerId))
        {
            query = query.Where(r => r.OwnerId == ownerId);
        }

        var list = await query
            .Select(r => new {
                id = r.Id,
                name = r.Name,
                outlets = r.Outlets.Select(o => new {
                    id = o.Id,
                    name = o.Name
                })
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("restaurants/admin")]
    public async Task<IActionResult> CreateRestaurantSuperAdmin([FromBody] SuperAdminCreateRestaurantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { message = "Restaurant Name is required." });
        }

        var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        Guid? ownerId = null;
        if (Guid.TryParse(userIdClaim, out var parsedId))
        {
            ownerId = parsedId;
        }

        var restaurant = new Restaurant
        {
            Name = req.Name,
            Tagline = req.Tagline ?? "Savor the flavor",
            CurrencySymbol = "₹",
            OwnerId = ownerId
        };
        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        var outletName = string.IsNullOrWhiteSpace(req.OutletName) ? "Outlet 1" : req.OutletName;
        var mainOutlet = new Outlet
        {
            RestaurantId = restaurant.Id,
            Name = outletName,
            Location = "Main Floor"
        };
        _context.Outlets.Add(mainOutlet);
        await _context.SaveChangesAsync();

        var floorName = string.IsNullOrWhiteSpace(req.FloorName) ? "Ground Floor" : req.FloorName;
        var groundFloor = new Floor
        {
            OutletId = mainOutlet.Id,
            Name = floorName,
            DisplayOrder = 1
        };
        _context.Floors.Add(groundFloor);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 4; i++)
        {
            _context.Tables.Add(new RestaurantTable
            {
                FloorId = groundFloor.Id,
                TableNumber = $"T-{i}",
                SeatingCapacity = 4,
                Status = TableStatus.Vacant,
                PosX = ((i - 1) % 4) * 120 + 20,
                PosY = 150
            });
        }

        SeedMenuItems(restaurant);
        await _context.SaveChangesAsync();

        return Ok(new {
            id = restaurant.Id,
            name = restaurant.Name,
            outlets = new[] {
                new { id = mainOutlet.Id, name = mainOutlet.Name }
            }
        });
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        if (await _context.Users.AnyAsync(u => u.Username == req.Username))
        {
            return BadRequest(new { message = "Username already exists." });
        }

        var user = new User
        {
            RestaurantId = req.RestaurantId,
            OutletId = req.OutletId,
            FullName = req.FullName,
            Username = req.Username,
            PasswordHash = HashPassword(req.Password),
            Role = req.Role,
            Pin = req.Pin ?? "1111",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User created successfully." });
    }

    [HttpGet("users/{restaurantId}")]
    public async Task<IActionResult> GetUsers(Guid restaurantId)
    {
        var roleClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role"))?.Value;
        var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        IQueryable<User> query = _context.Users.Include(u => u.Restaurant);
        
        if (roleClaim == UserRole.SuperAdmin.ToString())
        {
            query = query.Where(u => u.Role != UserRole.SuperAdmin);
        }
        else if (roleClaim == UserRole.RestaurantUser.ToString() && Guid.TryParse(userIdClaim, out var ownerId))
        {
            // Restaurant users only see staff under their owned restaurants.
            var ownedRestaurantIds = await _context.Restaurants
                .Where(r => r.OwnerId == ownerId)
                .Select(r => r.Id)
                .ToListAsync();

            query = query.Where(u => u.RestaurantId != null && ownedRestaurantIds.Contains(u.RestaurantId.Value) && u.Role != UserRole.RestaurantUser && u.Role != UserRole.SuperAdmin);
        }
        else
        {
            // Fallback filtering by restaurantId
            query = query.Where(u => u.RestaurantId == restaurantId);
        }

        var users = await query
            .Select(u => new {
                u.Id,
                u.FullName,
                u.Username,
                role = u.Role.ToString(),
                u.Pin,
                u.IsActive,
                restaurantName = u.Restaurant != null ? u.Restaurant.Name : "Platform"
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterRestaurant([FromBody] CreateRestaurantRequest req)
    {
        if (await _context.Users.AnyAsync(u => u.Username == req.AdminUsername))
        {
            return BadRequest(new { message = "Username already exists." });
        }

        var restaurant = new Restaurant
        {
            Name = req.Name,
            Tagline = req.Tagline,
            GstNumber = req.GstNumber,
            FssaiNumber = req.FssaiNumber,
            Address = req.Address,
            Phone = req.Phone,
            Email = req.Email,
            CurrencySymbol = string.IsNullOrWhiteSpace(req.CurrencySymbol) ? "₹" : req.CurrencySymbol,
            DefaultGstPercent = req.DefaultGstPercent,
            ServiceChargePercent = req.ServiceChargePercent
        };

        var mainOutlet = new Outlet
        {
            Restaurant = restaurant,
            Name = "Main Outlet",
            Location = req.Address,
            ContactPhone = req.Phone
        };

        var adminUser = new User
        {
            Restaurant = restaurant,
            Outlet = mainOutlet,
            FullName = req.AdminFullName,
            Username = req.AdminUsername,
            PasswordHash = HashPassword(req.AdminPassword),
            Role = UserRole.RestaurantUser,
            Pin = "1234"
        };

        restaurant.OwnerId = adminUser.Id;

        var groundFloor = new Floor
        {
            Outlet = mainOutlet,
            Name = "Ground Floor",
            DisplayOrder = 1
        };

        for (int i = 1; i <= 8; i++)
        {
            groundFloor.Tables.Add(new RestaurantTable
            {
                TableNumber = $"T-{i}",
                SeatingCapacity = i % 2 == 0 ? 4 : 2,
                Status = TableStatus.Vacant,
                PosX = ((i - 1) % 4) * 120 + 20,
                PosY = ((i - 1) / 4) * 120 + 20
            });
        }

        _context.Restaurants.Add(restaurant);
        _context.Outlets.Add(mainOutlet);
        _context.Users.Add(adminUser);
        _context.Floors.Add(groundFloor);

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(adminUser);
        return Ok(new AuthResponse(token, adminUser.Id, adminUser.FullName, adminUser.Role.ToString(), restaurant.Id, mainOutlet.Id, restaurant.Name, mainOutlet.Name));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _context.Users
            .Include(u => u.Restaurant)
            .Include(u => u.Outlet)
            .FirstOrDefaultAsync(u => u.Username == req.Username);

        if (user == null || user.PasswordHash != HashPassword(req.Password))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponse(token, user.Id, user.FullName, user.Role.ToString(), user.RestaurantId, user.OutletId, user.Restaurant?.Name, user.Outlet?.Name));
    }

    [HttpPost("pin-login")]
    public async Task<IActionResult> PinLogin([FromBody] PinLoginRequest req)
    {
        var user = await _context.Users
            .Include(u => u.Restaurant)
            .Include(u => u.Outlet)
            .FirstOrDefaultAsync(u => u.OutletId == req.OutletId && u.Pin == req.Pin);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid PIN for this outlet." });
        }

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponse(token, user.Id, user.FullName, user.Role.ToString(), user.RestaurantId, user.OutletId, user.Restaurant?.Name, user.Outlet?.Name));
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "RestaurantPOS_Salt_2026"));
        return Convert.ToBase64String(bytes);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "super_secret_restaurant_pos_jwt_key_2026_production_safe!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("RestaurantId", user.RestaurantId.ToString()),
            new Claim("OutletId", user.OutletId?.ToString() ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "RestaurantPOS",
            audience: _configuration["Jwt:Audience"] ?? "RestaurantPOS_Client",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
