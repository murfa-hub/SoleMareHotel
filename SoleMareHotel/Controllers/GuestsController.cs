using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;
using SoleMareHotel.Services;

#pragma warning disable CS8602

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class GuestsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GuestsController(HotelDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var staffEmails = new HashSet<string>();
            foreach (var role in new[] { "Admin", "Receptionist", "Housekeeper" })
            {
                var users = await _userManager.GetUsersInRoleAsync(role);
                foreach (var u in users)
                {
                    if (!string.IsNullOrEmpty(u.Email))
                        staffEmails.Add(u.Email);
                }
            }

            var guests = _context.Guests.Where(g => (g.Email == null || !staffEmails.Contains(g.Email)) && g.GuestType != "Организация").AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                guests = guests.Where(g =>
                    (g.FullName != null && g.FullName.Contains(search)) ||
                    (g.Phone != null && g.Phone.Contains(search)) ||
                    (g.Email != null && g.Email.Contains(search)) ||
                    (g.GuestCardNumber != null && g.GuestCardNumber.Contains(search)));
            }
            ViewBag.Search = search;
            return View(await guests.OrderBy(g => g.FullName).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,IdentificationNumber,PassportNumber,Phone,RegistrationAddress,BirthDate,Email,GuestType")] Guest guest)
        {
            if (string.IsNullOrWhiteSpace(guest.FullName))
                ModelState.AddModelError("", "ФИО обязательно.");

            if (string.IsNullOrWhiteSpace(guest.Phone))
                ModelState.AddModelError("Phone", "Номер телефона обязателен.");

            if (ModelState.IsValid)
            {
                guest.GuestCardNumber = "GC-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Гость {guest.FullName} добавлен. Карта: {guest.GuestCardNumber}";
                return RedirectToAction(nameof(Index));
            }
            return View(guest);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();
            return View(guest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GuestId,GuestCardNumber,FullName,IdentificationNumber,PassportNumber,Phone,RegistrationAddress,BirthDate,Email,GuestType,OrganizationName,INN,OGRN,LegalAddress,ContactPersonName,ContactPersonPosition,ContactPersonPhone,ContactPersonEmail")] Guest guest)
        {
            if (id != guest.GuestId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(guest);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Данные гостя {guest.FullName} обновлены.";
                return RedirectToAction(nameof(Index));
            }
            return View(guest);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            var hasActiveBookings = await _context.Bookings.AnyAsync(b =>
                b.GuestId == id &&
                (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
            if (hasActiveBookings)
            {
                TempData["Error"] = "Невозможно удалить гостя с активными бронированиями.";
                return RedirectToAction(nameof(Index));
            }
            return View(guest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest != null)
            {
                var hasActiveBookings = await _context.Bookings.AnyAsync(b =>
                    b.GuestId == id &&
                    (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
                if (hasActiveBookings)
                {
                    TempData["Error"] = "Невозможно удалить гостя с активными бронированиями.";
                    return RedirectToAction(nameof(Index));
                }

                var ledgerEntries = await _context.LedgerEntries.Where(l => l.GuestId == id).ToListAsync();
                if (ledgerEntries.Any())
                    _context.LedgerEntries.RemoveRange(ledgerEntries);

                _context.Guests.Remove(guest);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Гость {guest.FullName} удалён.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Annul(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            var hasActiveBookings = await _context.Bookings.AnyAsync(b =>
                b.GuestId == id &&
                (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
            if (hasActiveBookings)
            {
                TempData["Error"] = "Невозможно аннулировать данные гостя с активными бронированиями.";
                return RedirectToAction(nameof(Index));
            }

            var hasDebt = await _context.Bookings
                .Where(b => b.GuestId == id)
                .Include(b => b.Payments)
                .Include(b => b.ServiceOrders)
                .ToListAsync();
            foreach (var b in hasDebt)
            {
                if (b.CalculateRemainingDebt() > 0)
                {
                    TempData["Error"] = "Невозможно аннулировать данные гостя: имеется задолженность.";
                    return RedirectToAction(nameof(Index));
                }
            }

            guest.IsAnnuiled = true;
            guest.FullName = "[Аннулировано]";
            guest.Phone = null;
            guest.IdentificationNumber = null;
            guest.PassportNumber = null;
            guest.RegistrationAddress = null;
            guest.BirthDate = null;
            guest.OrganizationName = null;
            guest.INN = null;
            guest.OGRN = null;
            guest.LegalAddress = null;
            guest.ContactPersonName = null;
            guest.ContactPersonPosition = null;
            guest.ContactPersonPhone = null;
            guest.ContactPersonEmail = null;
            guest.Email = null;

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Данные гостя аннулированы. Карта: {guest.GuestCardNumber}";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ExportExcel([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportGuestsToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Гости.xlsx");
        }

        public IActionResult ExportWord([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportGuestsListToWord();
            if (bytes == null) return NotFound();
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Гости.docx");
        }
    }
}
