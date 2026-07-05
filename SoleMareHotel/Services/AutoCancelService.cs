using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

namespace SoleMareHotel.Services
{
    public class AutoCancelService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AutoCancelService> _logger;

        public AutoCancelService(IServiceProvider services, ILogger<AutoCancelService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

                    var cutoff = DateTime.Now.AddHours(-24);
                    var expired = await context.Bookings.Include(b => b.Room)
                        .Where(b => b.Status != BookingStatus.CheckedIn && b.Status != BookingStatus.CheckedOut && b.Status != BookingStatus.Cancelled)
                        .Where(b => b.BookingType != BookingType.Corporate)
                        .Where(b => b.CheckIn <= cutoff)
                        .ToListAsync(stoppingToken);

                    if (expired.Count > 0)
                    {
                        foreach (var b in expired)
                        {
                            b.Status = BookingStatus.Cancelled;
                            if (b.Room != null && b.Room.Status == RoomStatus.Occupied) b.Room.Status = RoomStatus.Cleaning;
                        }
                        await context.SaveChangesAsync(stoppingToken);

                        context.ActivityLogs.Add(new ActivityLog
                        {
                            UserName = "Система",
                            Action = "Автоотмена",
                            Description = $"Отменено {expired.Count} броней из-за неявки",
                            Timestamp = DateTime.Now
                        });
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Auto-cancelled {Count} bookings due to no-show", expired.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoCancelService");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
