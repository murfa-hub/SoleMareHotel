using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class GuestServiceOrdersController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<GuestServiceOrdersController> _logger;

        public GuestServiceOrdersController(HotelDbContext context, ILogger<GuestServiceOrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int bookingId)
        {
            ViewBag.BookingId = bookingId;
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();
            ViewBag.Booking = booking;

            var orders = await _context.GuestServiceOrders
                .Include(o => o.Service)
                .Where(o => o.BookingId == bookingId)
                .ToListAsync();

            return View(orders);
        }

        public IActionResult Create(int bookingId)
        {
            ViewBag.BookingId = bookingId;
            ViewBag.Services = _context.Services.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int BookingId, DateTime OrderDate, [FromForm] List<ServiceItem> services)
        {
            if (services == null || !services.Any(s => s.ServiceId > 0))
            {
                ViewBag.BookingId = BookingId;
                ViewBag.Services = _context.Services.ToList();
                ModelState.AddModelError("", "Добавьте хотя бы одну услугу.");
                return View();
            }

            var serviceIds = services.Where(s => s.ServiceId > 0).Select(s => s.ServiceId).Distinct().ToList();
            var dbServices = await _context.Services.Where(s => serviceIds.Contains(s.ServiceId)).ToDictionaryAsync(s => s.ServiceId);

            foreach (var item in services.Where(s => s.ServiceId > 0))
            {
                if (!dbServices.TryGetValue(item.ServiceId, out var dbService))
                    continue;

                var order = new GuestServiceOrder
                {
                    BookingId = BookingId,
                    ServiceId = item.ServiceId,
                    Quantity = item.Quantity > 0 ? item.Quantity : 1,
                    PriceCharged = dbService.Price,
                    OrderDate = OrderDate
                };
                _context.GuestServiceOrders.Add(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { bookingId = BookingId });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.GuestServiceOrders
                .Include(o => o.Service)
                .Include(o => o.Booking)
                .FirstOrDefaultAsync(m => m.GuestServiceOrderId == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.GuestServiceOrders.FindAsync(id);
            if (order != null)
            {
                int bookingId = order.BookingId;
                _context.GuestServiceOrders.Remove(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { bookingId });
            }
            return RedirectToAction("Index", "Bookings");
        }
    }

    // Вспомогательный класс для массового добавления
    public class ServiceItem
    {
        public int ServiceId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Price { get; set; }
    }
}