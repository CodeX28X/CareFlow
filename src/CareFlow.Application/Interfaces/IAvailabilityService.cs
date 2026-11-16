
using CareFlow.Application.Availability;

namespace CareFlow.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(
        DateOnly bookingDate,
        CancellationToken cancellationToken = default);
}
