using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

#pragma warning disable CS8602

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class PaymentsController : Controller
    {
        private readonly HotelDbContext _context;

        public PaymentsController(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();
            return View(booking);
        }

        public IActionResult Create(int bookingId)
        {
            ViewBag.BookingId = bookingId;
            ViewBag.Methods = Enum.GetValues(typeof(PaymentMethod));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bookingId, decimal amount, string method, string? description)
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();

            if (amount <= 0)
            {
                TempData["Error"] = "Сумма должна быть больше нуля.";
                return RedirectToAction("Index", new { bookingId });
            }

            decimal debt = booking.CalculateRemainingDebt();
            if (amount > debt)
            {
                TempData["Error"] = $"Сумма превышает задолженность ({debt:N2} €).";
                return RedirectToAction("Index", new { bookingId });
            }

            if (!Enum.TryParse<PaymentMethod>(method, out var payMethod))
                payMethod = PaymentMethod.Cash;

            var payment = new Payment
            {
                PaymentNumber = "PAY-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                PaymentDate = DateTime.Now,
                Amount = amount,
                Method = payMethod,
                Description = description,
                BookingId = bookingId
            };
            _context.Payments.Add(payment);

            _context.LedgerEntries.Add(new LedgerEntry
            {
                EntryType = LedgerEntryType.Payment,
                Date = DateTime.Now,
                Description = $"Оплата по брони {booking.BookingNumber} ({payMethod.GetDisplayName()})",
                Credit = amount,
                Debit = 0,
                Balance = 0,
                GuestId = booking.GuestId,
                BookingId = bookingId
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = $"Оплата {amount:N2} € принята. Остаток задолженности: {booking.CalculateRemainingDebt() - amount:N2} €.";
            return RedirectToAction("Index", new { bookingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int paymentId, int bookingId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Платёж удалён.";
            }
            return RedirectToAction("Index", new { bookingId });
        }
    }
}
