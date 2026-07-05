using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Display(Name = "Номер платежа")]
        public string? PaymentNumber { get; set; }

        [Display(Name = "Дата оплаты")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Display(Name = "Сумма")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Способ оплаты")]
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        [Display(Name = "Назначение")]
        public string? Description { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }
    }
}
