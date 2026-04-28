using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;
// PERSONA 3- Mechanic endpoint
public class MechanicsApiControllerTests
{
    private const string Route = "/api/MechanicsApi";

    // Test: Manager krijon mechanic + ApplicationUser + rol Mechanic
    [Fact]
    public async Task CreateMechanic_AsManager_CreatesMechanicAndUser()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateAuthenticatedClient("Manager");
        var email = "mechanic.test9001@test.com";

        var response = await client.PostAsJsonAsync(Route, new
        {
            FirstName = "Test",
            LastName = "Mechanic",
            Email = email,
            Password = "Test123!",
            Specialization = "Engine",
            ServiceCenterId = 1,
            HourlyRate = 25m,
            IsAvailable = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.CreateTestScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        var isMechanic = await userManager.IsInRoleAsync(user!, "Mechanic");
        Assert.True(isMechanic);

        var mechanic = await db.Mechanics
            .FirstOrDefaultAsync(m => m.UserId == user!.Id);

        Assert.NotNull(mechanic);
        Assert.Equal(1, mechanic!.ServiceCenterId);
        Assert.Equal("Engine", mechanic.Specialization);
        Assert.Equal(25m, mechanic.HourlyRate);
        Assert.True(mechanic.IsAvailable);
    }

    // Test: Client nuk ka të drejtë të krijojë mechanic
    [Fact]
    public async Task CreateMechanic_AsClient_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync(Route, new
        {
            FirstName = "No",
            LastName = "Access",
            Email = "no.access.mechanic@test.com",
            Password = "Test123!",
            Specialization = "Engine",
            ServiceCenterId = 1,
            HourlyRate = 20m,
            IsAvailable = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Manager fshin mechanic dhe user-in e lidhur
    [Fact]
    public async Task DeleteMechanic_AsManager_DeletesMechanicAndUser()
    {
        await using var factory = new TestWebApplicationFactory();

        int mechanicId;
        string email = "delete.mechanic9002@test.com";

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Delete",
                LastName = "Mechanic",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, "Test123!");
            Assert.True(createResult.Succeeded);

            await userManager.AddToRoleAsync(user, "Mechanic");

            var mechanic = new Mechanic
            {
                UserId = user.Id,
                ServiceCenterId = 1,
                Specialization = "Brakes",
                HourlyRate = 30m,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Mechanics.Add(mechanic);
            await db.SaveChangesAsync();

            mechanicId = mechanic.Id;
        }

        var client = factory.CreateAuthenticatedClient("Manager");

        var response = await client.DeleteAsync($"{Route}/{mechanicId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var mechanicExists = await db.Mechanics.AnyAsync(m => m.Id == mechanicId);
            var user = await userManager.FindByEmailAsync(email);

            Assert.False(mechanicExists);
            Assert.Null(user);
        }
    }

    // Test: Client nuk ka të drejtë të fshijë mechanic
    [Fact]
    public async Task DeleteMechanic_AsClient_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();

        int mechanicId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var mechanic = new Mechanic
            {
                UserId = "test-user",
                ServiceCenterId = 1,
                Specialization = "Oil",
                HourlyRate = 10m,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Mechanics.Add(mechanic);
            await db.SaveChangesAsync();

            mechanicId = mechanic.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.DeleteAsync($"{Route}/{mechanicId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}