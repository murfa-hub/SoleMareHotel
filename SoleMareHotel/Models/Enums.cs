using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public enum RoomStatus
    {
        [Display(Name = "Свободен")]
        Free = 0,
        [Display(Name = "Занят")]
        Occupied = 1,
        [Display(Name = "На уборке")]
        Cleaning = 2,
        [Display(Name = "На ремонте")]
        UnderRepair = 3,
        [Display(Name = "Списан")]
        WrittenOff = 4
    }

    public enum BookingStatus
    {
        [Display(Name = "Забронировано")]
        Booked = 0,
        [Display(Name = "Подтверждено")]
        Confirmed = 1,
        [Display(Name = "Заселено")]
        CheckedIn = 2,
        [Display(Name = "Выписано")]
        CheckedOut = 3,
        [Display(Name = "Отменено")]
        Cancelled = 4
    }

    public enum BookingType
    {
        [Display(Name = "Индивидуальное")]
        Individual = 0,
        [Display(Name = "Корпоративное")]
        Corporate = 1
    }

    public enum RequestStatus
    {
        [Display(Name = "Новая")]
        New = 0,
        [Display(Name = "Подтверждена")]
        Confirmed = 1,
        [Display(Name = "Отклонена")]
        Rejected = 2
    }

    public enum CleaningTaskStatus
    {
        [Display(Name = "Назначена")]
        Assigned = 0,
        [Display(Name = "В работе")]
        InProgress = 1,
        [Display(Name = "Выполнена")]
        Completed = 2
    }

    public enum CleaningType
    {
        [Display(Name = "Выездная")]
        Departure = 0,
        [Display(Name = "Промежуточная")]
        Intermediate = 1,
        [Display(Name = "Внеплановая")]
        Unscheduled = 2
    }

    public enum CleaningResult
    {
        [Display(Name = "Успешно")]
        Success = 0,
        [Display(Name = "Повреждения")]
        Damage = 1,
        [Display(Name = "Неисправности")]
        Malfunction = 2
    }

    public enum CleaningReportStatus
    {
        [Display(Name = "На проверке")]
        Pending = 0,
        [Display(Name = "Согласован")]
        Approved = 1,
        [Display(Name = "Отклонён")]
        Rejected = 2
    }

    public enum ServiceCategory
    {
        [Display(Name = "Питание")]
        Food = 0,
        [Display(Name = "Бытовые")]
        Household = 1,
        [Display(Name = "Транспорт")]
        Transport = 2,
        [Display(Name = "Оздоровительные")]
        Wellness = 3,
        [Display(Name = "Прочие")]
        Other = 4
    }

    public enum PaymentMethod
    {
        [Display(Name = "Наличные")]
        Cash = 0,
        [Display(Name = "Банковская карта")]
        Card = 1,
        [Display(Name = "Безналичный расчёт")]
        BankTransfer = 2
    }

    public enum LedgerEntryType
    {
        [Display(Name = "Проживание")]
        Accommodation = 0,
        [Display(Name = "Доп. услуга")]
        Service = 1,
        [Display(Name = "Оплата")]
        Payment = 2,
        [Display(Name = "Корректировка")]
        Adjustment = 3
    }
}
