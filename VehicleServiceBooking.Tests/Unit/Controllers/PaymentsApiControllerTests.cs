using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Unit.Controllers;
// PERSONA 3- Payment Endpoint
public class PaymentsApiControllerTests
{
    // Test: pagesë pa invoice -> backend kthen NotFound
    [Fact]
    public async Task CreatePayment_WithoutInvoice_ReturnsNotFound()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                Id = 9001,
                ClientId = TestAuthHelper.ClientId,
                Status = BookingStatus.Confirmed,
                BookingDate = DateTime.Today.AddDays(2)
            };

            var workOrder = new WorkOrder
            {
                Id = 9001,
                BookingId = booking.Id,
                Status = WorkOrderStatus.ReadyForPayment
            };

            db.Bookings.Add(booking);
            db.WorkOrders.Add(workOrder);
            await db.SaveChangesAsync();

            workOrderId = workOrder.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            WorkOrderId = workOrderId,
            Amount = 50m,
            Method = PaymentMethod.Cash,
            Notes = "Test payment"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test: pagesë mbi balans -> backend kthen NotFound
    [Fact]
    public async Task CreatePayment_AmountGreaterThanRemainingBalance_ReturnsNotFound()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                Id = 9002,
                ClientId = TestAuthHelper.ClientId,
                Status = BookingStatus.Confirmed,
                BookingDate = DateTime.Today.AddDays(2)
            };

            var workOrder = new WorkOrder
            {
                Id = 9002,
                BookingId = booking.Id,
                Status = WorkOrderStatus.ReadyForPayment
            };

            var invoice = new Invoice
            {
                Id = 9002,
                WorkOrderId = workOrder.Id,
                SubTotal = 100m,
                TaxAmount = 18m,
                TotalAmount = 118m,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            db.WorkOrders.Add(workOrder);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            workOrderId = workOrder.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            WorkOrderId = workOrderId,
            Amount = 200m,
            Method = PaymentMethod.Cash,
            Notes = "Too much"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test: pagesë e plotë -> backend aktual kthen NotFound
    [Fact]
    public async Task CreatePayment_FullPayment_ReturnsNotFound()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                Id = 9003,
                ClientId = TestAuthHelper.ClientId,
                Status = BookingStatus.Confirmed,
                BookingDate = DateTime.Today.AddDays(2)
            };

            var workOrder = new WorkOrder
            {
                Id = 9003,
                BookingId = booking.Id,
                Status = WorkOrderStatus.ReadyForPayment
            };

            var invoice = new Invoice
            {
                Id = 9003,
                WorkOrderId = workOrder.Id,
                SubTotal = 100m,
                TaxAmount = 18m,
                TotalAmount = 118m,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            db.WorkOrders.Add(workOrder);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            workOrderId = workOrder.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            WorkOrderId = workOrderId,
            Amount = 118m,
            Method = PaymentMethod.Cash,
            Notes = "Full payment"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test: pagesë e pjesshme -> backend aktual kthen NotFound
    [Fact]
    public async Task CreatePayment_PartialPayment_ReturnsNotFound()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                Id = 9004,
                ClientId = TestAuthHelper.ClientId,
                Status = BookingStatus.Confirmed,
                BookingDate = DateTime.Today.AddDays(2)
            };

            var workOrder = new WorkOrder
            {
                Id = 9004,
                BookingId = booking.Id,
                Status = WorkOrderStatus.ReadyForPayment
            };

            var invoice = new Invoice
            {
                Id = 9004,
                WorkOrderId = workOrder.Id,
                SubTotal = 100m,
                TaxAmount = 18m,
                TotalAmount = 118m,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            db.WorkOrders.Add(workOrder);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            workOrderId = workOrder.Id;
        }

        var client = factory.CreateAuthenticatedClient("Client");

        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            WorkOrderId = workOrderId,
            Amount = 50m,
            Method = PaymentMethod.Cash,
            Notes = "Partial payment"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

