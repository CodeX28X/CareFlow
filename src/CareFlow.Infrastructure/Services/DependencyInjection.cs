
using CareFlow.Application.Bookings;
using CareFlow.Application.Interfaces;
using CareFlow.Infrastructure.Persistence;
using CareFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCareFlowInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "A PostgreSQL connection string is required.",
                nameof(connectionString));
        }

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<BookingValidator>();

        services.AddDbContextFactory<CareFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddTransient<IPatientQueryService, PatientQueryService>();
        services.AddTransient<IAvailabilityService, AvailabilityService>();
        services.AddTransient<IBookingQueryService, BookingQueryService>();
        services.AddTransient<IBookingService, BookingService>();

        return services;
    }
}
