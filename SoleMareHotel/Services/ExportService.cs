using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8602
using SoleMareHotel.Models;

namespace SoleMareHotel.Services
{
    public class ExportService
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<ExportService> _logger;

        public ExportService(HotelDbContext context, ILogger<ExportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== ЭКСПОРТ В EXCEL (ОСНОВНЫЕ ТАБЛИЦЫ) ====================

        public byte[] ExportBookingsToExcel()
        {
            var bookings = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Бронирования");

                worksheet.Cell(1, 1).Value = "№ брони";
                worksheet.Cell(1, 2).Value = "Гость";
                worksheet.Cell(1, 3).Value = "Номер";
                worksheet.Cell(1, 4).Value = "Категория";
                worksheet.Cell(1, 5).Value = "Заезд";
                worksheet.Cell(1, 6).Value = "Выезд";
                worksheet.Cell(1, 7).Value = "Гостей";
                worksheet.Cell(1, 8).Value = "Статус";
                worksheet.Cell(1, 9).Value = "Стоимость (€)";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                headerRow.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < bookings.Count; i++)
                {
                    var b = bookings[i];
                    int row = i + 2;
                    worksheet.Cell(row, 1).Value = b.BookingNumber;
                    worksheet.Cell(row, 2).Value = b.Guest?.FullName ?? b.Guest?.OrganizationName ?? b.Guest?.Email ?? "—";
                    worksheet.Cell(row, 3).Value = b.Room?.RoomNumber;
                    worksheet.Cell(row, 4).Value = b.Room?.Category?.Name;
                    worksheet.Cell(row, 5).Value = b.CheckIn.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 6).Value = b.CheckOut.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 7).Value = b.NumberOfGuests;
                    worksheet.Cell(row, 8).Value = b.Status.GetDisplayName();
                    worksheet.Cell(row, 9).Value = b.CalculateTotalCost();
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportGuestsToExcel()
        {
            var guests = _context.Guests.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Гости");

                worksheet.Cell(1, 1).Value = "Тип";
                worksheet.Cell(1, 2).Value = "ФИО / Организация";
                worksheet.Cell(1, 3).Value = "Телефон";
                worksheet.Cell(1, 4).Value = "Паспорт";
                worksheet.Cell(1, 5).Value = "Дата рождения";
                worksheet.Cell(1, 6).Value = "Email";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                headerRow.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < guests.Count; i++)
                {
                    var g = guests[i];
                    int row = i + 2;
                    worksheet.Cell(row, 1).Value = g.GuestType;
                    worksheet.Cell(row, 2).Value = g.FullName ?? g.OrganizationName ?? g.Email ?? "—";
                    worksheet.Cell(row, 3).Value = g.Phone ?? g.ContactPersonPhone ?? "—";
                    var passport = $"{g.IdentificationNumber} {g.PassportNumber}".Trim();
                    worksheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(passport) ? "—" : passport;
                    worksheet.Cell(row, 5).Value = g.BirthDate?.ToString("dd.MM.yyyy") ?? "—";
                    worksheet.Cell(row, 6).Value = g.ContactPersonEmail ?? g.Email ?? "—";
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportRoomsToExcel()
        {
            var rooms = _context.Rooms.Include(r => r.Category).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Номера");

                worksheet.Cell(1, 1).Value = "Номер";
                worksheet.Cell(1, 2).Value = "Категория";
                worksheet.Cell(1, 3).Value = "Этаж";
                worksheet.Cell(1, 4).Value = "Статус";
                worksheet.Cell(1, 5).Value = "Вместимость";
                worksheet.Cell(1, 6).Value = "Цена/сутки (€)";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                headerRow.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < rooms.Count; i++)
                {
                    var r = rooms[i];
                    int row = i + 2;
                    worksheet.Cell(row, 1).Value = r.RoomNumber;
                    worksheet.Cell(row, 2).Value = r.Category?.Name;
                    worksheet.Cell(row, 3).Value = r.Floor;
                    worksheet.Cell(row, 4).Value = r.Status.GetDisplayName();
                    worksheet.Cell(row, 5).Value = r.Capacity;
                    worksheet.Cell(row, 6).Value = r.PricePerNight;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // ==================== ЭКСПОРТ В WORD ====================

        public byte[] ExportBookingToWord(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .FirstOrDefault(b => b.BookingId == bookingId);

            if (booking == null) return null!;

            using (var stream = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    var title = new Paragraph(new Run(new Text("СЧЁТ НА ОПЛАТУ")));
                    title.ParagraphProperties = new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center },
                        new SpacingBetweenLines() { After = "200" }
                    );
                    body.AppendChild(title);

                    body.AppendChild(CreateParagraph("Отель «Sole Mare»", true, 16));
                    body.AppendChild(CreateParagraph("Дата создания: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")));
                    body.AppendChild(CreateParagraph(""));

                    body.AppendChild(CreateParagraph($"Номер брони: {booking.BookingNumber}", true));
                    body.AppendChild(CreateParagraph($"Гость: {booking.Guest?.FullName ?? booking.Guest?.OrganizationName ?? booking.Guest?.Email ?? "Гость"}"));
                    body.AppendChild(CreateParagraph($"Номер: {booking.Room?.RoomNumber} ({booking.Room?.Category?.Name})"));
                    body.AppendChild(CreateParagraph($"Дата заезда: {booking.CheckIn:dd.MM.yyyy HH:mm}"));
                    body.AppendChild(CreateParagraph($"Дата выезда: {booking.CheckOut:dd.MM.yyyy HH:mm}"));
                    body.AppendChild(CreateParagraph($"Количество гостей: {booking.NumberOfGuests}"));
                    body.AppendChild(CreateParagraph($"Статус: {booking.Status}"));
                    body.AppendChild(CreateParagraph(""));

                    var nights = (booking.CheckOut - booking.CheckIn).Days;
                    if (nights <= 0) nights = 1;
                    body.AppendChild(CreateParagraph("ПРОЖИВАНИЕ:", true, 14));
                    body.AppendChild(CreateParagraph($"Количество ночей: {nights}"));
                    body.AppendChild(CreateParagraph($"Цена за сутки: {booking.Room?.PricePerNight:N2} €"));
                    body.AppendChild(CreateParagraph($"Стоимость проживания: {booking.CalculateTotalCost():N2} €"));
                    body.AppendChild(CreateParagraph(""));

                    if (booking.ServiceOrders != null && booking.ServiceOrders.Any())
                    {
                        body.AppendChild(CreateParagraph("ДОПОЛНИТЕЛЬНЫЕ УСЛУГИ:", true, 14));
                        foreach (var order in booking.ServiceOrders)
                        {
                            body.AppendChild(CreateParagraph($"{order.Service?.Name} × {order.Quantity} = {order.TotalPrice:N2} €"));
                        }
                        body.AppendChild(CreateParagraph($"Итого услуги: {booking.ServiceOrders.Sum(o => o.TotalPrice):N2} €"));
                        body.AppendChild(CreateParagraph(""));
                    }

                    body.AppendChild(CreateParagraph($"ИТОГО К ОПЛАТЕ: {booking.CalculateFullCost():N2} €", true, 18));
                    body.AppendChild(CreateParagraph(""));
                    body.AppendChild(CreateParagraph("Подпись администратора: ________________"));

                    mainPart.Document.Save();
                }
                return stream.ToArray();
            }
        }

        public byte[] ExportGuestsListToWord()
        {
            var guests = _context.Guests.ToList();

            using (var stream = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    body.AppendChild(CreateParagraph("КАРТОТЕКА ГОСТЕЙ", true, 20));
                    body.AppendChild(CreateParagraph("Отель «Sole Mare»"));
                    body.AppendChild(CreateParagraph($"Дата выгрузки: {DateTime.Now:dd.MM.yyyy}"));
                    body.AppendChild(CreateParagraph(""));

                    foreach (var g in guests)
                    {
                        body.AppendChild(CreateParagraph($"{g.FullName ?? g.OrganizationName ?? g.Email ?? "Гость"}", true, 14));
                        body.AppendChild(CreateParagraph($"Тип: {g.GuestType}"));
                        if (!string.IsNullOrEmpty(g.Phone))
                            body.AppendChild(CreateParagraph($"Телефон: {g.Phone}"));
                        if (g.BirthDate != null)
                            body.AppendChild(CreateParagraph($"Дата рождения: {g.BirthDate:dd.MM.yyyy}"));
                        body.AppendChild(CreateParagraph("---"));
                    }

                    mainPart.Document.Save();
                }
                return stream.ToArray();
            }
        }

        // ==================== ЭКСПОРТ ОТЧЁТОВ ====================

        public byte[] ExportRevenueToExcel(DateTime startDate, DateTime endDate)
        {
            var bookings = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Where(b => b.CheckIn >= startDate && b.CheckOut <= endDate)
                .Where(b => b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.CheckedIn)
                .ToList();

            decimal totalRevenue = 0;
            foreach (var b in bookings) totalRevenue += b.CalculateFullCost();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Выручка");

                ws.Cell(1, 1).Value = "Отель «Sole Mare»";
                ws.Cell(2, 1).Value = $"Отчёт по выручке за период: {startDate:dd.MM.yyyy} — {endDate:dd.MM.yyyy}";
                ws.Cell(3, 1).Value = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                ws.Cell(4, 1).Value = $"Всего бронирований: {bookings.Count}";
                ws.Cell(5, 1).Value = $"Общая выручка: {totalRevenue:N2} €";

                ws.Cell(7, 1).Value = "№ брони";
                ws.Cell(7, 2).Value = "Гость";
                ws.Cell(7, 3).Value = "Номер";
                ws.Cell(7, 4).Value = "Заезд";
                ws.Cell(7, 5).Value = "Выезд";
                ws.Cell(7, 6).Value = "Проживание (€)";
                ws.Cell(7, 7).Value = "Услуги (€)";
                ws.Cell(7, 8).Value = "Итого (€)";

                var headerRow = ws.Row(7);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                headerRow.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < bookings.Count; i++)
                {
                    var b = bookings[i];
                    int row = i + 8;
                    decimal roomCost = b.CalculateTotalCost();
                    decimal serviceCost = b.ServiceOrders?.Sum(o => o.TotalPrice) ?? 0;

                    ws.Cell(row, 1).Value = b.BookingNumber;
                    ws.Cell(row, 2).Value = b.Guest?.FullName ?? b.Guest?.OrganizationName ?? b.Guest?.Email ?? "—";
                    ws.Cell(row, 3).Value = b.Room?.RoomNumber;
                    ws.Cell(row, 4).Value = b.CheckIn.ToString("dd.MM.yyyy");
                    ws.Cell(row, 5).Value = b.CheckOut.ToString("dd.MM.yyyy");
                    ws.Cell(row, 6).Value = roomCost;
                    ws.Cell(row, 7).Value = serviceCost;
                    ws.Cell(row, 8).Value = roomCost + serviceCost;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportUnpopularRoomsToExcel()
        {
            var rooms = _context.Rooms
                .Include(r => r.Category)
                .Include(r => r.Bookings)
                .OrderBy(r => r.Bookings!.Count)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Непопулярные номера");

                ws.Cell(1, 1).Value = "Отель «Sole Mare»";
                ws.Cell(2, 1).Value = "Отчёт: Непопулярные номера";
                ws.Cell(3, 1).Value = $"Дата: {DateTime.Now:dd.MM.yyyy}";

                ws.Cell(5, 1).Value = "Номер";
                ws.Cell(5, 2).Value = "Категория";
                ws.Cell(5, 3).Value = "Статус";
                ws.Cell(5, 4).Value = "Цена/сутки (€)";
                ws.Cell(5, 5).Value = "Кол-во броней";

                var hr = ws.Row(5);
                hr.Style.Font.Bold = true;
                hr.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                hr.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < rooms.Count; i++)
                {
                    int row = i + 6;
                    ws.Cell(row, 1).Value = rooms[i].RoomNumber;
                    ws.Cell(row, 2).Value = rooms[i].Category?.Name;
                    ws.Cell(row, 3).Value = rooms[i].Status.GetDisplayName();
                    ws.Cell(row, 4).Value = rooms[i].PricePerNight;
                    ws.Cell(row, 5).Value = rooms[i].Bookings?.Count ?? 0;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportOccupancyToExcel()
        {
            var totalRooms = _context.Rooms.Count();
            var occupied = _context.Rooms.Count(r => r.Status == RoomStatus.Occupied);
            var free = totalRooms - occupied;
            double rate = totalRooms > 0 ? (double)occupied / totalRooms * 100 : 0;

            var byStatus = _context.Rooms.GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() }).ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Занятость");

                ws.Cell(1, 1).Value = "Отель «Sole Mare»";
                ws.Cell(2, 1).Value = "Отчёт: Занятость номеров";
                ws.Cell(3, 1).Value = $"Дата: {DateTime.Now:dd.MM.yyyy}";

                ws.Cell(5, 1).Value = "Всего номеров:";
                ws.Cell(5, 2).Value = totalRooms;
                ws.Cell(6, 1).Value = "Занято:";
                ws.Cell(6, 2).Value = occupied;
                ws.Cell(7, 1).Value = "Свободно:";
                ws.Cell(7, 2).Value = free;
                ws.Cell(8, 1).Value = "Загруженность:";
                ws.Cell(8, 2).Value = $"{rate:F1}%";

                ws.Cell(10, 1).Value = "Статус";
                ws.Cell(10, 2).Value = "Количество";
                var hr = ws.Row(10);
                hr.Style.Font.Bold = true;
                hr.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                hr.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < byStatus.Count; i++)
                {
                    ws.Cell(i + 11, 1).Value = byStatus[i].Status.GetDisplayName();
                    ws.Cell(i + 11, 2).Value = byStatus[i].Count;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportOverdueToExcel()
        {
            var today = DateTime.Today;
            var overdue = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => b.Status == BookingStatus.CheckedIn && b.CheckOut < today)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Просроченные выезды");

                ws.Cell(1, 1).Value = "Отель «Sole Mare»";
                ws.Cell(2, 1).Value = "Отчёт: Гости, не освободившие номер вовремя";
                ws.Cell(3, 1).Value = $"Дата: {DateTime.Now:dd.MM.yyyy}";

                ws.Cell(5, 1).Value = "№ брони";
                ws.Cell(5, 2).Value = "Гость";
                ws.Cell(5, 3).Value = "Номер";
                ws.Cell(5, 4).Value = "Плановый выезд";
                ws.Cell(5, 5).Value = "Просрочка (дней)";

                var hr = ws.Row(5);
                hr.Style.Font.Bold = true;
                hr.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                hr.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < overdue.Count; i++)
                {
                    int row = i + 6;
                    var b = overdue[i];
                    ws.Cell(row, 1).Value = b.BookingNumber;
                    ws.Cell(row, 2).Value = b.Guest?.FullName ?? b.Guest?.OrganizationName ?? b.Guest?.Email ?? "—";
                    ws.Cell(row, 3).Value = b.Room?.RoomNumber;
                    ws.Cell(row, 4).Value = b.CheckOut.ToString("dd.MM.yyyy");
                    ws.Cell(row, 5).Value = (today - b.CheckOut.Date).Days;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportCategoryPopularityToExcel()
        {
            var categories = _context.RoomCategories
                .Include(c => c.Rooms!).ThenInclude(r => r.Bookings!)
                .ToList();

            var result = categories.Select(c => new
            {
                c.Name,
                RoomCount = c.Rooms!.Count,
                BookingCount = c.Rooms!.Sum(r => r.Bookings != null ? r.Bookings.Count : 0),
                TotalRevenue = c.Rooms!.Sum(r => r.Bookings != null
                    ? r.Bookings.Sum(b => {
                        var nights = (b.CheckOut - b.CheckIn).Days;
                        if (nights <= 0) nights = 1;
                        return nights * r.PricePerNight;
                    })
                    : 0)
            }).OrderByDescending(c => c.BookingCount).ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Популярность категорий");

                ws.Cell(1, 1).Value = "Отель «Sole Mare»";
                ws.Cell(2, 1).Value = "Отчёт: Популярность категорий номеров";
                ws.Cell(3, 1).Value = $"Дата: {DateTime.Now:dd.MM.yyyy}";

                ws.Cell(5, 1).Value = "Категория";
                ws.Cell(5, 2).Value = "Кол-во номеров";
                ws.Cell(5, 3).Value = "Кол-во броней";
                ws.Cell(5, 4).Value = "Выручка (€)";

                var hr = ws.Row(5);
                hr.Style.Font.Bold = true;
                hr.Style.Fill.BackgroundColor = XLColor.FromHtml("#002B5B");
                hr.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < result.Count; i++)
                {
                    int row = i + 6;
                    ws.Cell(row, 1).Value = result[i].Name;
                    ws.Cell(row, 2).Value = result[i].RoomCount;
                    ws.Cell(row, 3).Value = result[i].BookingCount;
                    ws.Cell(row, 4).Value = result[i].TotalRevenue;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportRevenueToWord(DateTime startDate, DateTime endDate)
        {
            var bookings = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.ServiceOrders)!.ThenInclude(o => o.Service)
                .Where(b => b.CheckIn >= startDate && b.CheckOut <= endDate)
                .Where(b => b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.CheckedIn)
                .ToList();

            decimal totalRevenue = 0;
            foreach (var b in bookings) totalRevenue += b.CalculateFullCost();

            using (var stream = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    body.AppendChild(CreateParagraph("ОТЧЁТ ПО ВЫРУЧКЕ", true, 20));
                    body.AppendChild(CreateParagraph("Отель «Sole Mare»", true, 14));
                    body.AppendChild(CreateParagraph($"Период: {startDate:dd.MM.yyyy} — {endDate:dd.MM.yyyy}"));
                    body.AppendChild(CreateParagraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}"));
                    body.AppendChild(CreateParagraph(""));
                    body.AppendChild(CreateParagraph($"Всего бронирований: {bookings.Count}", true));
                    body.AppendChild(CreateParagraph($"Общая выручка: {totalRevenue:N2} €", true, 16));
                    body.AppendChild(CreateParagraph(""));

                    body.AppendChild(CreateParagraph("ДЕТАЛИЗАЦИЯ:", true, 14));
                    body.AppendChild(CreateParagraph(""));

                    foreach (var b in bookings)
                    {
                        body.AppendChild(CreateParagraph($"Бронь: {b.BookingNumber}", true));
                        body.AppendChild(CreateParagraph($"Гость: {b.Guest?.FullName ?? b.Guest?.OrganizationName ?? b.Guest?.Email ?? "Гость"}"));
                        body.AppendChild(CreateParagraph($"Номер: {b.Room?.RoomNumber} | {b.CheckIn:dd.MM.yyyy} — {b.CheckOut:dd.MM.yyyy}"));
                        body.AppendChild(CreateParagraph($"Проживание: {b.CalculateTotalCost():N2} € | Услуги: {b.ServiceOrders?.Sum(o => o.TotalPrice) ?? 0:N2} €"));
                        body.AppendChild(CreateParagraph($"Итого: {b.CalculateFullCost():N2} €"));
                        body.AppendChild(CreateParagraph("---"));
                    }

                    mainPart.Document.Save();
                }
                return stream.ToArray();
            }
        }

        // ==================== ЭКСПОРТ АКТОВ ====================

        public byte[] ExportActToWord(int actId)
        {
            var act = _context.Acts
                .Include(a => a.Booking)!.ThenInclude(b => b.Room)!.ThenInclude(r => r.Category)
                .Include(a => a.Guest)
                .Include(a => a.Room)
                .FirstOrDefault(a => a.ActId == actId);

            if (act == null) return null!;

            using (var stream = new MemoryStream())
            {
                using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    if (act.ActType == "Заселение")
                    {
                        body.AppendChild(CreateParagraph("АКТ ЗАСЕЛЕНИЯ", true, 20));
                        body.AppendChild(CreateParagraph($"№ {act.ActNumber}", true, 14));
                        body.AppendChild(CreateParagraph(""));
                        body.AppendChild(CreateParagraph($"Дата составления: {act.CreatedDate:dd.MM.yyyy}"));
                        body.AppendChild(CreateParagraph($"Номер брони: {act.Booking?.BookingNumber}"));
                        body.AppendChild(CreateParagraph($"Гость: {act.Guest?.FullName ?? act.Guest?.OrganizationName ?? act.Guest?.Email ?? "Гость"}"));
                        body.AppendChild(CreateParagraph($"Паспорт: {act.Guest?.IdentificationNumber} {act.Guest?.PassportNumber}"));
                        body.AppendChild(CreateParagraph($"Номер комнаты: {act.Booking?.Room?.RoomNumber} ({act.Booking?.Room?.Category?.Name})"));
                        body.AppendChild(CreateParagraph($"Дата заезда: {act.CheckInDate:dd.MM.yyyy}"));
                        body.AppendChild(CreateParagraph($"Время заезда: {act.CheckInTime}"));
                        body.AppendChild(CreateParagraph($"Количество гостей: {act.NumberOfGuests}"));
                        body.AppendChild(CreateParagraph(""));
                        body.AppendChild(CreateParagraph("Подпись администратора: ________________", true));
                        body.AppendChild(CreateParagraph("Подпись гостя: ________________", true));
                    }
                    else if (act.ActType == "Списание номера")
                    {
                        body.AppendChild(CreateParagraph("АКТ НА СПИСАНИЕ НОМЕРА", true, 20));
                        body.AppendChild(CreateParagraph($"№ {act.ActNumber}", true, 14));
                        body.AppendChild(CreateParagraph(""));
                        body.AppendChild(CreateParagraph($"Дата составления: {act.CreatedDate:dd.MM.yyyy}"));
                        body.AppendChild(CreateParagraph($"Номер комнаты: {act.Room?.RoomNumber}"));
                        body.AppendChild(CreateParagraph($"Описание: {act.DamageDescription}"));
                        body.AppendChild(CreateParagraph(""));
                        body.AppendChild(CreateParagraph("Подпись администратора: ________________", true));
                        body.AppendChild(CreateParagraph("Подпись директора: ________________", true));
                    }

                    mainPart.Document.Save();
                }
                return stream.ToArray();
            }
        }

        private Paragraph CreateParagraph(string text, bool bold = false, int fontSize = 12)
        {
            var run = new Run(new Text(text));
            run.RunProperties = new RunProperties();
            if (bold)
                run.RunProperties.AppendChild(new Bold());
            run.RunProperties.AppendChild(new FontSize() { Val = (fontSize * 2).ToString() });

            return new Paragraph(run);
        }
    }
}