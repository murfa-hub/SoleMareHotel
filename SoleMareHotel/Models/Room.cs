using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Номер комнаты обязателен")]
        [Display(Name = "Номер комнаты")]
        public string RoomNumber { get; set; } = string.Empty;

        [Display(Name = "Этаж")]
        public int Floor { get; set; }

        [Display(Name = "Расположение")]
        public string? Location { get; set; }

        [Required]
        [Display(Name = "Статус")]
        public RoomStatus Status { get; set; } = RoomStatus.Free;

        [Display(Name = "Вместимость (чел.)")]
        public int Capacity { get; set; } = 1;

        [Display(Name = "Цена за сутки")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "Дополнительные удобства")]
        public string? AdditionalAmenities { get; set; }

        [Display(Name = "Описание номера")]
        public string? Description { get; set; }

        [Display(Name = "Фото номера")]
        public string? PhotoPath { get; set; }

        // Внешний ключ
        [Display(Name = "Категория")]
        public int RoomCategoryId { get; set; }
        public RoomCategory? Category { get; set; }

        public List<Booking>? Bookings { get; set; }
    }
}