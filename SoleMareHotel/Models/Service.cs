using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class Service
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Название услуги обязательно")]
        [Display(Name = "Название услуги")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Категория")]
        public ServiceCategory? Category { get; set; }

        [Display(Name = "Цена")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Описание")]
        public string? Description { get; set; }
    }
}