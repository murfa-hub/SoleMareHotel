using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class Act
    {
        public int ActId { get; set; }

        [Display(Name = "Тип акта")]
        public string ActType { get; set; } = "Заселение";

        [Display(Name = "Номер акта")]
        public string? ActNumber { get; set; }

        [Display(Name = "Дата составления")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Дата заезда")]
        public DateTime? CheckInDate { get; set; }

        [Display(Name = "Время заезда")]
        public string? CheckInTime { get; set; }

        [Display(Name = "Количество гостей")]
        public int? NumberOfGuests { get; set; }

        [Display(Name = "Описание")]
        public string? DamageDescription { get; set; }

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int? GuestId { get; set; }
        public Guest? Guest { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }
    }
}