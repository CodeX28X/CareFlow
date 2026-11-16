
namespace CareFlow.Application.Bookings;

public sealed record BookedService(
    int ServiceId,
    string ServiceName);

public sealed record BookingDetails(
    int BookingId,
    int PatientId,
    string PatientCode,
    string PatientName,
    DateOnly BookingDate,
    DateTime CreatedAt,
    IReadOnlyList<BookedService> Services);
