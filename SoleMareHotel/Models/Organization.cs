using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class Organization
    {
        public int OrganizationId { get; set; }

        [Required(ErrorMessage = "Наименование организации обязательно")]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "ИНН")]
        public string? INN { get; set; }

        [Display(Name = "ОГРН")]
        public string? OGRN { get; set; }

        [Display(Name = "Юридический адрес")]
        public string? LegalAddress { get; set; }

        [Display(Name = "Контактное лицо (ФИО)")]
        public string? ContactPersonName { get; set; }

        [Display(Name = "Должность")]
        public string? ContactPersonPosition { get; set; }

        [Display(Name = "Телефон контактного лица")]
        public string? ContactPersonPhone { get; set; }

        [Display(Name = "Email контактного лица")]
        [EmailAddress]
        public string? ContactPersonEmail { get; set; }

        [Display(Name = "Номер договора")]
        public string? ContractNumber { get; set; }

        [Display(Name = "Дата договора")]
        [DataType(DataType.Date)]
        public DateTime? ContractDate { get; set; }

        [Display(Name = "Договор действует")]
        public bool IsActive { get; set; } = true;

        public List<Booking>? Bookings { get; set; }
    }
}
