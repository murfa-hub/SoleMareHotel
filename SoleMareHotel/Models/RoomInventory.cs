using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class RoomInventory
    {
        public int RoomInventoryId { get; set; }

        [Display(Name = "Номер")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Display(Name = "Наименование предмета")]
        public string ItemName { get; set; } = string.Empty;

        [Display(Name = "Количество по описи")]
        public int ExpectedQuantity { get; set; } = 1;

        [Display(Name = "Категория")]
        public string? Category { get; set; } // Мебель, Текстиль, Оборудование, Мини-бар, Сантехника, Другое
    }
}