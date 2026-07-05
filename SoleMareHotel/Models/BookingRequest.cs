using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoleMareHotel.Models
{
    public class BookingRequest
    {
        public int BookingRequestId { get; set; }

        [Display(Name = "Номер заявки")]
        public string? RequestNumber { get; set; }

        [Display(Name = "Тип бронирования")]
        public BookingType BookingType { get; set; } = BookingType.Individual;

        [Display(Name = "Дата заезда")]
        public DateTime CheckIn { get; set; }

        [Display(Name = "Дата выезда")]
        public DateTime CheckOut { get; set; }

        [Display(Name = "Количество гостей")]
        public int Guests { get; set; } = 1;

        [Display(Name = "Взрослых")]
        public int Adults { get; set; } = 1;

        [Display(Name = "Детей")]
        public int Children { get; set; } = 0;

        [Display(Name = "Статус")]
        public RequestStatus Status { get; set; } = RequestStatus.New;

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Комментарий")]
        public string? Comment { get; set; }

        [Display(Name = "Организация")]
        public int? OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization? Organization { get; set; }

        [Display(Name = "Контактное лицо")]
        public string? ContactPerson { get; set; }

        [Display(Name = "Телефон организации")]
        public string? CompanyPhone { get; set; }

        [Display(Name = "Список сотрудников")]
        public string? EmployeesList { get; set; }

        [Display(Name = "Требуемое количество номеров")]
        public int? RoomsCount { get; set; }

        [Display(Name = "Гарант оплаты")]
        public bool IsCompanyGuarantor { get; set; } = false;

        public int GuestId { get; set; }
        public Guest? Guest { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }
    }
}