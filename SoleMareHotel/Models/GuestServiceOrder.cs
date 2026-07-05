using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class GuestServiceOrder
    {
        public int GuestServiceOrderId { get; set; }

        [Display(Name = "Дата заказа")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Количество")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Цена на момент заказа")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceCharged { get; set; }

        // Внешние ключи
        [Display(Name = "Бронирование")]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Display(Name = "Услуга")]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        // Стоимость = количество × цена
        public decimal TotalPrice => Quantity * PriceCharged;
    }
}