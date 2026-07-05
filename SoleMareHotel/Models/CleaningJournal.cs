using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class CleaningJournal
    {
        public int CleaningJournalId { get; set; }

        [Display(Name = "Номер комнаты")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Display(Name = "Горничная")]
        public string? HousekeeperName { get; set; }

        [Display(Name = "Время входа")]
        public DateTime EntryTime { get; set; }

        [Display(Name = "Время выхода")]
        public DateTime? ExitTime { get; set; }

        [Display(Name = "Тип уборки")]
        public CleaningType CleaningType { get; set; }

        [Display(Name = "Результат")]
        public string? Result { get; set; }

        [Display(Name = "Примечания")]
        public string? Notes { get; set; }

        [Display(Name = "Повреждения обнаружены")]
        public bool DamageFound { get; set; } = false;

        [Display(Name = "Описание повреждений")]
        public string? DamageDescription { get; set; }
    }
}
