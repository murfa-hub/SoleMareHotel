using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;
using SoleMareHotel.Services;

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class RoomsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RoomsController> _logger;

        public RoomsController(HotelDbContext context, IWebHostEnvironment environment, ILogger<RoomsController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms.Include(r => r.Category).ToListAsync();
            return View(rooms);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Catalog(int? categoryId, int? minPrice, int? maxPrice, int? capacity, DateTime? dateFrom, DateTime? dateTo)
        {
            // Показываем свободные и на уборке (не показываем занятые и на ремонте)
            var rooms = _context.Rooms.Include(r => r.Category)
                .Where(r => r.Status == RoomStatus.Free || r.Status == RoomStatus.Cleaning)
                .AsQueryable();

            if (categoryId != null)
                rooms = rooms.Where(r => r.RoomCategoryId == categoryId);
            if (minPrice != null)
                rooms = rooms.Where(r => r.PricePerNight >= minPrice);
            if (maxPrice != null)
                rooms = rooms.Where(r => r.PricePerNight <= maxPrice);
            if (capacity != null)
                rooms = rooms.Where(r => r.Capacity >= capacity);

            if (dateFrom != null && dateTo != null && dateTo > dateFrom)
            {
                var occupiedRoomIds = await _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.CheckedOut)
                    .Where(b => b.CheckIn < dateTo && b.CheckOut > dateFrom)
                    .Select(b => b.RoomId)
                    .Distinct()
                    .ToListAsync();
                rooms = rooms.Where(r => !occupiedRoomIds.Contains(r.RoomId));
            }

            ViewBag.Categories = _context.RoomCategories.ToList();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Capacity = capacity;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            return View(await rooms.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.Include(r => r.Category).FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null) return NotFound();
            return View(room);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _context.RoomCategories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,RoomNumber,Floor,Location,Status,Capacity,PricePerNight,AdditionalAmenities,Description,RoomCategoryId")] Room room, IFormFile? photo)
        {
            if (photo != null && photo.Length > 0)
            {
                var ext = Path.GetExtension(photo.FileName);
                if (!IsValidImageExtension(ext))
                {
                    ModelState.AddModelError("", "Допустимые форматы: .jpg, .jpeg, .png, .gif, .webp");
                    ViewBag.Categories = _context.RoomCategories.ToList();
                    return View(room);
                }
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "rooms");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                    await photo.CopyToAsync(fileStream);
                room.PhotoPath = "/uploads/rooms/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _context.RoomCategories.ToList();
            return View(room);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            ViewBag.Categories = _context.RoomCategories.ToList();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,RoomNumber,Floor,Location,Status,Capacity,PricePerNight,AdditionalAmenities,Description,PhotoPath,RoomCategoryId")] Room room, IFormFile? photo)
        {
            if (id != room.RoomId) return NotFound();

            if (photo != null && photo.Length > 0)
            {
                var ext = Path.GetExtension(photo.FileName);
                if (!IsValidImageExtension(ext))
                {
                    ModelState.AddModelError("", "Допустимые форматы: .jpg, .jpeg, .png, .gif, .webp");
                    ViewBag.Categories = _context.RoomCategories.ToList();
                    return View(room);
                }
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "rooms");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                if (!string.IsNullOrEmpty(room.PhotoPath))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, room.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                    await photo.CopyToAsync(fileStream);
                room.PhotoPath = "/uploads/rooms/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                _context.Update(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _context.RoomCategories.ToList();
            return View(room);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.Include(r => r.Category).FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                var hasActiveBookings = await _context.Bookings.AnyAsync(b =>
                    b.RoomId == id &&
                    b.Status != BookingStatus.Cancelled &&
                    b.Status != BookingStatus.CheckedOut);
                if (hasActiveBookings)
                {
                    TempData["Error"] = "Невозможно удалить номер с активными бронированиями.";
                    return RedirectToAction(nameof(Index));
                }

                if (!string.IsNullOrEmpty(room.PhotoPath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, room.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Номер {room.RoomNumber} удалён.";
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult ExportExcel([FromServices] ExportService exportService)
        {
            var fileBytes = exportService.ExportRoomsToExcel();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Номера.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteOff(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            var hasActiveBookings = await _context.Bookings.AnyAsync(b =>
                b.RoomId == id &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.CheckedOut);
            if (hasActiveBookings)
            {
                TempData["Error"] = "Невозможно списать номер с активными бронированиями.";
                return RedirectToAction(nameof(Index));
            }

            if (room.Status == RoomStatus.Occupied)
            {
                TempData["Error"] = "Невозможно списать занятый номер.";
                return RedirectToAction(nameof(Index));
            }

            room.Status = RoomStatus.UnderRepair;
            _context.Acts.Add(new Act
            {
                ActType = "Списание номера",
                ActNumber = "ACT-WO-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
                CreatedDate = DateTime.Now,
                DamageDescription = $"Номер {room.RoomNumber} выведен из эксплуатации. Требуется утверждение администрации.",
                RoomId = room.RoomId,
                GuestId = null,
                BookingId = null
            });
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Номер {room.RoomNumber} переведён в статус «На ремонте». Акт на списание создан — требуется утверждение администрации.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreFromRepair(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            if (room.Status != RoomStatus.UnderRepair)
            {
                TempData["Error"] = "Номер не находится на ремонте.";
                return RedirectToAction(nameof(Index));
            }

            room.Status = RoomStatus.Free;
            await _context.SaveChangesAsync();
            await LogAction("Восстановление номера", $"Номер {room.RoomNumber} переведён из «На ремонте» в «Свободен»");

            TempData["Message"] = $"Номер {room.RoomNumber} восстановлен и доступен для заселения.";
            return RedirectToAction(nameof(Index));
        }

        private static bool IsValidImageExtension(string ext)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return allowed.Contains(ext.ToLowerInvariant());
        }

        private async Task LogAction(string action, string description)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserName = User.Identity?.Name ?? "Система",
                Action = action,
                Description = description,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}