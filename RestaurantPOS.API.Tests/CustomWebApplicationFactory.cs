using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantPOS.API.Data;
using RestaurantPOS.API.Tests.Helpers;

namespace RestaurantPOS.API.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<RestaurantPosContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(RestaurantPosContext)).ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // Create dedicated isolated service provider for InMemory database
            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Register InMemory database using isolated internal service provider
            services.AddDbContext<RestaurantPosContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName)
                       .UseInternalServiceProvider(inMemoryProvider);
            });

            // Build service provider and seed initial test dataset
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RestaurantPosContext>();
            db.Database.EnsureCreated();
            TestDatabaseSeeder.SeedData(db);
        });
    }

    public RestaurantPosContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<RestaurantPosContext>();
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username = "admin", string password = TestDataConstants.DefaultPassword)
    {
        var client = CreateClient();
        var auth = await AuthHelper.LoginAsync(client, username, password);
        AuthHelper.SetBearerToken(client, auth.Token);
        return client;
    }

    public async Task<HttpClient> CreateSuperAdminClientAsync()
    {
        return await CreateAuthenticatedClientAsync("superadmin", TestDataConstants.DefaultPassword);
    }

    public HubConnection CreateSignalRConnection(string? token = null)
    {
        var client = CreateClient();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(client.BaseAddress!, "hubs/pos"), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    var wsClient = Server.CreateWebSocketClient();
                    return await wsClient.ConnectAsync(context.Uri, cancellationToken);
                };
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                if (!string.IsNullOrEmpty(token))
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();

        return hubConnection;
    }
}

public class CustomWebApplicationFactory : CustomWebApplicationFactory<Program>
{
}
