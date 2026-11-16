
using CareFlow.Application.Bookings;

namespace CareFlow.Application.Interfaces;

public interface IBookingService
{
    Task<BookingResult> ConfirmBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);
}
