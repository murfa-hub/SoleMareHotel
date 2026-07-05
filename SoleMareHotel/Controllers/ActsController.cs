using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;
using SoleMareHotel.Services;

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class ActsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<ActsController> _logger;

        public ActsController(HotelDbContext context, ILogger<ActsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var acts = await _context.Acts
                .Include(a => a.Booking)
                .Include(a => a.Guest)
                .Include(a => a.Room)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
            return View(acts);
        }

        public IActionResult CreateCheckIn()
        {
            ViewBag.Bookings = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
                .ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckIn(int bookingId, DateTime checkInDate, string checkInTime, int numberOfGuests)
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            var act = new Act
            {
                ActType = "Заселение",
                ActNumber = "ACT-IN-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
                CreatedDate = DateTime.Now,
                CheckInDate = checkInDate,
                CheckInTime = checkInTime,
                NumberOfGuests = numberOfGuests,
                BookingId = bookingId,
                GuestId = booking.GuestId,
                RoomId = booking.RoomId
            };

            _context.Acts.Add(act);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Акт заселения {act.ActNumber} создан.";
            return RedirectToAction("Index");
        }

        public IActionResult ExportWord(int id, [FromServices] ExportService exportService)
        {
            var fileBytes = exportService.ExportActToWord(id);
            if (fileBytes == null) return NotFound();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Акт_{id}.docx");
        }
    }
}