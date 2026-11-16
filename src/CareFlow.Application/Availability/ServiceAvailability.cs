
namespace CareFlow.Application.Availability;

public sealed record ServiceAvailability(
    int ServiceId,
    string ServiceName,
    int DailyCapacity,
    int ConfirmedCount,
    int RemainingCapacity)
{
    public bool IsAvailable => RemainingCapacity > 0;
}
