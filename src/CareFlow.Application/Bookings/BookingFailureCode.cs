
namespace CareFlow.Application.Bookings;

public enum BookingFailureCode
{
    None = 0,
    InvalidRequest,
    InvalidPatient,
    InvalidDate,
    InvalidServiceCount,
    DuplicateServices,
    InvalidService,
    CapacityUnavailable,
    PersistenceFailure
}
