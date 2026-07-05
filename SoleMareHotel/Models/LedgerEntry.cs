using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class LedgerEntry
    {
        public int LedgerEntryId { get; set; }

        [Display(Name = "Тип записи")]
        public LedgerEntryType EntryType { get; set; }

        [Display(Name = "Дата")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Сумма (приход)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Credit { get; set; }

        [Display(Name = "Сумма (расход)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Debit { get; set; }

        [Display(Name = "Баланс")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public int GuestId { get; set; }
        public Guest? Guest { get; set; }

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }
    }
}
