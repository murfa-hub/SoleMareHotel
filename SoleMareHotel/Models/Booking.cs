using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Display(Name = "Номер брони")]
        public string? BookingNumber { get; set; }

        [Required(ErrorMessage = "Дата заезда обязательна")]
        [Display(Name = "Дата заезда")]
        [DataType(DataType.DateTime)]
        public DateTime CheckIn { get; set; }

        [Required(ErrorMessage = "Дата выезда обязательна")]
        [Display(Name = "Дата выезда")]
        [DataType(DataType.DateTime)]
        public DateTime CheckOut { get; set; }

        [Display(Name = "Статус")]
        public BookingStatus Status { get; set; } = BookingStatus.Booked;

        [Display(Name = "Количество гостей")]
        public int NumberOfGuests { get; set; } = 1;

        [Display(Name = "Взрослых")]
        public int Adults { get; set; } = 1;

        [Display(Name = "Детей")]
        public int Children { get; set; } = 0;

        [Display(Name = "Тип бронирования")]
        public BookingType BookingType { get; set; } = BookingType.Individual;

        [Display(Name = "Организация")]
        public int? OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization? Organization { get; set; }

        [Display(Name = "Контактное лицо организации")]
        public string? CompanyContactPerson { get; set; }

        [Display(Name = "Телефон организации")]
        public string? CompanyPhone { get; set; }

        [Display(Name = "Список сотрудников")]
        public string? EmployeeList { get; set; }

        [Display(Name = "Гарант оплаты")]
        public bool IsCompanyGuarantor { get; set; } = false;

        public int GuestId { get; set; }
        public Guest? Guest { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }

        public List<BookingRoom>? BookingRooms { get; set; }
        public List<GuestServiceOrder>? ServiceOrders { get; set; }
        public List<Payment>? Payments { get; set; }
        public List<Act>? Acts { get; set; }
        public List<LedgerEntry>? LedgerEntries { get; set; }

        public bool IsValidDuration()
        {
            return (CheckOut - CheckIn).TotalDays <= 30;
        }

        public decimal CalculateTotalCost()
        {
            decimal total = 0;
            if (BookingRooms != null && BookingRooms.Any())
            {
                foreach (var br in BookingRooms)
                {
                    if (br.Room != null)
                    {
                        var nights = (CheckOut - CheckIn).Days;
                        if (nights <= 0) nights = 1;
                        total += nights * br.Room.PricePerNight;
                    }
                }
            }
            else if (Room != null)
            {
                var nights = (CheckOut - CheckIn).Days;
                if (nights <= 0) nights = 1;
                total = nights * Room.PricePerNight;
            }
            return total;
        }

        public decimal CalculateFullCost()
        {
            decimal servicesTotal = 0;
            if (ServiceOrders != null)
                servicesTotal = ServiceOrders.Sum(o => o.TotalPrice);
            return CalculateTotalCost() + servicesTotal;
        }

        public decimal CalculatePaidTotal()
        {
            if (Payments == null || !Payments.Any()) return 0;
            return Payments.Sum(p => p.Amount);
        }

        public decimal CalculateRemainingDebt()
        {
            return CalculateFullCost() - CalculatePaidTotal();
        }
    }
}
