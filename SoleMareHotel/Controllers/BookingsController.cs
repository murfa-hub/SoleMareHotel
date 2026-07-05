using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;
using SoleMareHotel.Services;

#pragma warning disable CS8602

namespace SoleMareHotel.Controllers
{
    public class BookingsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<BookingsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(HotelDbContext context, ILogger<BookingsController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Organization)
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.BookingRooms)!.ThenInclude(br => br.Room)!.ThenInclude(r => r.Category)
                .OrderByDescending(b => b.CheckIn)
                .ToListAsync();
            return View(bookings);
        }

        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var userEmail = User.Identity?.Name ?? "";
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == userEmail);

            if (guest == null)
            {
                guest = await _context.Bookings
                    .Include(b => b.Guest)
                    .Where(b => b.Guest != null && b.Guest.Email == userEmail)
                    .Select(b => b.Guest!)
                    .FirstOrDefaultAsync();
            }
            if (guest == null)
            {
                guest = await _context.BookingRequests
                    .Include(r => r.Guest)
                    .Where(r => r.Guest != null && r.Guest.Email == userEmail)
                    .Select(r => r.Guest!)
                    .FirstOrDefaultAsync();
            }
            if (guest == null)
            {
                var appUser = await _userManager.FindByEmailAsync(userEmail);
                if (appUser != null)
                {
                    var isStaff = await _userManager.IsInRoleAsync(appUser, "Admin") ||
                                  await _userManager.IsInRoleAsync(appUser, "Receptionist") ||
                                  await _userManager.IsInRoleAsync(appUser, "Housekeeper");
                    if (!isStaff)
                    {
                        guest = new Guest
                        {
                            FullName = appUser.FullName,
                            Email = userEmail,
                            GuestType = "Физическое лицо",
                            GuestCardNumber = "GC-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper()
                        };
                        _context.Guests.Add(guest);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            if (guest == null) return RedirectToAction("Index", "Home");

            var guestId = guest.GuestId;
            var bookings = await _context.Bookings
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.BookingRooms)!.ThenInclude(br => br.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Include(b => b.Payments)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CheckIn)
                .ToListAsync();
            return View(bookings);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var userEmail = User.Identity?.Name ?? "";
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == userEmail);
            if (guest == null) return Forbid();

            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();
            if (booking.GuestId != guest.GuestId) return Forbid();
            if (booking.Status != BookingStatus.Booked && booking.Status != BookingStatus.Confirmed)
            {
                TempData["Error"] = "Можно отменить только бронь в статусе «Забронировано» или «Подтверждено».";
                return RedirectToAction("MyBookings");
            }
            if (booking.CheckIn <= DateTime.Now.AddHours(24))
            {
                TempData["Error"] = "Отмена возможна не позже чем за 24 часа до заезда.";
                return RedirectToAction("MyBookings");
            }

            booking.Status = BookingStatus.Cancelled;

            var roomIds = new List<int> { booking.RoomId };
            if (booking.BookingRooms != null)
                roomIds.AddRange(booking.BookingRooms.Select(br => br.RoomId));

            foreach (var rid in roomIds.Distinct())
            {
                var room = await _context.Rooms.FindAsync(rid);
                if (room != null && room.Status == RoomStatus.Occupied)
                    room.Status = RoomStatus.Cleaning;
            }

            await _context.SaveChangesAsync();
            await LogAction("Отмена брони", $"Бронь {booking.BookingNumber} отменена гостем");

            TempData["Message"] = $"Бронь {booking.BookingNumber} отменена.";
            return RedirectToAction("MyBookings");
        }

        [Authorize]
        public async Task<IActionResult> GuestBook(int roomId)
        {
            var room = await _context.Rooms.Include(r => r.Category).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return NotFound();
            ViewBag.Organizations = await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync();
            return View(room);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuestBook(int roomId, DateTime checkIn, DateTime checkOut, int adults, int children, string? comment, string bookingType, int? organizationId, string? contactPerson, string? companyPhone, string? employeesList, bool isCompanyGuarantor)
        {
            var userEmail = User.Identity?.Name ?? "";
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            if (!Enum.TryParse<BookingType>(bookingType, out var type))
            {
                TempData["Error"] = "Неверный тип бронирования.";
                return RedirectToAction("Catalog", "Rooms");
            }
            if (type == BookingType.Corporate)
            {
                if (organizationId == null || organizationId <= 0)
                {
                    TempData["Error"] = "Для корпоративного бронирования выберите организацию.";
                    return RedirectToAction("Catalog", "Rooms");
                }
                var org = await _context.Organizations.FindAsync(organizationId);
                if (org == null || !org.IsActive)
                {
                    TempData["Error"] = "Организация не найдена или договор не действует.";
                    return RedirectToAction("Catalog", "Rooms");
                }
            }

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
                    TempData["Error"] = "Сотрудники не могут бронировать номера как гости.";
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
                BookingType = type,
                OrganizationId = organizationId,
                ContactPerson = contactPerson,
                CompanyPhone = companyPhone,
                EmployeesList = employeesList,
                IsCompanyGuarantor = isCompanyGuarantor,
                GuestId = guest.GuestId,
                RoomId = roomId,
                CreatedAt = DateTime.Now
            };

            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Заявка {request.RequestNumber} отправлена!";
            return RedirectToAction("MyRequests", "BookingRequests");
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Guests = await GetGuestsExcludingStaff();
            ViewBag.Rooms = _context.Rooms.Where(r => r.Status == RoomStatus.Free).Include(r => r.Category).ToList();
            ViewBag.Organizations = await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Create([Bind("BookingId,BookingNumber,CheckIn,CheckOut,Status,NumberOfGuests,Adults,Children,BookingType,OrganizationId,CompanyContactPerson,CompanyPhone,EmployeeList,IsCompanyGuarantor,GuestId,RoomId")] Booking booking, int[] additionalRoomIds)
        {
            var room = await _context.Rooms.Include(r => r.Category).FirstOrDefaultAsync(r => r.RoomId == booking.RoomId);
            if (room == null) ModelState.AddModelError("", "Номер не найден.");
            else if (room.Status != RoomStatus.Free) ModelState.AddModelError("", $"Номер {room.RoomNumber} недоступен.");
            if (room != null && booking.NumberOfGuests > room.Capacity) ModelState.AddModelError("", $"Макс. {room.Capacity} чел.");
            if (!booking.IsValidDuration()) ModelState.AddModelError("", "Максимум 30 дней.");
            if (booking.CheckOut <= booking.CheckIn) ModelState.AddModelError("", "Дата выезда позже заезда.");
            if (booking.BookingType == BookingType.Corporate)
            {
                if (booking.OrganizationId == null || booking.OrganizationId <= 0)
                    ModelState.AddModelError("", "Выберите организацию.");
                else
                {
                    var org = await _context.Organizations.FindAsync(booking.OrganizationId);
                    if (org == null || !org.IsActive)
                        ModelState.AddModelError("", "Организация не найдена или договор не действует.");
                }
            }

            var guest = await _context.Guests.FindAsync(booking.GuestId);
            if (guest != null && !guest.IsAdult()) ModelState.AddModelError("", "Бронирование разрешено только для лиц старше 18 лет.");

            if (room != null)
            {
                var hasOverlap = await _context.Bookings.AnyAsync(b =>
                    b.RoomId == booking.RoomId &&
                    b.BookingId != booking.BookingId &&
                    b.Status != BookingStatus.Cancelled &&
                    b.CheckIn < booking.CheckOut && b.CheckOut > booking.CheckIn);
                if (hasOverlap) ModelState.AddModelError("", $"Номер {room.RoomNumber} уже забронирован на эти даты.");
            }

            if (additionalRoomIds != null && additionalRoomIds.Length > 0)
            {
                additionalRoomIds = additionalRoomIds.Where(id => id != booking.RoomId).Distinct().ToArray();
                int totalRooms = 1 + additionalRoomIds.Length;
                if (totalRooms > 5) ModelState.AddModelError("", "Максимум 5 номеров в одном бронировании.");

                foreach (var rid in additionalRoomIds)
                {
                    var r = await _context.Rooms.FindAsync(rid);
                    if (r == null) { ModelState.AddModelError("", $"Номер {rid} не найден."); continue; }
                    if (r.Status != RoomStatus.Free) ModelState.AddModelError("", $"Номер {r.RoomNumber} недоступен.");
                    var overlap = await _context.Bookings.AnyAsync(b =>
                        b.RoomId == rid &&
                        b.Status != BookingStatus.Cancelled &&
                        b.CheckIn < booking.CheckOut && b.CheckOut > booking.CheckIn);
                    if (overlap) ModelState.AddModelError("", $"Номер {r.RoomNumber} уже забронирован на эти даты.");
                }
            }

            {
                var activeBookings = await _context.Bookings.CountAsync(b =>
                    b.GuestId == booking.GuestId &&
                    (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
                if (activeBookings >= 5) ModelState.AddModelError("", "У гостя уже 5 активных бронирований (максимум).");
            }

            if (string.IsNullOrEmpty(booking.BookingNumber))
                booking.BookingNumber = (booking.BookingType == BookingType.Corporate ? "BR-C-" : "BR-I-") + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

            if (ModelState.IsValid)
            {
                if (room != null && booking.Status == BookingStatus.CheckedIn) { room.Status = RoomStatus.Occupied; _context.Update(room); }

                _context.Add(booking);
                await _context.SaveChangesAsync();

                _context.BookingRooms.Add(new BookingRoom { BookingId = booking.BookingId, RoomId = booking.RoomId });
                if (additionalRoomIds != null)
                {
                    foreach (var rid in additionalRoomIds)
                    {
                        if (rid != booking.RoomId)
                        {
                            _context.BookingRooms.Add(new BookingRoom { BookingId = booking.BookingId, RoomId = rid });
                            var addRoom = await _context.Rooms.FindAsync(rid);
                            if (addRoom != null && booking.Status == BookingStatus.CheckedIn)
                            {
                                addRoom.Status = RoomStatus.Occupied;
                                _context.Update(addRoom);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await LogAction("Создание брони", $"Бронь {booking.BookingNumber}, номер(а) {room?.RoomNumber}");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Guests = await GetGuestsExcludingStaff();
            ViewBag.Rooms = _context.Rooms.Where(r => r.Status == RoomStatus.Free).Include(r => r.Category).ToList();
            return View(booking);
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Bookings
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();
            ViewBag.Guests = await GetGuestsExcludingStaff();
            ViewBag.Rooms = _context.Rooms.Include(r => r.Category).ToList();
            ViewBag.Organizations = await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync();
            ViewBag.BookingRooms = booking.BookingRooms?.Select(br => br.RoomId).ToList() ?? new List<int>();
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,BookingNumber,CheckIn,CheckOut,Status,NumberOfGuests,Adults,Children,BookingType,OrganizationId,CompanyContactPerson,CompanyPhone,EmployeeList,IsCompanyGuarantor,GuestId,RoomId")] Booking booking)
        {
            if (id != booking.BookingId) return NotFound();
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            if (room != null && booking.NumberOfGuests > room.Capacity) ModelState.AddModelError("", $"Макс. {room.Capacity} чел.");
            if (!booking.IsValidDuration()) ModelState.AddModelError("", "Максимум 30 дней.");
            if (booking.CheckOut <= booking.CheckIn) ModelState.AddModelError("", "Дата выезда позже заезда.");

            if (room != null)
            {
                var hasOverlap = await _context.Bookings.AnyAsync(b =>
                    b.RoomId == booking.RoomId &&
                    b.BookingId != booking.BookingId &&
                    b.Status != BookingStatus.Cancelled &&
                    b.CheckIn < booking.CheckOut && b.CheckOut > booking.CheckIn);
                if (hasOverlap) ModelState.AddModelError("", $"Номер {room.RoomNumber} уже забронирован на эти даты.");
            }

            if (ModelState.IsValid)
            {
                var existingBooking = await _context.Bookings
                    .Include(b => b.BookingRooms)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (existingBooking != null)
                {
                    var oldStatus = existingBooking.Status;
                    var newStatus = booking.Status;

                    existingBooking.CheckIn = booking.CheckIn;
                    existingBooking.CheckOut = booking.CheckOut;
                    existingBooking.Status = newStatus;
                    existingBooking.NumberOfGuests = booking.NumberOfGuests;
                    existingBooking.Adults = booking.Adults;
                    existingBooking.Children = booking.Children;
                    existingBooking.BookingType = booking.BookingType;
                    existingBooking.OrganizationId = booking.OrganizationId;
                    existingBooking.CompanyContactPerson = booking.CompanyContactPerson;
                    existingBooking.CompanyPhone = booking.CompanyPhone;
                    existingBooking.EmployeeList = booking.EmployeeList;
                    existingBooking.IsCompanyGuarantor = booking.IsCompanyGuarantor;
                    existingBooking.GuestId = booking.GuestId;
                    existingBooking.RoomId = booking.RoomId;

                    if (oldStatus != newStatus)
                    {
                        var allRoomIds = new List<int> { booking.RoomId };
                        if (existingBooking.BookingRooms != null)
                            allRoomIds.AddRange(existingBooking.BookingRooms.Select(br => br.RoomId));

                        foreach (var rid in allRoomIds.Distinct())
                        {
                            var rm = await _context.Rooms.FindAsync(rid);
                            if (rm == null) continue;

                            if (newStatus == BookingStatus.CheckedIn && rm.Status != RoomStatus.Occupied)
                            {
                                rm.Status = RoomStatus.Occupied;
                            }
                            else if (newStatus == BookingStatus.CheckedOut && rm.Status == RoomStatus.Occupied)
                            {
                                rm.Status = RoomStatus.Cleaning;
                            }
                            else if (newStatus == BookingStatus.Cancelled && rm.Status == RoomStatus.Occupied)
                            {
                                rm.Status = RoomStatus.Cleaning;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await LogAction("Изменение брони", $"Бронь {existingBooking.BookingNumber}, статус → {booking.Status}");
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Guests = await GetGuestsExcludingStaff();
            ViewBag.Rooms = _context.Rooms.Include(r => r.Category).ToList();
            return View(booking);
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Organization)
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.BookingRooms)!.ThenInclude(br => br.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Include(b => b.Payments)
                .Include(b => b.Acts)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.BookingRooms)!.ThenInclude(br => br.Room)!.ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Acts)
                .Include(b => b.BookingRooms)
                .Include(b => b.Payments)
                .Include(b => b.ServiceOrders)
                .FirstOrDefaultAsync(b => b.BookingId == id);
            if (booking != null)
            {
                var roomIds = new List<int> { booking.RoomId };
                if (booking.BookingRooms != null)
                    roomIds.AddRange(booking.BookingRooms.Select(br => br.RoomId));

                var roomsToClean = new List<int>();
                foreach (var rid in roomIds.Distinct())
                {
                    var r = await _context.Rooms.FindAsync(rid);
                    if (r != null)
                    {
                        if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut)
                        {
                            r.Status = RoomStatus.Cleaning;
                            roomsToClean.Add(r.RoomId);
                        }
                        else
                        {
                            r.Status = RoomStatus.Free;
                        }
                    }
                }

                if (booking.BookingRooms != null)
                    _context.BookingRooms.RemoveRange(booking.BookingRooms);

                foreach (var roomId in roomsToClean)
                {
                    _context.CleaningTasks.Add(new CleaningTask
                    {
                        RoomId = roomId,
                        CleaningType = CleaningType.Departure,
                        Status = CleaningTaskStatus.Assigned,
                        Notes = $"Уборка после удаления брони {booking.BookingNumber}",
                        CreatedAt = DateTime.Now
                    });
                }

                if (booking.Acts != null && booking.Acts.Any()) _context.Acts.RemoveRange(booking.Acts);
                if (booking.Payments != null && booking.Payments.Any()) _context.Payments.RemoveRange(booking.Payments);
                if (booking.ServiceOrders != null && booking.ServiceOrders.Any()) _context.GuestServiceOrders.RemoveRange(booking.ServiceOrders);

                var ledgerEntries = await _context.LedgerEntries.Where(l => l.BookingId == id).ToListAsync();
                if (ledgerEntries.Any())
                    _context.LedgerEntries.RemoveRange(ledgerEntries);

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                await LogAction("Удаление брони", $"Бронь {booking.BookingNumber} удалена");
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CheckInForm(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.Guest)
                .Include(b => b.BookingRooms)!.ThenInclude(br => br.Room)!.ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInForm(int bookingId, string identificationNumber, string passportNumber, string phone, string? registrationAddress, DateTime? birthDate)
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            if (booking.Guest != null)
            {
                if (string.IsNullOrEmpty(booking.Guest.GuestCardNumber))
                    booking.Guest.GuestCardNumber = "GC-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
                booking.Guest.IdentificationNumber = identificationNumber;
                booking.Guest.PassportNumber = passportNumber;
                booking.Guest.Phone = phone;
                booking.Guest.RegistrationAddress = registrationAddress;
                booking.Guest.BirthDate = birthDate;
            }

            booking.Status = BookingStatus.CheckedIn;

            var roomIds = new List<int> { booking.RoomId };
            if (booking.BookingRooms != null)
                roomIds.AddRange(booking.BookingRooms.Select(br => br.RoomId));

            foreach (var rid in roomIds.Distinct())
            {
                var room = await _context.Rooms.FindAsync(rid);
                if (room != null) room.Status = RoomStatus.Occupied;
            }

            _context.LedgerEntries.Add(new LedgerEntry
            {
                EntryType = LedgerEntryType.Accommodation,
                Date = DateTime.Now,
                Description = $"Заселение по брони {booking.BookingNumber}",
                Credit = 0,
                Debit = booking.CalculateTotalCost(),
                Balance = -booking.CalculateTotalCost(),
                GuestId = booking.GuestId,
                BookingId = booking.BookingId
            });

            await _context.SaveChangesAsync();
            await LogAction("Заселение", $"Бронь {booking.BookingNumber}, номер(а) {string.Join(", ", roomIds)}");
            TempData["Message"] = $"Бронь {booking.BookingNumber} — гость заселён.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.ServiceOrders)
                .Include(b => b.Payments)
                .Include(b => b.BookingRooms)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            decimal debt = booking.CalculateRemainingDebt();
            if (debt > 0)
            {
                TempData["Error"] = $"Невозможно выселить гостя. Задолженность: {debt:N2} €. Примите оплату.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.CheckedOut;

            var roomIds = new List<int> { booking.RoomId };
            if (booking.BookingRooms != null)
                roomIds.AddRange(booking.BookingRooms.Select(br => br.RoomId));

            var roomsToClean = new List<Room>();
            foreach (var rid in roomIds.Distinct())
            {
                var room = await _context.Rooms.FindAsync(rid);
                if (room != null)
                {
                    room.Status = RoomStatus.Cleaning;
                    roomsToClean.Add(room);
                }
            }

            foreach (var room in roomsToClean)
            {
                _context.CleaningTasks.Add(new CleaningTask
                {
                    RoomId = room.RoomId,
                    CleaningType = CleaningType.Departure,
                    Status = CleaningTaskStatus.Assigned,
                    Notes = $"Выездная уборка после гостя по брони {booking.BookingNumber}",
                    AssignedTo = null,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await LogAction("Выселение", $"Бронь {booking.BookingNumber}, номера: {string.Join(", ", roomsToClean.Select(r => r.RoomNumber))}");

            TempData["Message"] = $"Бронь {booking.BookingNumber} — гость выселен. Создано заданий на уборку: {roomsToClean.Count}.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public IActionResult ExportExcel([FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportBookingsToExcel();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Бронирования.xlsx");
        }

        [Authorize]
        public IActionResult ExportWord(int id, [FromServices] ExportService exportService)
        {
            var bytes = exportService.ExportBookingToWord(id);
            if (bytes == null) return NotFound();
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Счёт_{id}.docx");
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

        private async Task<List<Guest>> GetGuestsExcludingStaff()
        {
            var staffEmails = new HashSet<string>();
            foreach (var role in new[] { "Admin", "Receptionist", "Housekeeper" })
            {
                var users = await _userManager.GetUsersInRoleAsync(role);
                foreach (var u in users)
                    if (!string.IsNullOrEmpty(u.Email)) staffEmails.Add(u.Email);
            }
            return await _context.Guests.Where(g => g.Email == null || !staffEmails.Contains(g.Email)).ToListAsync();
        }
    }
}
