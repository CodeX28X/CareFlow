
using CareFlow.Application.Bookings;

namespace CareFlow.Application.Interfaces;

public interface IBookingQueryService
{
    Task<IReadOnlyList<BookingDetails>> GetBookingsAsync(
        CancellationToken cancellationToken = default);

    Task<BookingDetails?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);
}
