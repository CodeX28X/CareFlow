using CareFlow.Application.Bookings;
using CareFlow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareFlow.Tests.Integration;

[Collection("CareFlow PostgreSQL")]
[Trait("Category", "Integration")]
public sealed class BookingIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public BookingIntegrationTests(PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task MultiServiceBooking_IsSavedCompletely()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();

        try
        {
            using var services = _database.CreateServices();

            var bookingService =
                services.GetRequiredService<IBookingService>();

            var result = await bookingService.ConfirmBookingAsync(
                new CreateBookingRequest(
                    1, date, new[] { 1, 2, 3 }));

            Assert.True(result.IsSuccess, result.Message);
            Assert.NotNull(result.BookingId);

            var query =
                services.GetRequiredService<IBookingQueryService>();

            var saved = await query.GetBookingByIdAsync(
                result.BookingId.Value);

            Assert.NotNull(saved);
            Assert.Equal(1, saved.PatientId);
            Assert.Equal(date, saved.BookingDate);
            Assert.Equal(3, saved.Services.Count);

            await using var db = _database.CreateDbContext();

            Assert.Equal(1, await db.Bookings
                .CountAsync(b => b.BookingDate == date));

            Assert.Equal(3, await db.BookingServices
                .CountAsync(bs => bs.Booking.BookingDate == date));
        }
        finally
        {
            await _database.CleanupDateAsync(date);
        }
    }

    [Fact]
    public async Task InvalidPatientAndService_CreateNoBooking()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();

        try
        {
            using var services = _database.CreateServices();

            var bookingService =
                services.GetRequiredService<IBookingService>();

            var invalidPatient =
                await bookingService.ConfirmBookingAsync(
                    new CreateBookingRequest(
                        int.MaxValue, date, new[] { 1 }));

            Assert.False(invalidPatient.IsSuccess);
            Assert.Equal(
                BookingFailureCode.InvalidPatient,
                invalidPatient.FailureCode);

            var invalidService =
                await bookingService.ConfirmBookingAsync(
                    new CreateBookingRequest(
                        1, date, new[] { 999999 }));

            Assert.False(invalidService.IsSuccess);
            Assert.Equal(
                BookingFailureCode.InvalidService,
                invalidService.FailureCode);

            await using var db = _database.CreateDbContext();

            Assert.Equal(0, await db.Bookings
                .CountAsync(b => b.BookingDate == date));
        }
        finally
        {
            await _database.CleanupDateAsync(date);
        }
    }

    [Fact]
    public async Task FullService_RejectsWholeBooking()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();

        try
        {
            using var services = _database.CreateServices();

            var bookingService =
                services.GetRequiredService<IBookingService>();

            // ECG has daily capacity of three.
            for (int i = 0; i < 3; i++)
            {
                var setup = await bookingService.ConfirmBookingAsync(
                    new CreateBookingRequest(
                        1, date, new[] { 4 }));

                Assert.True(setup.IsSuccess, setup.Message);
            }

            // CBC is available, but ECG is full.
            var rejected = await bookingService.ConfirmBookingAsync(
                new CreateBookingRequest(
                    1, date, new[] { 1, 4 }));

            Assert.False(rejected.IsSuccess);
            Assert.Equal(
                BookingFailureCode.CapacityUnavailable,
                rejected.FailureCode);

            await using var db = _database.CreateDbContext();

            Assert.Equal(3, await db.Bookings
                .CountAsync(b => b.BookingDate == date));

            Assert.Equal(0, await db.BookingServices
                .CountAsync(bs =>
                    bs.Booking.BookingDate == date &&
                    bs.ServiceId == 1));
        }
        finally
        {
            await _database.CleanupDateAsync(date);
        }
    }

    [Fact]
    public async Task StaleAvailability_CannotCauseOverbooking()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();

        try
        {
            using var services = _database.CreateServices();

            var availability =
                services.GetRequiredService<IAvailabilityService>();

            var bookingService =
                services.GetRequiredService<IBookingService>();

            var before = await availability.GetAvailabilityAsync(date);

            var ecgBefore = Assert.Single(
                before.Where(s => s.ServiceId == 4));

            Assert.Equal(3, ecgBefore.RemainingCapacity);

            // Other bookings consume the displayed availability.
            for (int i = 0; i < 3; i++)
            {
                var setup = await bookingService.ConfirmBookingAsync(
                    new CreateBookingRequest(
                        1, date, new[] { 4 }));

                Assert.True(setup.IsSuccess, setup.Message);
            }

            // Staff attempts to confirm using an outdated display.
            var result = await bookingService.ConfirmBookingAsync(
                new CreateBookingRequest(
                    1, date, new[] { 4 }));

            Assert.False(result.IsSuccess);
            Assert.Equal(
                BookingFailureCode.CapacityUnavailable,
                result.FailureCode);

            var after = await availability.GetAvailabilityAsync(date);

            var ecgAfter = Assert.Single(
                after.Where(s => s.ServiceId == 4));

            Assert.Equal(0, ecgAfter.RemainingCapacity);
        }
        finally
        {
            await _database.CleanupDateAsync(date);
        }
    }

    [Fact]
    public async Task ConcurrentRequests_CannotBothTakeFinalSlot()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();

        try
        {
            using var services = _database.CreateServices();

            var bookingService =
                services.GetRequiredService<IBookingService>();

            // Leave exactly one ECG slot available.
            for (int i = 0; i < 2; i++)
            {
                var setup = await bookingService.ConfirmBookingAsync(
                    new CreateBookingRequest(
                        1, date, new[] { 4 }));

                Assert.True(setup.IsSuccess, setup.Message);
            }

            var request = new CreateBookingRequest(
                1, date, new[] { 4 });

            Task<BookingResult> first =
                bookingService.ConfirmBookingAsync(request);

            Task<BookingResult> second =
                bookingService.ConfirmBookingAsync(request);

            var results = await Task.WhenAll(first, second);

            Assert.Equal(1, results.Count(r => r.IsSuccess));

            Assert.Equal(
                1,
                results.Count(r =>
                    r.FailureCode ==
                    BookingFailureCode.CapacityUnavailable));

            await using var db = _database.CreateDbContext();

            Assert.Equal(3, await db.BookingServices
                .CountAsync(bs =>
                    bs.Booking.BookingDate == date &&
                    bs.ServiceId == 4));
        }
        finally
        {
            await _database.CleanupDateAsync(date);
        }
    }

    [Fact]
    public async Task ConfirmedBooking_CanBeRetrievedUsingNewContext()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();

        try
        {
            int bookingId;

            using (var firstServices = _database.CreateServices())
            {
                var bookingService =
                    firstServices.GetRequiredService<IBookingService>();

                var result = await bookingService.ConfirmBookingAsync(
                    new CreateBookingRequest(
                        1, date, new[] { 1, 2 }));

                Assert.True(result.IsSuccess, result.Message);
                bookingId = result.BookingId!.Value;
            }

            // A fresh service provider opens new DbContexts.
            using (var secondServices = _database.CreateServices())
            {
                var query =
                    secondServices.GetRequiredService<IBookingQueryService>();

                var saved = await query.GetBookingByIdAsync(bookingId);

                Assert.NotNull(saved);
                Assert.Equal(date, saved.BookingDate);
                Assert.Equal(2, saved.Services.Count);

                var allBookings = await query.GetBookingsAsync();

                Assert.Contains(
                    allBookings,
                    b => b.BookingId == bookingId);
            }
        }
        finally
        {
            await _database.CleanupDateAsync(date);
        }
    }

    [Fact]
    public async Task DatabaseFailure_RollsBackEntireBooking()
    {
        DateOnly date = PostgreSqlTestDatabase.NewTestDate();
        bool triggerInstalled = false;

        try
        {
            await using (var db = _database.CreateDbContext())
            {
                // The trigger raises a PostgreSQL exception only
                // for bookings matching this test date.
                string dateLiteral = date.ToString("yyyy-MM-dd");

                string functionSql = $$"""
                    CREATE OR REPLACE FUNCTION
                    public.careflow_test_reject_booking_service()
                    RETURNS trigger
                    LANGUAGE plpgsql
                    AS $function$
                    BEGIN
                        IF EXISTS (
                            SELECT 1
                            FROM public."Bookings"
                            WHERE "BookingId" = NEW."BookingId"
                            AND "BookingDate" = DATE '{{dateLiteral}}'
                        ) THEN
                            RAISE EXCEPTION
                            'Simulated booking-service insert failure';
                        END IF;

                        RETURN NEW;
                    END;
                    $function$;
                    """;

                await db.Database.ExecuteSqlRawAsync(functionSql);

                await db.Database.ExecuteSqlRawAsync("""
                    DROP TRIGGER IF EXISTS
                    careflow_test_reject_insert
                    ON public."BookingServices";
                    """);

                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER careflow_test_reject_insert
                    BEFORE INSERT ON public."BookingServices"
                    FOR EACH ROW
                    EXECUTE FUNCTION
                    public.careflow_test_reject_booking_service();
                    """);

                triggerInstalled = true;
            }

            using var services = _database.CreateServices();

            var bookingService =
                services.GetRequiredService<IBookingService>();

            var result = await bookingService.ConfirmBookingAsync(
                new CreateBookingRequest(
                    1, date, new[] { 1, 2 }));

            Assert.False(result.IsSuccess);
            Assert.Equal(
                BookingFailureCode.PersistenceFailure,
                result.FailureCode);

            await using var check = _database.CreateDbContext();

            Assert.Equal(0, await check.Bookings
                .CountAsync(b => b.BookingDate == date));

            Assert.Equal(0, await check.BookingServices
                .CountAsync(bs => bs.Booking.BookingDate == date));
        }
        finally
        {
            // Always attempt to remove the test trigger,
            // including when its creation was interrupted.
            await using var db = _database.CreateDbContext();

            try
            {
                await db.Database.ExecuteSqlRawAsync("""
                    DROP TRIGGER IF EXISTS
                    careflow_test_reject_insert
                    ON public."BookingServices";
                    """);

                await db.Database.ExecuteSqlRawAsync("""
                    DROP FUNCTION IF EXISTS
                    public.careflow_test_reject_booking_service();
                    """);
            }
            finally
            {
                await _database.CleanupDateAsync(date);
            }
        }
    }
}