using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

#pragma warning disable CS8602

namespace SoleMareHotel.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class OrganizationsController : Controller
    {
        private readonly HotelDbContext _context;

        public OrganizationsController(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var orgs = _context.Organizations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                orgs = orgs.Where(o =>
                    o.Name.Contains(search) ||
                    (o.INN != null && o.INN.Contains(search)) ||
                    (o.ContactPersonName != null && o.ContactPersonName.Contains(search)));
            }
            ViewBag.Search = search;
            return View(await orgs.OrderBy(o => o.Name).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var org = await _context.Organizations.FindAsync(id);
            if (org == null) return NotFound();
            return View(org);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Organization organization)
        {
            if (ModelState.IsValid)
            {
                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Организация «{organization.Name}» зарегистрирована.";
                return RedirectToAction(nameof(Index));
            }
            return View(organization);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var org = await _context.Organizations.FindAsync(id);
            if (org == null) return NotFound();
            return View(org);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Organization organization)
        {
            if (id != organization.OrganizationId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(organization);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Организация «{organization.Name}» обновлена.";
                return RedirectToAction(nameof(Index));
            }
            return View(organization);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var org = await _context.Organizations.FindAsync(id);
            if (org == null) return NotFound();
            return View(org);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org != null)
            {
                _context.Organizations.Remove(org);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Организация «{org.Name}» удалена.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            var orgs = await _context.Organizations
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new { o.OrganizationId, o.Name, o.ContactPersonName, o.ContactPersonPhone, o.ContractNumber })
                .ToListAsync();
            return Json(orgs);
        }
    }
}
