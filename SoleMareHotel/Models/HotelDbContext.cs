using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SoleMareHotel.Models
{
    public class HotelDbContext : IdentityDbContext<ApplicationUser>
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }

        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingRoom> BookingRooms { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<GuestServiceOrder> GuestServiceOrders { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<Act> Acts { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<RoomInventory> RoomInventories { get; set; }
        public DbSet<CleaningTask> CleaningTasks { get; set; }
        public DbSet<CleaningReport> CleaningReports { get; set; }
        public DbSet<CleaningSchedule> CleaningSchedules { get; set; }
        public DbSet<CleaningJournal> CleaningJournals { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<RoomChecklist> RoomChecklists { get; set; }
        public DbSet<RoomChecklistItem> RoomChecklistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Room>()
                .Property(r => r.PricePerNight)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<GuestServiceOrder>()
                .Property(o => o.PriceCharged)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LedgerEntry>()
                .Property(l => l.Credit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LedgerEntry>()
                .Property(l => l.Debit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LedgerEntry>()
                .Property(l => l.Balance)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomNumber)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomCategoryId);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.RoomId);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.GuestId);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.Status);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.CheckIn);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.CheckOut);

            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.Status);

            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.RoomId);

            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.GuestId);

            modelBuilder.Entity<Guest>()
                .HasIndex(g => g.Email);

            modelBuilder.Entity<Guest>()
                .HasIndex(g => g.GuestCardNumber)
                .IsUnique();

            modelBuilder.Entity<GuestServiceOrder>()
                .HasIndex(o => o.BookingId);

            modelBuilder.Entity<GuestServiceOrder>()
                .HasIndex(o => o.ServiceId);

            modelBuilder.Entity<Act>()
                .HasIndex(a => a.RoomId);

            modelBuilder.Entity<CleaningTask>()
                .HasIndex(ct => ct.RoomId);

            modelBuilder.Entity<CleaningTask>()
                .HasIndex(ct => ct.AssignedTo);

            modelBuilder.Entity<CleaningTask>()
                .HasIndex(ct => ct.Status);

            modelBuilder.Entity<CleaningReport>()
                .HasIndex(cr => cr.RoomId);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(al => al.Timestamp);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(al => al.UserName);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.BookingId);

            modelBuilder.Entity<LedgerEntry>()
                .HasIndex(l => l.GuestId);

            modelBuilder.Entity<CleaningJournal>()
                .HasIndex(cj => cj.RoomId);

            modelBuilder.Entity<CleaningJournal>()
                .HasIndex(cj => cj.HousekeeperName);

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Name);

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.INN);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.OrganizationId);

            modelBuilder.Entity<BookingRequest>()
                .HasIndex(br => br.OrganizationId);

            modelBuilder.Entity<Act>()
                .HasOne(a => a.Booking)
                .WithMany(b => b.Acts)
                .HasForeignKey(a => a.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Act>()
                .HasOne(a => a.Guest)
                .WithMany()
                .HasForeignKey(a => a.GuestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Act>()
                .HasOne(a => a.Room)
                .WithMany()
                .HasForeignKey(a => a.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CleaningReport>()
                .HasOne(cr => cr.CleaningTask)
                .WithMany()
                .HasForeignKey(cr => cr.CleaningTaskId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CleaningReport>()
                .HasOne(cr => cr.Room)
                .WithMany()
                .HasForeignKey(cr => cr.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CleaningTask>()
                .HasOne(ct => ct.Room)
                .WithMany()
                .HasForeignKey(ct => ct.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RoomInventory>()
                .HasOne(ri => ri.Room)
                .WithMany()
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BookingRoom>()
                .HasOne(br => br.Booking)
                .WithMany(b => b.BookingRooms)
                .HasForeignKey(br => br.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingRoom>()
                .HasOne(br => br.Room)
                .WithMany()
                .HasForeignKey(br => br.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LedgerEntry>()
                .HasOne(l => l.Guest)
                .WithMany()
                .HasForeignKey(l => l.GuestId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LedgerEntry>()
                .HasOne(l => l.Booking)
                .WithMany(b => b.LedgerEntries)
                .HasForeignKey(l => l.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CleaningJournal>()
                .HasOne(cj => cj.Room)
                .WithMany()
                .HasForeignKey(cj => cj.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Organization)
                .WithMany(o => o.Bookings)
                .HasForeignKey(b => b.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BookingRequest>()
                .HasOne(br => br.Organization)
                .WithMany()
                .HasForeignKey(br => br.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RoomChecklist>()
                .HasOne(rc => rc.Room)
                .WithMany()
                .HasForeignKey(rc => rc.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RoomChecklist>()
                .HasOne(rc => rc.CleaningTask)
                .WithMany()
                .HasForeignKey(rc => rc.CleaningTaskId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RoomChecklistItem>()
                .HasOne(rci => rci.RoomChecklist)
                .WithMany(rc => rc.Items)
                .HasForeignKey(rci => rci.RoomChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomChecklist>()
                .HasIndex(rc => rc.RoomId);

            modelBuilder.Entity<RoomChecklist>()
                .HasIndex(rc => rc.HousekeeperName);

            modelBuilder.Entity<RoomChecklist>()
                .HasIndex(rc => rc.CheckDate);
        }
    }
}
