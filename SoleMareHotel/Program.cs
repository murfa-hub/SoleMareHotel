using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoleMareHotel.Models;
using SoleMareHotel.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<HotelDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<ExportService>();
builder.Services.AddHostedService<AutoCancelService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<HotelDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Receptionist", "Guest", "Housekeeper" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    if (app.Environment.IsDevelopment())
    {
        await SeedUserAsync(userManager, "admin@solemare.com", "Admin123!", "Администратор отеля", "Admin");
        await SeedUserAsync(userManager, "reception@solemare.com", "Recep123!", "Менеджер ресепшн", "Receptionist");
        await SeedUserAsync(userManager, "guest@solemare.com", "Guest123!", "Тест Гость", "Guest");
        await SeedUserAsync(userManager, "housekeeper@solemare.com", "Clean123!", "Горничная", "Housekeeper");
    }

    if (!dbContext.RoomCategories.Any())
    {
        dbContext.RoomCategories.AddRange(
            new RoomCategory { Name = "Стандарт", Description = "Уютный номер эконом-класса" },
            new RoomCategory { Name = "Люкс", Description = "Просторный номер повышенного комфорта" },
            new RoomCategory { Name = "Президентский", Description = "Номер премиум-класса с панорамным видом" },
            new RoomCategory { Name = "Семейный", Description = "Просторный номер для семейного отдыха" }
        );
        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.Services.Any())
    {
        dbContext.Services.AddRange(
            new Service { Name = "Заврак в номер", Category = ServiceCategory.Food, Price = 25.00m, Description = "Континентальный заврак с доставкой в номер" },
            new Service { Name = "Ужин в ресторане", Category = ServiceCategory.Food, Price = 55.00m, Description = "Двухразовое меню в ресторане" },
            new Service { Name = "Бутылка вина", Category = ServiceCategory.Food, Price = 35.00m, Description = "Бутылка домашнего вина" },
            new Service { Name = "Уборка номера", Category = ServiceCategory.Household, Price = 15.00m, Description = "Ежедневная уборка номера" },
            new Service { Name = "Стирка белья", Category = ServiceCategory.Household, Price = 10.00m, Description = "Стирка личного белья гостя" },
            new Service { Name = "Трансфер из аэропорта", Category = ServiceCategory.Transport, Price = 40.00m, Description = "Встреча в аэропорту и доставка в отель" },
            new Service { Name = "Трансфер до аэропорта", Category = ServiceCategory.Transport, Price = 40.00m, Description = "Доставка из отеля в аэропорт" },
            new Service { Name = "Массаж (60 мин)", Category = ServiceCategory.Wellness, Price = 70.00m, Description = "Расслабляющий массаж всего тела" },
            new Service { Name = "SPA-процедуры", Category = ServiceCategory.Wellness, Price = 85.00m, Description = "Комплекс SPA-процедур" },
            new Service { Name = "Аренда велосипеда", Category = ServiceCategory.Other, Price = 12.00m, Description = "Прокат велосипеда на сутки" }
        );
        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.Rooms.Any())
    {
        var categories = await dbContext.RoomCategories.ToListAsync();
        var std = categories.First(c => c.Name == "Стандарт");
        var lux = categories.First(c => c.Name == "Люкс");
        var pres = categories.First(c => c.Name == "Президентский");
        var fam = categories.First(c => c.Name == "Семейный");

        dbContext.Rooms.AddRange(
            new Room { RoomNumber = "101", Floor = 1, Location = "Корпус А", Status = RoomStatus.Free, Capacity = 2, PricePerNight = 55.00m, RoomCategoryId = std.RoomCategoryId, Description = "Уютный номер на первом этаже с видом на сад", AdditionalAmenities = "Кондиционер, TV, Wi-Fi" },
            new Room { RoomNumber = "102", Floor = 1, Location = "Корпус А", Status = RoomStatus.Free, Capacity = 2, PricePerNight = 55.00m, RoomCategoryId = std.RoomCategoryId, Description = "Компактный номер эконом-класса", AdditionalAmenities = "TV, Wi-Fi, Minibar" },
            new Room { RoomNumber = "103", Floor = 1, Location = "Корпус А", Status = RoomStatus.Occupied, Capacity = 1, PricePerNight = 45.00m, RoomCategoryId = std.RoomCategoryId, Description = "Экономичный номер для одинокого путешественника", AdditionalAmenities = "TV, Wi-Fi" },
            new Room { RoomNumber = "104", Floor = 1, Location = "Корпус А", Status = RoomStatus.Cleaning, Capacity = 2, PricePerNight = 55.00m, RoomCategoryId = std.RoomCategoryId, Description = "Стандартный номер после выезда гостя", AdditionalAmenities = "Кондиционер, TV, Wi-Fi" },

            new Room { RoomNumber = "201", Floor = 2, Location = "Корпус А", Status = RoomStatus.Free, Capacity = 2, PricePerNight = 95.00m, RoomCategoryId = lux.RoomCategoryId, Description = "Просторный номер с панорамными окнами", AdditionalAmenities = "Кондиционер, TV, Wi-Fi, Minibar, Халат, Тапочки" },
            new Room { RoomNumber = "202", Floor = 2, Location = "Корпус А", Status = RoomStatus.Occupied, Capacity = 2, PricePerNight = 95.00m, RoomCategoryId = lux.RoomCategoryId, Description = "Номер Люкс с видом на море", AdditionalAmenities = "Кондиционер, TV, Wi-Fi, Minibar, Халат, Тапочки, Сейф" },
            new Room { RoomNumber = "203", Floor = 2, Location = "Корпус Б", Status = RoomStatus.Free, Capacity = 3, PricePerNight = 110.00m, RoomCategoryId = lux.RoomCategoryId, Description = "Улучшенный номер с гостиной зоной", AdditionalAmenities = "Кондиционер, TV, Wi-Fi, Minibar, Халат, Тапочки, Кофемашина" },

            new Room { RoomNumber = "301", Floor = 3, Location = "Корпус Б", Status = RoomStatus.Free, Capacity = 2, PricePerNight = 180.00m, RoomCategoryId = pres.RoomCategoryId, Description = "Роскошный номер с панорамным видом на море и город", AdditionalAmenities = "Кондиционер, TV 65\", Wi-Fi, Minibar, Халат, Тапочки, Сейф, Кофемашина, Джакузи" },
            new Room { RoomNumber = "302", Floor = 3, Location = "Корпус Б", Status = RoomStatus.UnderRepair, Capacity = 2, PricePerNight = 200.00m, RoomCategoryId = pres.RoomCategoryId, Description = "Президентский номер — ремонт сантехники", AdditionalAmenities = "Кондиционер, TV 65\", Wi-Fi, Minibar, Халат, Тапочки, Сейф, Джакузи, Терраса" },

            new Room { RoomNumber = "401", Floor = 4, Location = "Корпус В", Status = RoomStatus.Free, Capacity = 4, PricePerNight = 120.00m, RoomCategoryId = fam.RoomCategoryId, Description = "Просторный семейный номер с двумя спальнями", AdditionalAmenities = "Кондиционер, TV, Wi-Fi, Minibar, Детская кроватка, Ванная с ванной" },
            new Room { RoomNumber = "402", Floor = 4, Location = "Корпус В", Status = RoomStatus.Free, Capacity = 5, PricePerNight = 140.00m, RoomCategoryId = fam.RoomCategoryId, Description = "Семейный номер-люкс с детской комнатой", AdditionalAmenities = "Кондиционер, TV, Wi-Fi, Minibar, Детская кроватка, Игровая зона, Ванная с ванной" },
            new Room { RoomNumber = "403", Floor = 4, Location = "Корпус В", Status = RoomStatus.Free, Capacity = 3, PricePerNight = 100.00m, RoomCategoryId = fam.RoomCategoryId, Description = "Компактный семейный номер", AdditionalAmenities = "Кондиционер, TV, Wi-Fi, Детская кроватка" }
        );
        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.Organizations.Any())
    {
        dbContext.Organizations.AddRange(
            new Organization
            {
                Name = "ООО «Призма»",
                INN = "123456789",
                OGRN = "1001234567890",
                LegalAddress = "г. Минск, ул. Притыцкого, д. 60",
                ContactPersonName = "Смирнова Татьяна",
                ContactPersonPosition = "Директор по HR",
                ContactPersonPhone = "+3 7529 700-00-01",
                ContactPersonEmail = "smirnova@prizma.by",
                ContractNumber = "Д-2026-001",
                ContractDate = new DateTime(2026, 1, 15),
                IsActive = true
            },
            new Organization
            {
                Name = "ЗАО «БелТехноМонтаж»",
                INN = "987654321",
                OGRN = "1029876543210",
                LegalAddress = "г. Минск, ул. Ложинская, д. 5",
                ContactPersonName = "Козлов Андрей",
                ContactPersonPosition = "Начальник отдела",
                ContactPersonPhone = "+3 7529 700-00-02",
                ContactPersonEmail = "kozlov@beltechmont.by",
                ContractNumber = "Д-2026-002",
                ContractDate = new DateTime(2026, 3, 1),
                IsActive = true
            }
        );
        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.Guests.Any())
    {
        dbContext.Guests.AddRange(
            new Guest { GuestCardNumber = "GC-2026-00000001", FullName = "Иванов Алексей Петрович", Phone = "+3 7529 123-45-67", IdentificationNumber = "123456789012", PassportNumber = "123456", RegistrationAddress = "г. Минск, ул. Ленина, д. 10, кв. 25", BirthDate = new DateTime(1985, 3, 15), Email = "ivanov@mail.ru", GuestType = "Физическое лицо" },
            new Guest { GuestCardNumber = "GC-2026-00000002", FullName = "Петрова Елена Сергеевна", Phone = "+3 7529 234-56-78", IdentificationNumber = "234567890123", PassportNumber = "234567", RegistrationAddress = "г. Минск, пр-т Независимости, д. 45, кв. 12", BirthDate = new DateTime(1990, 7, 22), Email = "petrova@gmail.com", GuestType = "Физическое лицо" },
            new Guest { GuestCardNumber = "GC-2026-00000003", FullName = "Сидоров Дмитрий Викторович", Phone = "+3 7529 345-67-89", IdentificationNumber = "345678901234", PassportNumber = "345678", RegistrationAddress = "г. Гомель, ул. Советская, д. 78", BirthDate = new DateTime(1978, 11, 5), Email = "sidorov@tut.by", GuestType = "Физическое лицо" },
            new Guest { GuestCardNumber = "GC-2026-00000004", FullName = "Козлова Анна Игоревна", Phone = "+3 7529 456-78-90", IdentificationNumber = "456789012345", PassportNumber = "456789", RegistrationAddress = "г. Брест, ул.Ленина, д. 5, кв. 33", BirthDate = new DateTime(1995, 1, 30), Email = "kozlova@bk.ru", GuestType = "Физическое лицо" },
            new Guest { GuestCardNumber = "GC-2026-00000005", FullName = "Новиков Сергей Александрович", Phone = "+3 7529 567-89-01", IdentificationNumber = "567890123456", PassportNumber = "567890", RegistrationAddress = "г. Витебск, ул. Мира, д. 12", BirthDate = new DateTime(1982, 6, 18), Email = "novikov@mail.ru", GuestType = "Физическое лицо" },
            new Guest { GuestCardNumber = "GC-2026-00000006", FullName = "Морозова Ольга Павловна", Phone = "+3 7529 678-90-12", IdentificationNumber = "678901234567", PassportNumber = "678901", RegistrationAddress = "г. Гродно, ул. Советская, д. 20, кв. 8", BirthDate = new DateTime(1988, 9, 12), Email = "morozova@gmail.com", GuestType = "Физическое лицо" },
            new Guest { GuestCardNumber = "GC-2026-00000007", FullName = "ООО «Призма»", Phone = "+3 7517 200-10-10", Email = "info@prizma.by", GuestType = "Организация", OrganizationName = "ООО «Призма»", INN = "123456789", OGRN = "1001234567890", LegalAddress = "г. Минск, ул. Притыцкого, д. 60", ContactPersonName = "Смирнова Татьяна", ContactPersonPosition = "Директор поHR", ContactPersonPhone = "+3 7529 700-00-01", ContactPersonEmail = "smirnova@prizma.by" },
            new Guest { GuestCardNumber = "GC-2026-00000008", FullName = "ЗАО «БелТехноМонтаж»", Phone = "+3 7517 300-20-20", Email = "office@beltechmont.by", GuestType = "Организация", OrganizationName = "ЗАО «БелТехноМонтаж»", INN = "987654321", OGRN = "1029876543210", LegalAddress = "г. Минск, ул. Ложинская, д. 5", ContactPersonName = "Козлов Андрей", ContactPersonPosition = "Начальник отдела", ContactPersonPhone = "+3 7529 700-00-02", ContactPersonEmail = "kozlov@beltechmont.by" }
        );
        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.Bookings.Any())
    {
        var guests = await dbContext.Guests.ToListAsync();
        var rooms = await dbContext.Rooms.ToListAsync();
        var dbServices = await dbContext.Services.ToListAsync();

        var g1 = guests.First(g => g.FullName.Contains("Иванов"));
        var g2 = guests.First(g => g.FullName.Contains("Петрова"));
        var g3 = guests.First(g => g.FullName.Contains("Сидоров"));
        var g4 = guests.First(g => g.FullName.Contains("Козлова"));
        var g5 = guests.First(g => g.FullName.Contains("Новиков"));
        var gOrg = guests.First(g => g.GuestType == "Организация" && g.OrganizationName!.Contains("Призма"));

        var orgPrizma = dbContext.Organizations.First(o => o.Name.Contains("Призма"));

        var r103 = rooms.First(r => r.RoomNumber == "103");
        var r104 = rooms.First(r => r.RoomNumber == "104");
        var r202 = rooms.First(r => r.RoomNumber == "202");
        var r402 = rooms.First(r => r.RoomNumber == "402");

        var sBreakfast = dbServices.First(s => s.Name.Contains("Заврак"));
        var sTransfer = dbServices.First(s => s.Name.Contains("из аэропорта"));
        var sMassage = dbServices.First(s => s.Name.Contains("Массаж"));
        var sWine = dbServices.First(s => s.Name.Contains("вина"));

        var today = DateTime.Today;

        var b1 = new Booking
        {
            BookingNumber = "BR-I-2026-SEED0001", CheckIn = today.AddDays(-2), CheckOut = today.AddDays(3),
            Status = BookingStatus.CheckedIn, NumberOfGuests = 1, Adults = 1, Children = 0,
            BookingType = BookingType.Individual, GuestId = g1.GuestId, RoomId = r103.RoomId
        };
        var b2 = new Booking
        {
            BookingNumber = "BR-I-2026-SEED0002", CheckIn = today.AddDays(-1), CheckOut = today.AddDays(2),
            Status = BookingStatus.CheckedIn, NumberOfGuests = 2, Adults = 2, Children = 0,
            BookingType = BookingType.Individual, GuestId = g2.GuestId, RoomId = r202.RoomId
        };
        var b3 = new Booking
        {
            BookingNumber = "BR-I-2026-SEED0003", CheckIn = today.AddDays(5), CheckOut = today.AddDays(10),
            Status = BookingStatus.Confirmed, NumberOfGuests = 2, Adults = 1, Children = 1,
            BookingType = BookingType.Individual, GuestId = g3.GuestId, RoomId = rooms.First(r => r.RoomNumber == "401").RoomId
        };
        var b4 = new Booking
        {
            BookingNumber = "BR-I-2026-SEED0004", CheckIn = today.AddDays(7), CheckOut = today.AddDays(9),
            Status = BookingStatus.Booked, NumberOfGuests = 1, Adults = 1, Children = 0,
            BookingType = BookingType.Individual, GuestId = g4.GuestId, RoomId = rooms.First(r => r.RoomNumber == "101").RoomId
        };
        var b5 = new Booking
        {
            BookingNumber = "BR-C-2026-SEED0005", CheckIn = today.AddDays(-5), CheckOut = today.AddDays(-2),
            Status = BookingStatus.CheckedOut, NumberOfGuests = 2, Adults = 2, Children = 0,
            BookingType = BookingType.Corporate, OrganizationId = orgPrizma.OrganizationId, CompanyContactPerson = "Смирнова Татьяна",
            CompanyPhone = "+3 7529 700-00-01", EmployeeList = "Смирнова Т., Волков А.", IsCompanyGuarantor = true,
            GuestId = gOrg.GuestId, RoomId = rooms.First(r => r.RoomNumber == "201").RoomId
        };
        var b6 = new Booking
        {
            BookingNumber = "BR-I-2026-SEED0006", CheckIn = today.AddDays(-10), CheckOut = today.AddDays(-7),
            Status = BookingStatus.CheckedOut, NumberOfGuests = 1, Adults = 1, Children = 0,
            BookingType = BookingType.Individual, GuestId = g5.GuestId, RoomId = rooms.First(r => r.RoomNumber == "102").RoomId
        };
        var b7 = new Booking
        {
            BookingNumber = "BR-I-2026-SEED0007", CheckIn = today.AddDays(15), CheckOut = today.AddDays(20),
            Status = BookingStatus.Cancelled, NumberOfGuests = 2, Adults = 2, Children = 0,
            BookingType = BookingType.Individual, GuestId = g1.GuestId, RoomId = rooms.First(r => r.RoomNumber == "101").RoomId
        };

        dbContext.Bookings.AddRange(b1, b2, b3, b4, b5, b6, b7);
        await dbContext.SaveChangesAsync();

        dbContext.BookingRooms.AddRange(
            new BookingRoom { BookingId = b1.BookingId, RoomId = r103.RoomId },
            new BookingRoom { BookingId = b2.BookingId, RoomId = r202.RoomId },
            new BookingRoom { BookingId = b3.BookingId, RoomId = rooms.First(r => r.RoomNumber == "401").RoomId },
            new BookingRoom { BookingId = b4.BookingId, RoomId = rooms.First(r => r.RoomNumber == "101").RoomId },
            new BookingRoom { BookingId = b5.BookingId, RoomId = rooms.First(r => r.RoomNumber == "201").RoomId },
            new BookingRoom { BookingId = b6.BookingId, RoomId = rooms.First(r => r.RoomNumber == "102").RoomId },
            new BookingRoom { BookingId = b7.BookingId, RoomId = rooms.First(r => r.RoomNumber == "101").RoomId }
        );

        dbContext.GuestServiceOrders.AddRange(
            new GuestServiceOrder { BookingId = b1.BookingId, ServiceId = sBreakfast.ServiceId, Quantity = 3, PriceCharged = sBreakfast.Price, OrderDate = today.AddDays(-1) },
            new GuestServiceOrder { BookingId = b1.BookingId, ServiceId = sMassage.ServiceId, Quantity = 1, PriceCharged = sMassage.Price, OrderDate = today },
            new GuestServiceOrder { BookingId = b2.BookingId, ServiceId = sTransfer.ServiceId, Quantity = 1, PriceCharged = sTransfer.Price, OrderDate = today.AddDays(-1) },
            new GuestServiceOrder { BookingId = b2.BookingId, ServiceId = sWine.ServiceId, Quantity = 1, PriceCharged = sWine.Price, OrderDate = today },
            new GuestServiceOrder { BookingId = b5.BookingId, ServiceId = sBreakfast.ServiceId, Quantity = 4, PriceCharged = sBreakfast.Price, OrderDate = today.AddDays(-4) }
        );

        dbContext.Payments.AddRange(
            new Payment { PaymentNumber = "PAY-2026-SEED0001", PaymentDate = today.AddDays(-2), Amount = 275.00m, Method = PaymentMethod.Card, Description = "Оплата проживания", BookingId = b1.BookingId },
            new Payment { PaymentNumber = "PAY-2026-SEED0002", PaymentDate = today.AddDays(-1), Amount = 190.00m, Method = PaymentMethod.Cash, Description = "Частичная оплата", BookingId = b2.BookingId },
            new Payment { PaymentNumber = "PAY-2026-SEED0003", PaymentDate = today.AddDays(-5), Amount = 330.00m, Method = PaymentMethod.BankTransfer, Description = "Оплата по счёту", BookingId = b5.BookingId },
            new Payment { PaymentNumber = "PAY-2026-SEED0004", PaymentDate = today.AddDays(-10), Amount = 165.00m, Method = PaymentMethod.Card, Description = "Полная оплата", BookingId = b6.BookingId }
        );

        dbContext.LedgerEntries.AddRange(
            new LedgerEntry { EntryType = LedgerEntryType.Accommodation, Date = today.AddDays(-2), Description = "Проживание: BR-I-2026-SEED0001", Debit = 275.00m, Credit = 0, Balance = -275.00m, GuestId = g1.GuestId, BookingId = b1.BookingId },
            new LedgerEntry { EntryType = LedgerEntryType.Payment, Date = today.AddDays(-2), Description = "Оплата картой", Debit = 0, Credit = 275.00m, Balance = 0, GuestId = g1.GuestId, BookingId = b1.BookingId },
            new LedgerEntry { EntryType = LedgerEntryType.Accommodation, Date = today.AddDays(-1), Description = "Проживание: BR-I-2026-SEED0002", Debit = 285.00m, Credit = 0, Balance = -285.00m, GuestId = g2.GuestId, BookingId = b2.BookingId },
            new LedgerEntry { EntryType = LedgerEntryType.Payment, Date = today.AddDays(-1), Description = "Оплата наличными", Debit = 0, Credit = 190.00m, Balance = -95.00m, GuestId = g2.GuestId, BookingId = b2.BookingId },
            new LedgerEntry { EntryType = LedgerEntryType.Service, Date = today, Description = "Доп. услуга: Массаж", Debit = 70.00m, Credit = 0, Balance = -165.00m, GuestId = g1.GuestId, BookingId = b1.BookingId }
        );

        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.CleaningTasks.Any())
    {
        var rooms = await dbContext.Rooms.ToListAsync();
        var r104 = rooms.First(r => r.RoomNumber == "104");

        dbContext.CleaningTasks.AddRange(
            new CleaningTask { RoomId = r104.RoomId, CleaningType = CleaningType.Departure, Status = CleaningTaskStatus.Assigned, AssignedTo = "housekeeper@solemare.com", Notes = "Выездная уборка, после гостя оставлены полотенца", CreatedAt = DateTime.Now.AddHours(-2) },
            new CleaningTask { RoomId = rooms.First(r => r.RoomNumber == "103").RoomId, CleaningType = CleaningType.Intermediate, Status = CleaningTaskStatus.Completed, AssignedTo = "housekeeper@solemare.com", Notes = "Промежуточная уборка", CreatedAt = DateTime.Now.AddDays(-1), CompletedAt = DateTime.Now.AddHours(-1), Result = CleaningResult.Success }
        );

        dbContext.CleaningJournals.Add(new CleaningJournal
        {
            RoomId = rooms.First(r => r.RoomNumber == "103").RoomId,
            HousekeeperName = "housekeeper@solemare.com",
            EntryTime = DateTime.Now.AddDays(-1),
            ExitTime = DateTime.Now.AddHours(-1),
            CleaningType = CleaningType.Intermediate,
            Result = "Успешно",
            Notes = "Промежуточная уборка"
        });

        dbContext.RoomInventories.AddRange(
            new RoomInventory { RoomId = r104.RoomId, ItemName = "Полотенце банное", ExpectedQuantity = 4, Category = "Текстиль" },
            new RoomInventory { RoomId = r104.RoomId, ItemName = "Полотенце лиц.", ExpectedQuantity = 2, Category = "Текстиль" },
            new RoomInventory { RoomId = r104.RoomId, ItemName = "Халат", ExpectedQuantity = 2, Category = "Текстиль" },
            new RoomInventory { RoomId = r104.RoomId, ItemName = "Телевизор Samsung", ExpectedQuantity = 1, Category = "Оборудование" },
            new RoomInventory { RoomId = r104.RoomId, ItemName = "Мини-бар (холодильник)", ExpectedQuantity = 1, Category = "Мини-бар" },
            new RoomInventory { RoomId = r104.RoomId, ItemName = "Кофейник", ExpectedQuantity = 1, Category = "Оборудование" }
        );

        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.ActivityLogs.Any())
    {
        dbContext.ActivityLogs.AddRange(
            new ActivityLog { UserName = "reception@solemare.com", Action = "Заселение", Description = "Бронь BR-I-2026-SEED0001, номер 103", Timestamp = DateTime.Now.AddDays(-2) },
            new ActivityLog { UserName = "reception@solemare.com", Action = "Заселение", Description = "Бронь BR-I-2026-SEED0002, номер 202", Timestamp = DateTime.Now.AddDays(-1) },
            new ActivityLog { UserName = "reception@solemare.com", Action = "Выселение", Description = "Бронь BR-C-2026-SEED0005, номер 201", Timestamp = DateTime.Now.AddDays(-2) },
            new ActivityLog { UserName = "reception@solemare.com", Action = "Выселение", Description = "Бронь BR-I-2026-SEED0006, номер 102", Timestamp = DateTime.Now.AddDays(-7) },
            new ActivityLog { UserName = "reception@solemare.com", Action = "Создание брони", Description = "Бронь BR-I-2026-SEED0004, номер 101", Timestamp = DateTime.Now.AddDays(-1) }
        );
        await dbContext.SaveChangesAsync();
    }
}

static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
{
    var existing = await userManager.FindByEmailAsync(email);
    if (existing != null)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(existing);
        await userManager.ResetPasswordAsync(existing, token, password);

        if (!await userManager.IsInRoleAsync(existing, role))
            await userManager.AddToRoleAsync(existing, role);

        Console.WriteLine($"[Seed] {email} ({role}) — пароль сброшен");
        return;
    }

    var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, Role = role };
    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(user, role);
        Console.WriteLine($"[Seed] {email} ({role}) — создан");
    }
    else
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        Console.WriteLine($"[Seed] ОШИБКА {email}: {errors}");
    }
}

app.Run();