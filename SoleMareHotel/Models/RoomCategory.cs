using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class RoomCategory
    {
        public int RoomCategoryId { get; set; }

        [Required(ErrorMessage = "Название категории обязательно")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Удобства по умолчанию")]
        public string? DefaultAmenities { get; set; }

        public List<Room>? Rooms { get; set; }
    }
}