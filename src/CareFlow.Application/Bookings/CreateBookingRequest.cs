
namespace CareFlow.Application.Bookings;

public sealed record CreateBookingRequest(
    int PatientId,
    DateOnly BookingDate,
    IReadOnlyList<int> ServiceIds);
