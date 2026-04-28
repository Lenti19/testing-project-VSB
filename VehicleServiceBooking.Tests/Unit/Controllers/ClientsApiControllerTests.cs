using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;
// PERSONA 3 - Clients endpoint
public class ClientsApiControllerTests
{
    private const string Route = "/api/ClientsApi";

    // Test: Client nuk mund të shohë listën e të gjithë klientëve
    [Fact]
    public async Task GetClients_AsClient_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Manager mund të shohë listën e klientëve
    [Fact]
    public async Task GetClients_AsManager_ReturnsOk()
    {
        await using var factory = new TestWebApplicationFactory();
        var manager = factory.CreateAuthenticatedClient("Manager");

        var response = await manager.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Test: Client nuk mund të shohë profilin e një klienti tjetër
    [Fact]
    public async Task GetClient_AsClient_ForOtherUser_ReturnsForbidden()
    {
        await using var factory = new TestWebApplicationFactory();

        // Krijo një klient tjetër
        var anon = factory.CreateClient();

        var regResp = await anon.PostAsJsonAsync("/api/auth/register-client", new
        {
            Email = "other.client@test.com",
            Password = "Test123!",
            FirstName = "Other",
            LastName = "Client"
        });

        Assert.True(
            regResp.StatusCode == HttpStatusCode.OK ||
            regResp.StatusCode == HttpStatusCode.Created
        );

        var body = await regResp.Content.ReadFromJsonAsync<JsonElement>();
        var otherId = body.GetProperty("user").GetProperty("id").GetString();

        // Client aktual tenton ta shohë tjetrin
        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.GetAsync($"{Route}/{otherId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

  
    // Test: Pa token → Unauthorized
    [Fact]
    public async Task GetClients_NoToken_ReturnsUnauthorized()
    {
        await using var factory = new TestWebApplicationFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}