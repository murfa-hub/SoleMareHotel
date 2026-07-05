using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class RoomChecklist
    {
        public int RoomChecklistId { get; set; }

        [Display(Name = "Номер")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Display(Name = "Горничная")]
        public string? HousekeeperName { get; set; }

        [Display(Name = "Дата проверки")]
        public DateTime CheckDate { get; set; } = DateTime.Now;

        [Display(Name = "Все в порядке")]
        public bool AllItemsOk { get; set; } = true;

        [Display(Name = "Замечания")]
        public string? Notes { get; set; }

        [Display(Name = "Связанное задание")]
        public int? CleaningTaskId { get; set; }
        public CleaningTask? CleaningTask { get; set; }

        public List<RoomChecklistItem> Items { get; set; } = new();
    }
}
