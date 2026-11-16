
using System.Data;
using CareFlow.Application.Bookings;
using CareFlow.Application.Interfaces;
using CareFlow.Domain.Entities;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using BookingServiceEntity = CareFlow.Domain.Entities.BookingService;

namespace CareFlow.Infrastructure.Services;

public sealed class BookingService : IBookingService
{
    private readonly IDbContextFactory<CareFlowDbContext> _contextFactory;
    private readonly BookingValidator _validator;

    public BookingService(
        IDbContextFactory<CareFlowDbContext> contextFactory,
        BookingValidator validator)
    {
        _contextFactory = contextFactory;
        _validator = validator;
    }

    public async Task<BookingResult> ConfirmBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(request);

        if (validation is not null)
            return validation;

        int[] serviceIds = request.ServiceIds.OrderBy(id => id).ToArray();

        try
        {
            await using var db =
                await _contextFactory.CreateDbContextAsync(cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            foreach (int serviceId in serviceIds)
            {
                bool serviceExists = await LockServiceAsync(
                    db, serviceId, cancellationToken);

                if (!serviceExists)
                {
                    return BookingResult.Failure(
                        BookingFailureCode.InvalidService,
                        "One or more selected diagnostic services do not exist.");
                }
            }

            // Recheck date and request validity inside the transaction.
            validation = _validator.Validate(request);

            if (validation is not null)
                return validation;

            bool patientExists = await db.Patients
                .AsNoTracking()
                .AnyAsync(
                    p => p.PatientId == request.PatientId,
                    cancellationToken);

            if (!patientExists)
            {
                return BookingResult.Failure(
                    BookingFailureCode.InvalidPatient,
                    "The selected patient does not exist.");
            }

            foreach (int serviceId in serviceIds)
            {
                int capacity = await db.DiagnosticServices
                    .AsNoTracking()
                    .Where(s => s.ServiceId == serviceId)
                    .Select(s => s.DailyCapacity)
                    .SingleAsync(cancellationToken);

                int confirmedCount = await db.BookingServices
                    .AsNoTracking()
                    .CountAsync(
                        bs => bs.ServiceId == serviceId &&
                              bs.Booking.BookingDate == request.BookingDate,
                        cancellationToken);

                if (confirmedCount >= capacity)
                {
                    return BookingResult.Failure(
                        BookingFailureCode.CapacityUnavailable,
                        "A selected service has reached its daily capacity. " +
                        "No booking was created.");
                }
            }

            var booking = new Booking
            {
                PatientId = request.PatientId,
                BookingDate = request.BookingDate,
                BookingServices = serviceIds
                    .Select(id => new BookingServiceEntity
                    {
                        ServiceId = id
                    })
                    .ToList()
            };

            db.Bookings.Add(booking);

            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return BookingResult.Success(booking.BookingId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return BookingResult.Failure(
                BookingFailureCode.PersistenceFailure,
                "The booking could not be saved. No confirmation was issued.");
        }
        catch (NpgsqlException)
        {
            return BookingResult.Failure(
                BookingFailureCode.PersistenceFailure,
                "A database error prevented booking confirmation.");
        }
    }

    private static async Task<bool> LockServiceAsync(
        CareFlowDbContext db,
        int serviceId,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.Transaction =
            db.Database.CurrentTransaction!.GetDbTransaction();

        command.CommandText = """
            SELECT "ServiceId"
            FROM "DiagnosticServices"
            WHERE "ServiceId" = @serviceId
            FOR UPDATE
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "serviceId";
        parameter.DbType = DbType.Int32;
        parameter.Value = serviceId;

        command.Parameters.Add(parameter);

        object? result =
            await command.ExecuteScalarAsync(cancellationToken);

        return result is not null && result != DBNull.Value;
    }
}
