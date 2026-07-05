using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class CleaningReport
    {
        public int CleaningReportId { get; set; }

        [Display(Name = "Задание")]
        public int CleaningTaskId { get; set; }
        public CleaningTask? CleaningTask { get; set; }

        [Display(Name = "Номер")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Display(Name = "Горничная")]
        public string? ReportedBy { get; set; }

        [Display(Name = "Дата отчёта")]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [Display(Name = "Имущество в порядке")]
        public bool InventoryOk { get; set; } = true;

        [Display(Name = "Описание проблем")]
        public string? IssueDescription { get; set; }

        [Display(Name = "Требуется ремонт")]
        public bool NeedsRepair { get; set; } = false;

        [Display(Name = "Описание неисправности")]
        public string? RepairDescription { get; set; }

        [Display(Name = "Путь к фото")]
        public string? PhotoPath { get; set; }

        [Display(Name = "Статус")]
        public CleaningReportStatus Status { get; set; } = CleaningReportStatus.Pending;
    }
}