using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestaurantPOS.API.Controllers;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Problem Details
builder.Services.AddProblemDetails();

// Add Controllers & JSON settings
builder.Services.AddControllers(options =>
{
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add EF Core PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=RestaurantPosDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<RestaurantPosContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Add Real-Time SignalR
builder.Services.AddSignalR();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_restaurant_pos_jwt_key_2026_production_safe!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RestaurantPOS",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RestaurantPOS_Client",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Add CORS for Frontend POS UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Seed default users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RestaurantPosContext>();
        AuthController.EnsureDefaultUsers(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding default users.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PosHub>("/hubs/pos");

app.Run();

public partial class Program { }
