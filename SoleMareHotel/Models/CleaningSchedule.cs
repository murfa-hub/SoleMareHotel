using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class CleaningSchedule
    {
        public int CleaningScheduleId { get; set; }

        [Display(Name = "Интервал (дни)")]
        public int IntervalDays { get; set; } = 3;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Последний запуск")]
        public DateTime? LastRun { get; set; }
    }
}