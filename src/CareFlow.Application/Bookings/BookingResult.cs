
namespace CareFlow.Application.Bookings;

public sealed record BookingResult(
    bool IsSuccess,
    int? BookingId,
    BookingFailureCode FailureCode,
    string Message)
{
    public static BookingResult Success(int bookingId)
    {
        return new BookingResult(
            true,
            bookingId,
            BookingFailureCode.None,
            $"Booking #{bookingId} was confirmed successfully.");
    }

    public static BookingResult Failure(
        BookingFailureCode code,
        string message)
    {
        if (code == BookingFailureCode.None)
        {
            throw new ArgumentException(
                "A failure must have a valid failure code.",
                nameof(code));
        }

        return new BookingResult(
            false,
            null,
            code,
            message);
    }
}
