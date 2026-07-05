using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class RoomChecklistItem
    {
        public int RoomChecklistItemId { get; set; }

        [Display(Name = "Чек-лист")]
        public int RoomChecklistId { get; set; }
        public RoomChecklist? RoomChecklist { get; set; }

        [Display(Name = "Предмет")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "Категория")]
        public string? Category { get; set; }

        [Display(Name = "Ожидаемое кол-во")]
        public int ExpectedQuantity { get; set; } = 1;

        [Display(Name = "Фактическое кол-во")]
        public int ActualQuantity { get; set; } = 1;

        [Display(Name = "В порядке")]
        public bool IsOk { get; set; } = true;

        [Display(Name = "Примечание")]
        public string? Note { get; set; }
    }
}
