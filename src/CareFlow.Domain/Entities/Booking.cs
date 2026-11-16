
namespace CareFlow.Domain.Entities;

public class Booking
{
    public int BookingId { get; set; }
    public int PatientId { get; set; }
    public DateOnly BookingDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Patient Patient { get; set; } = null!;

    public ICollection<BookingService> BookingServices { get; set; }
        = new List<BookingService>();
}
