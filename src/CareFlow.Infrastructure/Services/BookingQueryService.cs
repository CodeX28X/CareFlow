
using CareFlow.Application.Bookings;
using CareFlow.Application.Interfaces;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Services;

public sealed class BookingQueryService : IBookingQueryService
{
    private readonly IDbContextFactory<CareFlowDbContext> _contextFactory;

    public BookingQueryService(
        IDbContextFactory<CareFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<BookingDetails>> GetBookingsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.DiagnosticService)
            .AsSplitQuery()
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.BookingId)
            .ToListAsync(cancellationToken);

        return bookings.Select(MapBooking).ToList();
    }

    public async Task<BookingDetails?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.DiagnosticService)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                b => b.BookingId == bookingId,
                cancellationToken);

        return booking is null ? null : MapBooking(booking);
    }

    private static BookingDetails MapBooking(
        CareFlow.Domain.Entities.Booking booking)
    {
        var services = booking.BookingServices
            .OrderBy(bs => bs.ServiceId)
            .Select(bs => new BookedService(
                bs.ServiceId,
                bs.DiagnosticService.ServiceName))
            .ToList();

        return new BookingDetails(
            booking.BookingId,
            booking.PatientId,
            booking.Patient.PatientCode,
            booking.Patient.FullName,
            booking.BookingDate,
            booking.CreatedAt,
            services);
    }
}
