using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;
using SoleMareHotel.Services;

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(HotelDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;
            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Where(b => b.CheckIn >= start && b.CheckOut <= end)
                .Where(b => b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.CheckedIn)
                .ToListAsync();

            decimal totalRevenue = 0;
            int totalNights = 0;
            foreach (var b in bookings)
            {
                totalRevenue += b.CalculateFullCost();
                var nights = (b.CheckOut - b.CheckIn).Days;
                totalNights += nights > 0 ? nights : 1;
            }

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.TotalNights = totalNights;
            ViewBag.Bookings = bookings;
            return View();
        }

        public async Task<IActionResult> UnpopularRooms()
        {
            var rooms = await _context.Rooms.Include(r => r.Category).Include(r => r.Bookings).OrderBy(r => r.Bookings!.Count).ToListAsync();
            return View(rooms);
        }

        public async Task<IActionResult> OccupancyRate()
        {
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
            double occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

            ViewBag.TotalRooms = totalRooms;
            ViewBag.OccupiedRooms = occupiedRooms;
            ViewBag.FreeRooms = totalRooms - occupiedRooms;
            ViewBag.OccupancyRate = Math.Round(occupancyRate, 1);

            var roomsByStatus = await _context.Rooms.GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            ViewBag.RoomsByStatus = roomsByStatus;
            return View();
        }

        public async Task<IActionResult> OverdueGuests()
        {
            var today = DateTime.Today;
            var overdueBookings = await _context.Bookings.Include(b => b.Guest).Include(b => b.Room).Where(b => b.Status == BookingStatus.CheckedIn && b.CheckOut < today).ToListAsync();
            return View(overdueBookings);
        }

        public async Task<IActionResult> CategoryPopularity()
        {
            var categories = await _context.RoomCategories.Include(c => c.Rooms!).ThenInclude(r => r.Bookings!).ToListAsync();
            var result = categories.Select(c => new
            {
                Category = c,
                RoomCount = c.Rooms!.Count,
                BookingCount = c.Rooms!.Sum(r => r.Bookings != null ? r.Bookings.Count : 0),
                TotalRevenue = c.Rooms!.Sum(r => r.Bookings != null ? r.Bookings.Sum(b => {
                    var nights = (b.CheckOut - b.CheckIn).Days;
                    if (nights <= 0) nights = 1;
                    return nights * r.PricePerNight;
                }) : 0)
            }).OrderByDescending(c => c.BookingCount).ToList();

            ViewBag.Categories = result;
            return View();
        }

        public async Task<IActionResult> Calendar(int? year, int? month)
        {
            var currentYear = year ?? DateTime.Now.Year;
            var currentMonth = month ?? DateTime.Now.Month;
            var firstDay = new DateTime(currentYear, currentMonth, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
            var startDayOfWeek = (int)firstDay.DayOfWeek;
            if (startDayOfWeek == 0) startDayOfWeek = 7;

            var rooms = await _context.Rooms.Include(r => r.Category).OrderBy(r => r.RoomNumber).ToListAsync();
            var bookings = await _context.Bookings.Include(b => b.Room).Where(b => b.Status != BookingStatus.Cancelled).Where(b => b.CheckIn <= lastDay && b.CheckOut >= firstDay).ToListAsync();

            var occupancy = new Dictionary<int, HashSet<DateTime>>();
            foreach (var room in rooms)
            {
                occupancy[room.RoomId] = new HashSet<DateTime>();
                var roomBookings = bookings.Where(b => b.RoomId == room.RoomId);
                foreach (var booking in roomBookings)
                {
                    var date = booking.CheckIn.Date;
                    var end = booking.CheckOut.Date;
                    while (date < end)
                    {
                        if (date >= firstDay && date <= lastDay) occupancy[room.RoomId].Add(date);
                        date = date.AddDays(1);
                    }
                }
            }

            ViewBag.Year = currentYear;
            ViewBag.Month = currentMonth;
            ViewBag.MonthName = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.GetMonthName(currentMonth);
            ViewBag.DaysInMonth = daysInMonth;
            ViewBag.StartDayOfWeek = startDayOfWeek;
            ViewBag.FirstDay = firstDay;
            ViewBag.Rooms = rooms;
            ViewBag.Occupancy = occupancy;
            ViewBag.PrevMonth = firstDay.AddMonths(-1);
            ViewBag.NextMonth = firstDay.AddMonths(1);
            return View();
        }

        public async Task<IActionResult> ActivityLog()
        {
            var logs = await _context.ActivityLogs.OrderByDescending(l => l.Timestamp).Take(200).ToListAsync();
            return View(logs);
        }

        public IActionResult ExportRevenueExcel(DateTime startDate, DateTime endDate, [FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportRevenueToExcel(startDate, endDate);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Выручка_{startDate:ddMMyyyy}-{endDate:ddMMyyyy}.xlsx");
        }

        public IActionResult ExportRevenueWord(DateTime startDate, DateTime endDate, [FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportRevenueToWord(startDate, endDate);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Выручка_{startDate:ddMMyyyy}-{endDate:ddMMyyyy}.docx");
        }

        public IActionResult ExportUnpopularExcel([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportUnpopularRoomsToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Непопулярные_номера.xlsx");
        }

        public IActionResult ExportOccupancyExcel([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportOccupancyToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Занятость_номеров.xlsx");
        }

        public IActionResult ExportOverdueExcel([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportOverdueToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Просроченные_выезды.xlsx");
        }

        public IActionResult ExportCategoryExcel([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportCategoryPopularityToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Популярность_категорий.xlsx");
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> AvailabilityCalendar(int? month, int? year)
        {
            var today = DateTime.Today;
            var m = month ?? today.Month;
            var y = year ?? today.Year;
            var startDate = new DateTime(y, m, 1);
            var endDate = startDate.AddDays(30);

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            var rooms = await _context.Rooms
                .Include(r => r.Category)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();
            ViewBag.Rooms = rooms;

            var bookings = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled
                    && b.CheckIn < endDate && b.CheckOut > startDate)
                .ToListAsync();
            ViewBag.Bookings = bookings;

            return View();
        }
    }
}