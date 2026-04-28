using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;
// PERSONA 3- Parts Enpoint
public class PartsApiControllerTests
{
    private const string Route = "/api/PartsApi";

    // Test: GET /api/PartsApi aksessohet pa token
    [Fact]
    public async Task GetParts_WithoutToken_ReturnsOk()
    {
        await using var factory = new TestWebApplicationFactory();

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Parts.Add(new Part
            {
                Name = "Oil Filter",
                Description = "Test part",
                UnitPrice = 10m,
                StockQuantity = 20,
                MinStockLevel = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Test: POST pa rol Manager kthen Forbidden
    [Fact]
    public async Task CreatePart_AsClient_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync(Route, new Part
        {
            Name = "Brake Pad",
            Description = "Test part",
            UnitPrice = 30m,
            StockQuantity = 10,
            MinStockLevel = 3,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Manager krijon Part me StockQuantity më të vogël se MinStockLevel
    [Fact]
    public async Task CreatePart_WithStockQuantityLessThanMinStockLevel_CreatesPart()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateAuthenticatedClient("Manager");

        var response = await client.PostAsJsonAsync(Route, new Part
        {
            Name = "Low Stock Part",
            Description = "Part with low stock",
            UnitPrice = 15m,
            StockQuantity = 2,
            MinStockLevel = 5,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var part = await db.Parts
            .FirstOrDefaultAsync(p => p.Name == "Low Stock Part");

        Assert.NotNull(part);
        Assert.True(part!.StockQuantity < part.MinStockLevel);
    }
}