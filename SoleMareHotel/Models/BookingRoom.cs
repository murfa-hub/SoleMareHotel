using System.ComponentModel.DataAnnotations;

namespace SoleMareHotel.Models
{
    public class BookingRoom
    {
        public int BookingRoomId { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Display(Name = "Номер комнаты")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Display(Name = "Примечание")]
        public string? Note { get; set; }
    }
}
