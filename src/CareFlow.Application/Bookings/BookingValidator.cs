
namespace CareFlow.Application.Bookings;

public sealed class BookingValidator
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _hospitalTimeZone;

    public BookingValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));

        _hospitalTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
    }

    public BookingResult? Validate(CreateBookingRequest? request)
    {
        if (request is null)
        {
            return BookingResult.Failure(
                BookingFailureCode.InvalidRequest,
                "A booking request is required.");
        }

        if (request.PatientId <= 0)
        {
            return BookingResult.Failure(
                BookingFailureCode.InvalidPatient,
                "Please select a valid patient.");
        }

        if (request.ServiceIds is null ||
            request.ServiceIds.Count < 1 ||
            request.ServiceIds.Count > 3)
        {
            return BookingResult.Failure(
                BookingFailureCode.InvalidServiceCount,
                "Select between one and three diagnostic services.");
        }

        if (request.ServiceIds.Any(id => id <= 0))
        {
            return BookingResult.Failure(
                BookingFailureCode.InvalidService,
                "The selected services contain an invalid identifier.");
        }

        if (request.ServiceIds.Distinct().Count()
            != request.ServiceIds.Count)
        {
            return BookingResult.Failure(
                BookingFailureCode.DuplicateServices,
                "The same diagnostic service cannot be selected twice.");
        }

        DateTimeOffset hospitalNow =
            TimeZoneInfo.ConvertTime(
                _timeProvider.GetUtcNow(),
                _hospitalTimeZone);

        DateOnly today =
            DateOnly.FromDateTime(hospitalNow.DateTime);

        if (request.BookingDate < today)
        {
            return BookingResult.Failure(
                BookingFailureCode.InvalidDate,
                "The booking date cannot be earlier than today.");
        }

        return null;
    }
}
