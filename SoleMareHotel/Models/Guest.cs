using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class Guest
    {
        public int GuestId { get; set; }

        [Display(Name = "Номер гостевой карты")]
        public string? GuestCardNumber { get; set; }

        [Display(Name = "Email (для входа)")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Тип гостя")]
        public string? GuestType { get; set; } = "Физическое лицо";

        [Display(Name = "ФИО")]
        public string? FullName { get; set; }

        [Display(Name = "Идентификационный номер")]
        public string? IdentificationNumber { get; set; }

        [Display(Name = "Номер паспорта")]
        public string? PassportNumber { get; set; }

        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Display(Name = "Адрес регистрации")]
        public string? RegistrationAddress { get; set; }

        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Наименование организации")]
        public string? OrganizationName { get; set; }

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

        [Display(Name = "Данные аннулированы")]
        public bool IsAnnuiled { get; set; } = false;

        public List<Booking>? Bookings { get; set; }

        public bool IsAdult()
        {
            if (GuestType == "Организация") return true;
            if (BirthDate == null) return true;
            var age = DateTime.Today.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > DateTime.Today.AddYears(-age)) age--;
            return age >= 18;
        }
    }
}
