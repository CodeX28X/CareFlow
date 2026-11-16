
using CareFlow.Application.Availability;
using CareFlow.Application.Interfaces;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Services;

public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IDbContextFactory<CareFlowDbContext> _contextFactory;

    public AvailabilityService(
        IDbContextFactory<CareFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(
        DateOnly bookingDate,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var services = await db.DiagnosticServices
            .AsNoTracking()
            .OrderBy(s => s.ServiceId)
            .Select(s => new
            {
                s.ServiceId,
                s.ServiceName,
                s.DailyCapacity
            })
            .ToListAsync(cancellationToken);

        var counts = await db.BookingServices
            .AsNoTracking()
            .Where(bs => bs.Booking.BookingDate == bookingDate)
            .GroupBy(bs => bs.ServiceId)
            .Select(group => new
            {
                ServiceId = group.Key,
                ConfirmedCount = group.Count()
            })
            .ToDictionaryAsync(
                x => x.ServiceId,
                x => x.ConfirmedCount,
                cancellationToken);

        return services
            .Select(service =>
            {
                int confirmed = counts.GetValueOrDefault(service.ServiceId);

                return new ServiceAvailability(
                    service.ServiceId,
                    service.ServiceName,
                    service.DailyCapacity,
                    confirmed,
                    service.DailyCapacity - confirmed);
            })
            .ToList();
    }
}
