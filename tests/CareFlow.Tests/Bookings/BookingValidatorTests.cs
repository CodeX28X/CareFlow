
using CareFlow.Application.Bookings;
using Xunit;

namespace CareFlow.Tests.Bookings;

public sealed class BookingValidatorTests
{
    private static readonly DateOnly Today =
        new(2026, 11, 17);

    private static BookingValidator CreateValidator()
    {
        // November 16 at 16:30 UTC is November 17
        // at 00:30 in the Philippines.
        var clock = new FixedTimeProvider(
            new DateTimeOffset(2026, 11, 16, 16, 30, 0,
                TimeSpan.Zero));

        return new BookingValidator(clock);
    }

    [Fact]
    public void NullRequest_IsRejected()
    {
        var result = CreateValidator().Validate(null);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.InvalidRequest,
            result.FailureCode);
    }

    [Fact]
    public void InvalidPatientId_IsRejected()
    {
        var request = new CreateBookingRequest(
            0, Today, new[] { 1 });

        var result = CreateValidator().Validate(request);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.InvalidPatient,
            result.FailureCode);
    }

    [Fact]
    public void NoServices_IsRejected()
    {
        var request = new CreateBookingRequest(
            1, Today, Array.Empty<int>());

        var result = CreateValidator().Validate(request);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.InvalidServiceCount,
            result.FailureCode);
    }

    [Fact]
    public void MoreThanThreeServices_IsRejected()
    {
        var request = new CreateBookingRequest(
            1, Today, new[] { 1, 2, 3, 4 });

        var result = CreateValidator().Validate(request);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.InvalidServiceCount,
            result.FailureCode);
    }

    [Fact]
    public void DuplicateServices_AreRejected()
    {
        var request = new CreateBookingRequest(
            1, Today, new[] { 1, 1 });

        var result = CreateValidator().Validate(request);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.DuplicateServices,
            result.FailureCode);
    }

    [Fact]
    public void InvalidServiceId_IsRejected()
    {
        var request = new CreateBookingRequest(
            1, Today, new[] { -1 });

        var result = CreateValidator().Validate(request);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.InvalidService,
            result.FailureCode);
    }

    [Fact]
    public void PastDate_IsRejected()
    {
        var request = new CreateBookingRequest(
            1, Today.AddDays(-1), new[] { 1 });

        var result = CreateValidator().Validate(request);

        Assert.NotNull(result);
        Assert.Equal(
            BookingFailureCode.InvalidDate,
            result.FailureCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ValidServiceCounts_AreAccepted(int serviceCount)
    {
        var request = new CreateBookingRequest(
            1,
            Today,
            Enumerable.Range(1, serviceCount).ToArray());

        var result = CreateValidator().Validate(request);

        Assert.Null(result);
    }

    [Fact]
    public void TodayInManila_IsAccepted()
    {
        var request = new CreateBookingRequest(
            1, Today, new[] { 1 });

        Assert.Null(CreateValidator().Validate(request));
    }

    [Fact]
    public void Tomorrow_IsAccepted()
    {
        var request = new CreateBookingRequest(
            1, Today.AddDays(1), new[] { 1 });

        Assert.Null(CreateValidator().Validate(request));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
