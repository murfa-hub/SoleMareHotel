using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

#pragma warning disable CS8602

namespace SoleMareHotel.Controllers
{
    public class BookingRequestsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BookingRequestsController> _logger;

        public BookingRequestsController(HotelDbContext context, UserManager<ApplicationUser> userManager, ILogger<BookingRequestsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Index()
        {
            var requests = await _context.BookingRequests
                .Include(r => r.Guest)
                .Include(r => r.Organization)
                .Include(r => r.Room)!.ThenInclude(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        [Authorize]
        public async Task<IActionResult> MyRequests()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return View(new List<BookingRequest>());

            var requests = await _context.BookingRequests
                .Include(r => r.Room)!.ThenInclude(r => r.Category)
                .Include(r => r.Guest)
                .Where(r => r.Guest != null && r.Guest.Email == userEmail)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int roomId, DateTime checkIn, DateTime checkOut, int adults, int children, string? comment)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            int totalGuests = adults + children;

            if (totalGuests > room.Capacity)
            {
                TempData["Error"] = $"Номер вмещает максимум {room.Capacity} чел.";
                return RedirectToAction("Catalog", "Rooms");
            }
            if (adults <= 0)
            {
                TempData["Error"] = "Должен быть хотя бы 1 взрослый.";
                return RedirectToAction("Catalog", "Rooms");
            }
            if (checkOut <= checkIn)
            {
                TempData["Error"] = "Дата выезда должна быть позже даты заезда.";
                return RedirectToAction("Catalog", "Rooms");
            }
            if ((checkOut - checkIn).TotalDays > 30)
            {
                TempData["Error"] = "Максимальный срок бронирования — 30 дней.";
                return RedirectToAction("Catalog", "Rooms");
            }
            if (checkIn.Date < DateTime.Today)
            {
                TempData["Error"] = "Дата заезда не может быть в прошлом.";
                return RedirectToAction("Catalog", "Rooms");
            }

            var userEmail = User.Identity?.Name ?? "";
            var appUser = await _userManager.FindByEmailAsync(userEmail);
            var fullName = appUser?.FullName ?? "Гость";

            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == userEmail);
            if (guest == null)
            {
                var isStaff = appUser != null && (
                    await _userManager.IsInRoleAsync(appUser, "Admin") ||
                    await _userManager.IsInRoleAsync(appUser, "Receptionist") ||
                    await _userManager.IsInRoleAsync(appUser, "Housekeeper"));
                if (isStaff)
                {
                    TempData["Error"] = "Сотрудники не могут создавать заявки как гости.";
                    return RedirectToAction("Catalog", "Rooms");
                }
                guest = new Guest
                {
                    FullName = fullName,
                    Email = userEmail,
                    GuestType = "Физическое лицо",
                    GuestCardNumber = "GC-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper()
                };
                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();
            }

            if (!guest.IsAdult())
            {
                TempData["Error"] = "Бронирование разрешено только для лиц старше 18 лет.";
                return RedirectToAction("Catalog", "Rooms");
            }

            var request = new BookingRequest
            {
                RequestNumber = "REQ-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                CheckIn = checkIn,
                CheckOut = checkOut,
                Guests = totalGuests,
                Adults = adults,
                Children = children,
                Status = RequestStatus.New,
                Comment = comment,
                GuestId = guest.GuestId,
                RoomId = roomId,
                CreatedAt = DateTime.Now
            };

            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Заявка {request.RequestNumber} отправлена!";
            return RedirectToAction("MyRequests");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int requestId)
        {
            var request = await _context.BookingRequests
                .Include(r => r.Room)
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(r => r.BookingRequestId == requestId);

            if (request == null) return NotFound();

            if (request.Room != null && request.Room.Status != RoomStatus.Free && request.Room.Status != RoomStatus.Cleaning)
            {
                TempData["Error"] = $"Номер {request.Room.RoomNumber} уже занят.";
                return RedirectToAction("Index");
            }

            if (request.Room != null && request.Guests > request.Room.Capacity)
            {
                TempData["Error"] = $"Номер вмещает максимум {request.Room.Capacity} чел.";
                return RedirectToAction("Index");
            }

            if (request.Room != null)
            {
                var hasOverlap = await _context.Bookings.AnyAsync(b =>
                    b.RoomId == request.RoomId &&
                    b.Status != BookingStatus.Cancelled &&
                    b.CheckIn < request.CheckOut && b.CheckOut > request.CheckIn);
                if (hasOverlap)
                {
                    TempData["Error"] = $"Номер {request.Room.RoomNumber} уже забронирован на эти даты.";
                    return RedirectToAction("Index");
                }

                var hasPendingRequest = await _context.BookingRequests.AnyAsync(br =>
                    br.RoomId == request.RoomId &&
                    br.BookingRequestId != request.BookingRequestId &&
                    br.Status == RequestStatus.Confirmed &&
                    br.CheckIn < request.CheckOut && br.CheckOut > request.CheckIn);
                if (hasPendingRequest)
                {
                    TempData["Error"] = $"Номер {request.Room.RoomNumber} уже подтверждён в другой заявке на эти даты.";
                    return RedirectToAction("Index");
                }
            }

            request.Status = RequestStatus.Confirmed;

            var booking = new Booking
            {
                BookingNumber = (request.BookingType == BookingType.Corporate ? "BR-C-" : "BR-I-") + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                CheckIn = request.CheckIn,
                CheckOut = request.CheckOut,
                NumberOfGuests = request.Guests,
                Adults = request.Adults,
                Children = request.Children,
                Status = BookingStatus.Confirmed,
                BookingType = request.BookingType,
                OrganizationId = request.OrganizationId,
                CompanyContactPerson = request.ContactPerson,
                CompanyPhone = request.CompanyPhone,
                EmployeeList = request.EmployeesList,
                IsCompanyGuarantor = request.IsCompanyGuarantor,
                GuestId = request.GuestId,
                RoomId = request.RoomId
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.BookingRooms.Add(new BookingRoom { BookingId = booking.BookingId, RoomId = request.RoomId });
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Заявка {request.RequestNumber} подтверждена. Бронь {booking.BookingNumber} создана.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int requestId)
        {
            var request = await _context.BookingRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.Status = RequestStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Заявка {request.RequestNumber} отклонена.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int requestId)
        {
            var userEmail = User.Identity?.Name ?? "";
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == userEmail);
            if (guest == null) return Forbid();

            var request = await _context.BookingRequests
                .Include(r => r.Guest)
                .FirstOrDefaultAsync(r => r.BookingRequestId == requestId);

            if (request == null) return NotFound();
            if (request.GuestId != guest.GuestId) return Forbid();
            if (request.Status != RequestStatus.New) 
            {
                TempData["Error"] = "Можно отменить только заявку в статусе «Новая».";
                return RedirectToAction("MyRequests");
            }

            request.Status = RequestStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Заявка {request.RequestNumber} отменена.";
            return RedirectToAction("MyRequests");
        }
    }
}