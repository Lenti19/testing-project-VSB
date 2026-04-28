using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleServiceBooking.Tests.Fixtures;
using VehicleServiceBooking.Web.Data;
using VehicleServiceBooking.Web.Models.Entities;

namespace VehicleServiceBooking.Tests.Integration;
// PERSONA 3 - mechanic schedules integration tests
public class PaymentFlowIntegrationTests
{
    private const string PaymentsRoute = "/api/PaymentsApi";
    private const string InvoicesRoute = "/api/InvoicesApi";

    // Test: pagesë e plotë mbyll WorkOrder dhe Booking
    [Fact]
    public async Task FullPayment_ClosesWorkOrderAndCompletesBooking()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;
        int bookingId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                ClientId = TestAuthHelper.ClientId,
                BookingDate = DateTime.Today.AddDays(2),
                BookingTime = new TimeSpan(10, 0, 0),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            var workOrder = new WorkOrder
            {
                BookingId = booking.Id,
                MechanicId = 1,
                Status = WorkOrderStatus.ReadyForPayment,
                LaborCost = 100m,
                PartsCost = 50m,
                TotalCost = 150m,
                CreatedAt = DateTime.UtcNow
            };

            db.WorkOrders.Add(workOrder);
            await db.SaveChangesAsync();

            bookingId = booking.Id;
            workOrderId = workOrder.Id;
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var invoiceResponse = await manager.PostAsJsonAsync(InvoicesRoute, new
        {
            WorkOrderId = workOrderId,
            TaxRate = 0.18m
        });

        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);

        var paymentResponse = await manager.PostAsJsonAsync(PaymentsRoute, new
        {
            WorkOrderId = workOrderId,
            Amount = 177m,
            Method = PaymentMethod.Cash,
            Notes = "Full payment"
        });

        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var workOrder = await db.WorkOrders.FindAsync(workOrderId);
            var booking = await db.Bookings.FindAsync(bookingId);
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.WorkOrderId == workOrderId);

            Assert.NotNull(workOrder);
            Assert.NotNull(booking);
            Assert.NotNull(payment);

            Assert.Equal(WorkOrderStatus.Closed, workOrder!.Status);
            Assert.Equal(BookingStatus.Completed, booking!.Status);
            Assert.Equal(PaymentStatus.Completed, payment!.Status);
        }
    }

    // Test: pagesë e pjesshme nuk ndryshon statuset
    [Fact]
    public async Task PartialPayment_DoesNotCloseWorkOrderOrBooking()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;
        int bookingId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                ClientId = TestAuthHelper.ClientId,
                BookingDate = DateTime.Today.AddDays(2),
                BookingTime = new TimeSpan(11, 0, 0),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            var workOrder = new WorkOrder
            {
                BookingId = booking.Id,
                MechanicId = 1,
                Status = WorkOrderStatus.ReadyForPayment,
                LaborCost = 100m,
                PartsCost = 50m,
                TotalCost = 150m,
                CreatedAt = DateTime.UtcNow
            };

            db.WorkOrders.Add(workOrder);
            await db.SaveChangesAsync();

            bookingId = booking.Id;
            workOrderId = workOrder.Id;
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var invoiceResponse = await manager.PostAsJsonAsync(InvoicesRoute, new
        {
            WorkOrderId = workOrderId,
            TaxRate = 0.18m
        });

        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);

        var paymentResponse = await manager.PostAsJsonAsync(PaymentsRoute, new
        {
            WorkOrderId = workOrderId,
            Amount = 50m,
            Method = PaymentMethod.Cash,
            Notes = "Partial payment"
        });

        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var workOrder = await db.WorkOrders.FindAsync(workOrderId);
            var booking = await db.Bookings.FindAsync(bookingId);
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.WorkOrderId == workOrderId);

            Assert.NotNull(workOrder);
            Assert.NotNull(booking);
            Assert.NotNull(payment);

            Assert.Equal(WorkOrderStatus.ReadyForPayment, workOrder!.Status);
            Assert.Equal(BookingStatus.Confirmed, booking!.Status);
            Assert.Equal(PaymentStatus.Pending, payment!.Status);
        }
    }

    // Test: pagesa e dytë plotëson balancën dhe mbyll WorkOrder + Booking
    [Fact]
    public async Task SecondPayment_CompletesBalanceAndClosesWorkOrderAndBooking()
    {
        await using var factory = new TestWebApplicationFactory();

        int workOrderId;
        int bookingId;

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var booking = new Booking
            {
                ClientId = TestAuthHelper.ClientId,
                BookingDate = DateTime.Today.AddDays(2),
                BookingTime = new TimeSpan(12, 0, 0),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            var workOrder = new WorkOrder
            {
                BookingId = booking.Id,
                MechanicId = 1,
                Status = WorkOrderStatus.ReadyForPayment,
                LaborCost = 100m,
                PartsCost = 50m,
                TotalCost = 150m,
                CreatedAt = DateTime.UtcNow
            };

            db.WorkOrders.Add(workOrder);
            await db.SaveChangesAsync();

            bookingId = booking.Id;
            workOrderId = workOrder.Id;
        }

        var manager = factory.CreateAuthenticatedClient("Manager");

        var invoiceResponse = await manager.PostAsJsonAsync(InvoicesRoute, new
        {
            WorkOrderId = workOrderId,
            TaxRate = 0.18m
        });

        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);

        var firstPaymentResponse = await manager.PostAsJsonAsync(PaymentsRoute, new
        {
            WorkOrderId = workOrderId,
            Amount = 50m,
            Method = PaymentMethod.Cash,
            Notes = "First partial payment"
        });

        Assert.Equal(HttpStatusCode.Created, firstPaymentResponse.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var firstPayment = await db.Payments.FirstAsync(p => p.WorkOrderId == workOrderId);
            firstPayment.Status = PaymentStatus.Completed;
            await db.SaveChangesAsync();
        }

        var secondPaymentResponse = await manager.PostAsJsonAsync(PaymentsRoute, new
        {
            WorkOrderId = workOrderId,
            Amount = 127m,
            Method = PaymentMethod.Cash,
            Notes = "Second payment completes balance"
        });

        Assert.Equal(HttpStatusCode.Created, secondPaymentResponse.StatusCode);

        using (var scope = factory.CreateTestScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var workOrder = await db.WorkOrders.FindAsync(workOrderId);
            var booking = await db.Bookings.FindAsync(bookingId);
            var completedPaid = await db.Payments
                .Where(p => p.WorkOrderId == workOrderId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount);

            Assert.NotNull(workOrder);
            Assert.NotNull(booking);

            Assert.Equal(177m, completedPaid);
            Assert.Equal(WorkOrderStatus.Closed, workOrder!.Status);
            Assert.Equal(BookingStatus.Completed, booking!.Status);
        }
    }
}