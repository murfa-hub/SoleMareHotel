using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

namespace SoleMareHotel.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly HotelDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, HotelDbContext context, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Введите email и пароль.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Попытка входа с несуществующим email: {Email}", email);
                ViewBag.Error = "Пользователь не найден.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, false, true);
            if (result.Succeeded)
            {
                _logger.LogInformation("Пользователь {Email} успешно вошёл в систему", email);
                if (await _userManager.IsInRoleAsync(user, "Guest"))
                    return RedirectToAction("Catalog", "Rooms");
                else if (await _userManager.IsInRoleAsync(user, "Housekeeper"))
                    return RedirectToAction("Tasks", "Housekeeper");
                else
                    return RedirectToAction("Index", "Bookings");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Аккаунт {Email} заблокирован", email);
                ViewBag.Error = "Аккаунт заблокирован. Попробуйте через 15 минут.";
                return View();
            }

            _logger.LogWarning("Неверный пароль для пользователя {Email}", email);
            ViewBag.Error = "Неверный пароль.";
            return View();
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string firstName, string lastName, string? middleName, string phone, DateTime? birthDate)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ViewBag.Error = "Пароль должен содержать минимум 8 символов.";
                return View();
            }
            if (!password.Any(char.IsDigit))
            {
                ViewBag.Error = "Пароль должен содержать хотя бы одну цифру.";
                return View();
            }
            if (!password.Any(char.IsLower))
            {
                ViewBag.Error = "Пароль должен содержать хотя бы одну строчную букву.";
                return View();
            }
            if (!password.Any(char.IsUpper))
            {
                ViewBag.Error = "Пароль должен содержать хотя бы одну заглавную букву.";
                return View();
            }
            if (password != confirmPassword)
            {
                ViewBag.Error = "Пароли не совпадают.";
                return View();
            }
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                ViewBag.Error = "Email, имя и фамилия обязательны.";
                return View();
            }
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ViewBag.Error = "Пользователь с таким email уже существует.";
                return View();
            }

            var fullName = !string.IsNullOrEmpty(middleName) ? $"{lastName} {firstName} {middleName}" : $"{lastName} {firstName}";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName,
                Role = "Guest"
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Guest");

                var guest = new Guest
                {
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    BirthDate = birthDate,
                    GuestType = "Физическое лицо",
                    GuestCardNumber = "GC-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper()
                };
                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Catalog", "Rooms");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile(string? tab)
        {
            var email = User.Identity?.Name ?? "";
            var appUser = await _userManager.FindByEmailAsync(email);
            ViewBag.User = appUser;
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == email);

            if (guest == null && appUser != null)
            {
                guest = await _context.BookingRequests
                    .Include(r => r.Guest)
                    .Where(r => r.Guest != null && r.Guest.Email == email)
                    .Select(r => r.Guest!)
                    .FirstOrDefaultAsync();
            }
            if (guest == null && appUser != null)
            {
                guest = await _context.Bookings
                    .Include(b => b.Guest)
                    .Where(b => b.Guest != null && b.Guest.Email == email)
                    .Select(b => b.Guest!)
                    .FirstOrDefaultAsync();
            }

            if (guest == null && appUser != null)
            {
                var isStaff = await _userManager.IsInRoleAsync(appUser, "Admin") ||
                              await _userManager.IsInRoleAsync(appUser, "Receptionist") ||
                              await _userManager.IsInRoleAsync(appUser, "Housekeeper");
                if (!isStaff)
                {
                    guest = new Guest
                    {
                        FullName = appUser.FullName,
                        Email = email,
                        GuestType = "Физическое лицо",
                        GuestCardNumber = "GC-" + DateTime.Now.Year + "-" + Guid.NewGuid().ToString("N")[..8].ToUpper()
                    };
                    _context.Guests.Add(guest);
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.Guest = guest;
            ViewBag.ActiveTab = tab ?? "profile";

#pragma warning disable CS8602
            if (guest != null && tab == "bookings")
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Room).ThenInclude(r => r.Category)
                    .Where(b => b.GuestId == guest.GuestId)
                    .OrderByDescending(b => b.CheckIn)
                    .ToListAsync();
                ViewBag.Bookings = bookings;
            }
            if (guest != null && tab == "requests")
            {
                var requests = await _context.BookingRequests
                    .Include(r => r.Room).ThenInclude(r => r.Category)
                    .Where(r => r.GuestId == guest.GuestId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                ViewBag.Requests = requests;
            }
#pragma warning restore CS8602

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string? firstName, string? lastName, string? middleName, string? phone, string? tab)
        {
            var email = User.Identity?.Name ?? "";
            var user = await _userManager.FindByEmailAsync(email);
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == email);

            var fName = firstName ?? "";
            var lName = lastName ?? "";
            var mName = middleName ?? "";

            var fullName = !string.IsNullOrEmpty(mName) ? $"{lName} {fName} {mName}" : $"{lName} {fName}";

            if (!string.IsNullOrEmpty(fName) || !string.IsNullOrEmpty(lName))
            {
                if (user != null)
                {
                    user.FirstName = fName;
                    user.LastName = lName;
                    user.FullName = fullName;
                    await _userManager.UpdateAsync(user);
                }
                if (guest != null)
                {
                    guest.FullName = fullName;
                    guest.Phone = phone;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["ProfileMessage"] = "Данные обновлены!";
            return RedirectToAction("Profile", new { tab = tab ?? "profile" });
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();
    }
}