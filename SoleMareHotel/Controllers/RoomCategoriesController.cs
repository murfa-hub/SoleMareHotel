using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class RoomCategoriesController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<RoomCategoriesController> _logger;

        public RoomCategoriesController(HotelDbContext context, ILogger<RoomCategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: RoomCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.RoomCategories.ToListAsync());
        }

        // GET: RoomCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var roomCategory = await _context.RoomCategories.FirstOrDefaultAsync(m => m.RoomCategoryId == id);
            if (roomCategory == null) return NotFound();

            return View(roomCategory);
        }

        // GET: RoomCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RoomCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomCategoryId,Name,Description")] RoomCategory roomCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(roomCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(roomCategory);
        }

        // GET: RoomCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var roomCategory = await _context.RoomCategories.FindAsync(id);
            if (roomCategory == null) return NotFound();

            return View(roomCategory);
        }

        // POST: RoomCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomCategoryId,Name,Description")] RoomCategory roomCategory)
        {
            if (id != roomCategory.RoomCategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(roomCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomCategoryExists(roomCategory.RoomCategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(roomCategory);
        }

        // GET: RoomCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var roomCategory = await _context.RoomCategories.FirstOrDefaultAsync(m => m.RoomCategoryId == id);
            if (roomCategory == null) return NotFound();

            return View(roomCategory);
        }

        // POST: RoomCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roomCategory = await _context.RoomCategories.FindAsync(id);
            if (roomCategory != null)
            {
                var hasRooms = await _context.Rooms.AnyAsync(r => r.RoomCategoryId == id);
                if (hasRooms)
                {
                    TempData["Error"] = "Нельзя удалить категорию — к ней привязаны номера. Сначала переместите или удалите номера.";
                    return RedirectToAction(nameof(Index));
                }
                _context.RoomCategories.Remove(roomCategory);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool RoomCategoryExists(int id)
        {
            return _context.RoomCategories.Any(e => e.RoomCategoryId == id);
        }
    }
}