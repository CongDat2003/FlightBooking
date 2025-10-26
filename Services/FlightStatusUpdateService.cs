using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Services
{
    public class FlightStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FlightStatusUpdateService> _logger;

        public FlightStatusUpdateService(IServiceProvider serviceProvider, ILogger<FlightStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<FlightBookingContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var now = DateTime.Now;

                    // 1. Update flights to PREPARING status (10 minutes before departure)
                    var preparingFlights = await context.Flights
                        .Where(f => f.Status == "SCHEDULED" &&
                                   f.DepartureTime <= now.AddMinutes(10) &&
                                   f.DepartureTime > now)
                        .ToListAsync(stoppingToken);

                    foreach (var flight in preparingFlights)
                    {
                        flight.Status = "PREPARING";
                        await notificationService.SendFlightUpdateAsync(
                            flight.FlightId,
                            "PREPARING",
                            $"Flight {flight.FlightNumber} is preparing for departure."
                        );
                    }

                    if (preparingFlights.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Updated {preparingFlights.Count} flights to PREPARING status");
                    }

                    // 2. Update flights to DEPARTED status (at departure time)
                    var departedFlights = await context.Flights
                        .Where(f => (f.Status == "PREPARING" || f.Status == "SCHEDULED") &&
                                   f.DepartureTime <= now)
                        .ToListAsync(stoppingToken);

                    foreach (var flight in departedFlights)
                    {
                        flight.Status = "DEPARTED";
                        await notificationService.SendFlightUpdateAsync(
                            flight.FlightId,
                            "DEPARTED",
                            $"Flight {flight.FlightNumber} has departed."
                        );
                    }

                    if (departedFlights.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Updated {departedFlights.Count} flights to DEPARTED status");
                    }

                    // 3. Update flights to COMPLETED status (after arrival time) - FIXED
                    var completedFlights = await context.Flights
                        .Where(f => f.Status == "DEPARTED" &&
                                   f.ArrivalTime <= now) // Changed from < to <= for exact time match
                        .ToListAsync(stoppingToken);

                    foreach (var flight in completedFlights)
                    {
                        flight.Status = "COMPLETED";
                        await notificationService.SendFlightUpdateAsync(
                            flight.FlightId,
                            "COMPLETED",
                            $"Flight {flight.FlightNumber} has completed its journey."
                        );
                    }

                    if (completedFlights.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Updated {completedFlights.Count} flights to COMPLETED status");
                    }

                    // 4. Handle delay logic for unpaid bookings
                    await HandleDelayedBookings(context, notificationService, now, stoppingToken);

                    // 5. Send flight reminders (24 hours before departure)
                    await SendFlightReminders(context, notificationService, now, stoppingToken);

                    // 6. Handle manually set delayed flights (for paid bookings)
                    await HandleManuallyDelayedFlights(context, notificationService, stoppingToken);

                    // 7. Handle cancelled flights
                    await HandleCancelledFlights(context, notificationService, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating flight statuses");
                }

                // Run every 5 minutes for more frequent updates
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task HandleDelayedBookings(FlightBookingContext context, INotificationService notificationService, DateTime now, CancellationToken stoppingToken)
        {
            // Find flights that have departed but have unpaid bookings
            var unpaidBookingsWithDepartedFlights = await context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.PaymentStatus == "PENDING" && // Chưa thanh toán
                           b.BookingStatus == "CONFIRMED" && // Booking đã confirmed
                           b.Flight.DepartureTime <= now && // Chuyến bay đã khởi hành
                           b.Flight.Status != "DELAYED" && // Chưa được đánh dấu delay
                           b.Flight.Status != "CANCELLED") // Chưa bị hủy
                .ToListAsync(stoppingToken);

            var flightIdsToDelay = unpaidBookingsWithDepartedFlights
                .Select(b => b.FlightId)
                .Distinct()
                .ToList();

            if (flightIdsToDelay.Any())
            {
                var flightsToDelay = await context.Flights
                    .Where(f => flightIdsToDelay.Contains(f.FlightId))
                    .ToListAsync(stoppingToken);

                foreach (var flight in flightsToDelay)
                {
                    flight.Status = "DELAYED";

                    // Only send delay notifications to unpaid bookings
                    var unpaidBookings = unpaidBookingsWithDepartedFlights
                        .Where(b => b.FlightId == flight.FlightId)
                        .ToList();

                    foreach (var booking in unpaidBookings)
                    {
                        await notificationService.SendFlightUpdateAsync(
                            flight.FlightId,
                            "DELAYED",
                            $"Flight {flight.FlightNumber} is delayed due to unpaid booking. Please complete payment."
                        );
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Updated {flightsToDelay.Count} flights to DELAYED status due to unpaid bookings");
            }
        }

        private async Task HandleManuallyDelayedFlights(FlightBookingContext context, INotificationService notificationService, CancellationToken stoppingToken)
        {
            // Handle flights that are manually set to DELAYED
            var manuallyDelayedFlights = await context.Flights
                .Where(f => f.Status == "DELAYED")
                .ToListAsync(stoppingToken);

            foreach (var flight in manuallyDelayedFlights)
            {
                // Only send delay notifications to paid bookings for manually delayed flights
                var paidBookings = await context.Bookings
                    .Where(b => b.FlightId == flight.FlightId &&
                               b.PaymentStatus == "PAID" &&
                               b.BookingStatus == "CONFIRMED")
                    .ToListAsync(stoppingToken);

                foreach (var booking in paidBookings)
                {
                    await notificationService.SendFlightUpdateAsync(
                        flight.FlightId,
                        "DELAYED",
                        $"Flight {flight.FlightNumber} is delayed. Please check the updated schedule."
                    );
                }
            }

            if (manuallyDelayedFlights.Any())
            {
                _logger.LogInformation($"Processed {manuallyDelayedFlights.Count} manually delayed flights");
            }
        }

        private async Task SendFlightReminders(FlightBookingContext context, INotificationService notificationService, DateTime now, CancellationToken stoppingToken)
        {
            var reminderTime = now.AddHours(24);
            var reminderStart = reminderTime.AddMinutes(-30);
            var reminderEnd = reminderTime.AddMinutes(30);

            var upcomingBookings = await context.Bookings
                .Include(b => b.Flight)
                .Where(b => b.PaymentStatus == "PAID" && // Chỉ gửi nhắc nhở cho booking đã thanh toán
                           b.BookingStatus == "CONFIRMED" && // Booking đã confirmed
                           b.Flight.DepartureTime >= reminderStart &&
                           b.Flight.DepartureTime <= reminderEnd &&
                           b.Flight.Status != "CANCELLED")
                .ToListAsync(stoppingToken);

            foreach (var booking in upcomingBookings)
            {
                await notificationService.SendFlightReminderAsync(booking.BookingId);
            }

            if (upcomingBookings.Any())
            {
                _logger.LogInformation($"Sent {upcomingBookings.Count} flight reminders");
            }
        }

        private async Task HandleCancelledFlights(FlightBookingContext context, INotificationService notificationService, CancellationToken stoppingToken)
        {
            var cancelledFlights = await context.Flights
                .Where(f => f.Status == "CANCELLED")
                .ToListAsync(stoppingToken);

            foreach (var flight in cancelledFlights)
            {
                await notificationService.SendFlightUpdateAsync(
                    flight.FlightId,
                    "CANCELLED",
                    $"Flight {flight.FlightNumber} has been cancelled. Please contact us for assistance."
                );
            }

            if (cancelledFlights.Any())
            {
                _logger.LogInformation($"Processed {cancelledFlights.Count} cancelled flights");
            }
        }
    }
}