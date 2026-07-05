using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class CleaningTask
    {
        public int CleaningTaskId { get; set; }

        [Display(Name = "Номер комнаты")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Display(Name = "Тип уборки")]
        public CleaningType CleaningType { get; set; } = CleaningType.Departure;

        [Display(Name = "Статус")]
        public CleaningTaskStatus Status { get; set; } = CleaningTaskStatus.Assigned;

        [Display(Name = "Горничная")]
        public string? AssignedTo { get; set; }

        [Display(Name = "Особые отметки")]
        public string? Notes { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Дата выполнения")]
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Результат")]
        public CleaningResult? Result { get; set; }
    }
}