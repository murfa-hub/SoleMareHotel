using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;

#pragma warning disable CS8602

namespace SoleMareHotel.Controllers
{
    public class HousekeeperController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HousekeeperController> _logger;

        public HousekeeperController(HotelDbContext context, IWebHostEnvironment environment, UserManager<ApplicationUser> userManager, ILogger<HousekeeperController> logger)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _logger = logger;
        }

        // ==================== ГОРНИЧНАЯ ====================

        [Authorize(Roles = "Housekeeper")]
        public async Task<IActionResult> Tasks()
        {
            var userName = User.Identity?.Name ?? "";
            var tasks = await _context.CleaningTasks
                .Include(t => t.Room)!.ThenInclude(r => r.Category)
                .Where(t => t.AssignedTo == userName)
                .Where(t => t.Status != CleaningTaskStatus.Completed)
                .OrderBy(t => t.CleaningType).ThenBy(t => t.Room!.RoomNumber)
                .ToListAsync();
            return View(tasks);
        }

        [HttpPost]
        [Authorize(Roles = "Housekeeper")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTask(int taskId)
        {
            var task = await _context.CleaningTasks.FindAsync(taskId);
            if (task == null) return NotFound();
            if (task.AssignedTo != User.Identity?.Name) return Forbid();
            task.Status = CleaningTaskStatus.InProgress;

            var room = await _context.Rooms.FindAsync(task.RoomId);
            if (room != null && room.Status != RoomStatus.Cleaning)
            {
                room.Status = RoomStatus.Cleaning;
            }

            _context.CleaningJournals.Add(new CleaningJournal
            {
                RoomId = task.RoomId,
                HousekeeperName = User.Identity?.Name ?? "",
                EntryTime = DateTime.Now,
                CleaningType = task.CleaningType,
                Notes = "Начало уборки"
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Tasks");
        }

        [Authorize(Roles = "Housekeeper")]
        public async Task<IActionResult> CompleteTask(int taskId)
        {
            var task = await _context.CleaningTasks
                .Include(t => t.Room)!.ThenInclude(r => r.Category)
                .FirstOrDefaultAsync(t => t.CleaningTaskId == taskId);
            if (task == null) return NotFound();
            ViewBag.Inventory = await _context.RoomInventories.Where(i => i.RoomId == task.RoomId).ToListAsync();
            return View(task);
        }

        [HttpPost]
        [Authorize(Roles = "Housekeeper")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTask(int taskId, bool inventoryOk, string? issueDescription, bool needsRepair, string? repairDescription, IFormFile? photo)
        {
            var task = await _context.CleaningTasks.FindAsync(taskId);
            if (task == null) return NotFound();
            if (task.Status != CleaningTaskStatus.InProgress)
            {
                TempData["Error"] = "Это задание уже выполнено.";
                return RedirectToAction("Tasks");
            }

            string? photoPath = null;
            if (photo != null && photo.Length > 0)
            {
                var ext = Path.GetExtension(photo.FileName);
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext.ToLowerInvariant()))
                {
                    TempData["Error"] = "Допустимые форматы фото: .jpg, .jpeg, .png, .gif, .webp";
                    return RedirectToAction("Tasks");
                }
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "damage");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) await photo.CopyToAsync(stream);
                photoPath = "/uploads/damage/" + fileName;
            }

            var report = new CleaningReport
            {
                CleaningTaskId = taskId,
                RoomId = task.RoomId,
                ReportedBy = User.Identity?.Name ?? "",
                ReportDate = DateTime.Now,
                InventoryOk = inventoryOk,
                IssueDescription = issueDescription,
                NeedsRepair = needsRepair,
                RepairDescription = repairDescription,
                PhotoPath = photoPath,
                Status = CleaningReportStatus.Pending
            };

            task.Status = CleaningTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
            task.Result = !inventoryOk ? CleaningResult.Damage : needsRepair ? CleaningResult.Malfunction : CleaningResult.Success;

            var room = await _context.Rooms.FindAsync(task.RoomId);

            if (inventoryOk && !needsRepair && room != null)
            {
                if (task.CleaningType == CleaningType.Departure)
                {
                    room.Status = RoomStatus.Free;
                    task.Notes = (task.Notes ?? "") + " | Номер переведён в Свободен";
                }
                else if (task.CleaningType == CleaningType.Intermediate)
                {
                    room.Status = RoomStatus.Occupied;
                    task.Notes = (task.Notes ?? "") + " | Промежуточная уборка завершена";
                }
            }
            if (needsRepair && room != null) room.Status = RoomStatus.UnderRepair;

            _context.CleaningReports.Add(report);

            var currentUser = User.Identity?.Name ?? "";
            var lastJournal = await _context.CleaningJournals
                .Where(cj => cj.RoomId == task.RoomId && cj.HousekeeperName == currentUser)
                .OrderByDescending(cj => cj.EntryTime)
                .FirstOrDefaultAsync();
            if (lastJournal != null && lastJournal.ExitTime == null)
            {
                lastJournal.ExitTime = DateTime.Now;
                lastJournal.Result = task.Result == CleaningResult.Success ? "Успешно" : task.Result == CleaningResult.Damage ? "Повреждения" : "Неисправность";
                lastJournal.DamageFound = !inventoryOk;
                lastJournal.DamageDescription = issueDescription;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Уборка завершена. Номер готов к заселению.";
            return RedirectToAction("Tasks");
        }

        // ==================== АДМИНИСТРАТОР ====================

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> AllTasks()
        {
            var tasks = await _context.CleaningTasks.Include(t => t.Room).ThenInclude(r => r.Category)
                .OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(tasks);
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CreateTask()
        {
            var activeTaskRoomIds = await _context.CleaningTasks
                .Where(t => t.Status != CleaningTaskStatus.Completed)
                .Select(t => t.RoomId)
                .Distinct()
                .ToListAsync();

            ViewBag.Rooms = await _context.Rooms
                .Where(r => (r.Status == RoomStatus.Cleaning || r.Status == RoomStatus.Occupied) && !activeTaskRoomIds.Contains(r.RoomId))
                .Include(r => r.Category).OrderBy(r => r.RoomNumber).ToListAsync();
            ViewBag.Housekeepers = await GetHousekeepers();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(int roomId, string cleaningType, string? notes, string assignedTo)
        {
            if (!Enum.TryParse<CleaningType>(cleaningType, out var parsedType))
            {
                TempData["Error"] = "Неверный тип уборки.";
                return RedirectToAction("AllTasks");
            }

            var hasActiveTask = await _context.CleaningTasks.AnyAsync(t =>
                t.RoomId == roomId && t.Status != CleaningTaskStatus.Completed);
            if (hasActiveTask)
            {
                TempData["Error"] = "Для этого номера уже есть активное задание на уборку.";
                return RedirectToAction("AllTasks");
            }

            _context.CleaningTasks.Add(new CleaningTask
            {
                RoomId = roomId,
                CleaningType = parsedType,
                Status = CleaningTaskStatus.Assigned,
                Notes = notes,
                AssignedTo = assignedTo,
                CreatedAt = DateTime.Now
            });

            if (parsedType == CleaningType.Departure)
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room != null && room.Status != RoomStatus.Cleaning)
                {
                    room.Status = RoomStatus.Cleaning;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Задание создано.";
            return RedirectToAction("AllTasks");
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Inventory(int roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();
            ViewBag.Room = room;
            return View(await _context.RoomInventories.Where(i => i.RoomId == roomId).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInventoryItem(int roomId, string itemName, int quantity, string? category)
        {
            _context.RoomInventories.Add(new RoomInventory { RoomId = roomId, ItemName = itemName, ExpectedQuantity = quantity, Category = category });
            await _context.SaveChangesAsync();
            return RedirectToAction("Inventory", new { roomId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInventoryItem(int itemId, int roomId)
        {
            var item = await _context.RoomInventories.FindAsync(itemId);
            if (item != null) { _context.RoomInventories.Remove(item); await _context.SaveChangesAsync(); }
            return RedirectToAction("Inventory", new { roomId });
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Reports()
        {
            var reports = await _context.CleaningReports
                .Include(r => r.CleaningTask)!.ThenInclude(t => t.Room)
                .Include(r => r.Room).OrderByDescending(r => r.ReportDate).ToListAsync();
            return View(reports);
        }

        // ==================== ГРАФИК ====================

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Schedule()
        {
            var schedule = await _context.CleaningSchedules.FirstOrDefaultAsync();
            if (schedule == null)
            {
                schedule = new CleaningSchedule { IntervalDays = 3, IsActive = true };
                _context.CleaningSchedules.Add(schedule);
                await _context.SaveChangesAsync();
            }

            var occupiedRooms = await _context.Rooms.Where(r => r.Status == RoomStatus.Occupied).Include(r => r.Category).ToListAsync();
            var lastCleaning = await _context.CleaningTasks
                .Where(t => t.CleaningType == CleaningType.Intermediate && t.Status == CleaningTaskStatus.Completed)
                .GroupBy(t => t.RoomId)
                .Select(g => new { RoomId = g.Key, LastCleaning = g.Max(t => t.CompletedAt) })
                .ToListAsync();

            var roomsDue = new List<Room>();
            foreach (var room in occupiedRooms)
            {
                var last = lastCleaning.FirstOrDefault(l => l.RoomId == room.RoomId);
                if (last == null || (DateTime.Now - last.LastCleaning!.Value).TotalDays >= schedule.IntervalDays)
                    roomsDue.Add(room);
            }

            ViewBag.Schedule = schedule;
            ViewBag.RoomsDue = roomsDue;
            ViewBag.Housekeepers = await GetHousekeepers();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Receptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateScheduleTasks(string assignedTo)
        {
            var schedule = await _context.CleaningSchedules.FirstOrDefaultAsync();
            if (schedule == null) return NotFound();

            var occupiedRooms = await _context.Rooms.Where(r => r.Status == RoomStatus.Occupied).ToListAsync();
            var lastCleaning = await _context.CleaningTasks
                .Where(t => t.CleaningType == CleaningType.Intermediate && t.Status == CleaningTaskStatus.Completed)
                .GroupBy(t => t.RoomId)
                .Select(g => new { RoomId = g.Key, LastCleaning = g.Max(t => t.CompletedAt) })
                .ToListAsync();

            int count = 0;
            foreach (var room in occupiedRooms)
            {
                var hasActiveTask = await _context.CleaningTasks.AnyAsync(t =>
                    t.RoomId == room.RoomId && t.Status != CleaningTaskStatus.Completed);
                if (hasActiveTask) continue;

                var last = lastCleaning.FirstOrDefault(l => l.RoomId == room.RoomId);
                if (last == null || (DateTime.Now - last.LastCleaning!.Value).TotalDays >= schedule.IntervalDays)
                {
                    _context.CleaningTasks.Add(new CleaningTask
                    {
                        RoomId = room.RoomId,
                        CleaningType = CleaningType.Intermediate,
                Status = CleaningTaskStatus.Assigned,
                        AssignedTo = assignedTo,
                        Notes = "Плановая уборка по графику",
                        CreatedAt = DateTime.Now
                    });
                    count++;
                }
            }

            schedule.LastRun = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Создано {count} заданий.";
            return RedirectToAction("AllTasks");
        }

        // ==================== ЖУРНАЛ ====================

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Journal(DateTime? dateFrom, DateTime? dateTo, string? housekeeper)
        {
            var tasks = _context.CleaningTasks.Include(t => t.Room).ThenInclude(r => r.Category)
                .Where(t => t.Status == CleaningTaskStatus.Completed).AsQueryable();

            if (dateFrom != null) tasks = tasks.Where(t => t.CompletedAt >= dateFrom);
            if (dateTo != null) tasks = tasks.Where(t => t.CompletedAt <= dateTo);
            if (!string.IsNullOrEmpty(housekeeper)) tasks = tasks.Where(t => t.AssignedTo == housekeeper);

            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.SelectedHousekeeper = housekeeper;
            ViewBag.Housekeepers = await GetHousekeepers();

            return View(await tasks.OrderByDescending(t => t.CompletedAt).ToListAsync());
        }

        // ==================== СТАТИСТИКА ====================

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> CleaningJournal(DateTime? dateFrom, DateTime? dateTo, string? housekeeper)
        {
            var journal = _context.CleaningJournals.Include(cj => cj.Room).AsQueryable();

            if (dateFrom != null) journal = journal.Where(j => j.EntryTime >= dateFrom);
            if (dateTo != null) journal = journal.Where(j => j.EntryTime <= dateTo);
            if (!string.IsNullOrEmpty(housekeeper)) journal = journal.Where(j => j.HousekeeperName == housekeeper);

            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.SelectedHousekeeper = housekeeper;
            ViewBag.Housekeepers = await GetHousekeepers();

            return View(await journal.OrderByDescending(j => j.EntryTime).ToListAsync());
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Stats(DateTime? dateFrom, DateTime? dateTo)
        {
            var from = dateFrom ?? DateTime.Today.AddDays(-30);
            var to = dateTo ?? DateTime.Today;

            ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
            ViewBag.DateTo = to.ToString("yyyy-MM-dd");

            var tasks = await _context.CleaningTasks
                .Include(t => t.Room)
                .Where(t => t.Status == CleaningTaskStatus.Completed)
                .Where(t => t.CompletedAt >= from && t.CompletedAt <= to)
                .ToListAsync();

            var stats = tasks
                .GroupBy(t => t.AssignedTo ?? "Неизвестно")
                .Select(g => new
                {
                    Housekeeper = g.Key,
                    Total = g.Count(),
                    Departure = g.Count(t => t.CleaningType == CleaningType.Departure),
                    Intermediate = g.Count(t => t.CleaningType == CleaningType.Intermediate),
                    Unscheduled = g.Count(t => t.CleaningType == CleaningType.Unscheduled),
                    Issues = g.Count(t => t.Result == CleaningResult.Damage || t.Result == CleaningResult.Malfunction)
                })
                .OrderByDescending(s => s.Total)
                .ToList();

            ViewBag.Stats = stats;
            ViewBag.TotalTasks = tasks.Count;
            ViewBag.TotalDeparture = tasks.Count(t => t.CleaningType == CleaningType.Departure);
            ViewBag.TotalIntermediate = tasks.Count(t => t.CleaningType == CleaningType.Intermediate);
            ViewBag.TotalUnscheduled = tasks.Count(t => t.CleaningType == CleaningType.Unscheduled);

            return View();
        }

        // ==================== ЧЕК-ЛИСТ ИНВЕНТАРИЗАЦИИ ====================

        [Authorize(Roles = "Housekeeper")]
        public async Task<IActionResult> Checklist(int roomId, int? taskId)
        {
            var room = await _context.Rooms.Include(r => r.Category).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null) return NotFound();

            var inventory = await _context.RoomInventories
                .Where(i => i.RoomId == roomId)
                .OrderBy(i => i.Category).ThenBy(i => i.ItemName)
                .ToListAsync();

            ViewBag.Room = room;
            ViewBag.TaskId = taskId;
            return View(inventory);
        }

        [HttpPost]
        [Authorize(Roles = "Housekeeper")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checklist(int roomId, int? taskId, string[] itemNames, int[] expectedQty, int[] actualQty, bool[] itemOk, string? notes)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            var allOk = itemOk.All(x => x);

            var checklist = new RoomChecklist
            {
                RoomId = roomId,
                HousekeeperName = User.Identity?.Name ?? "",
                CheckDate = DateTime.Now,
                AllItemsOk = allOk,
                Notes = notes,
                CleaningTaskId = taskId
            };

            var inventory = await _context.RoomInventories.Where(i => i.RoomId == roomId).ToListAsync();

            for (int i = 0; i < itemNames.Length; i++)
            {
                var invItem = inventory.FirstOrDefault(x => x.ItemName == itemNames[i]);
                checklist.Items.Add(new RoomChecklistItem
                {
                    ItemName = itemNames[i],
                    Category = invItem?.Category,
                    ExpectedQuantity = i < expectedQty.Length ? expectedQty[i] : 1,
                    ActualQuantity = i < actualQty.Length ? actualQty[i] : 0,
                    IsOk = i < itemOk.Length ? itemOk[i] : true,
                    Note = null
                });
            }

            _context.RoomChecklists.Add(checklist);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Чек-лист инвентаризации сохранён.";
            if (taskId.HasValue)
                return RedirectToAction("Tasks");
            return RedirectToAction("AllTasks");
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Checklists(int? roomId, string? housekeeper)
        {
            var query = _context.RoomChecklists
                .Include(rc => rc.Room)
                .Include(rc => rc.Items)
                .AsQueryable();

            if (roomId != null) query = query.Where(rc => rc.RoomId == roomId);
            if (!string.IsNullOrEmpty(housekeeper)) query = query.Where(rc => rc.HousekeeperName == housekeeper);

            ViewBag.Rooms = await _context.Rooms.OrderBy(r => r.RoomNumber).ToListAsync();
            ViewBag.Housekeepers = await GetHousekeepers();
            ViewBag.SelectedRoom = roomId;
            ViewBag.SelectedHousekeeper = housekeeper;

            return View(await query.OrderByDescending(rc => rc.CheckDate).ToListAsync());
        }

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> ChecklistDetails(int id)
        {
            var checklist = await _context.RoomChecklists
                .Include(rc => rc.Room)
                .Include(rc => rc.Items)
                .Include(rc => rc.CleaningTask)
                .FirstOrDefaultAsync(rc => rc.RoomChecklistId == id);

            if (checklist == null) return NotFound();
            return View(checklist);
        }

        private async Task<List<ApplicationUser>> GetHousekeepers()
        {
            return (await _userManager.GetUsersInRoleAsync("Housekeeper")).ToList();
        }
    }
}