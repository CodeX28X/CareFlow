
using CareFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Persistence;

public static class SeedData
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiagnosticService>().HasData(
            new DiagnosticService
            {
                ServiceId = 1,
                ServiceName = "Complete Blood Count",
                DailyCapacity = 10
            },
            new DiagnosticService
            {
                ServiceId = 2,
                ServiceName = "Urinalysis",
                DailyCapacity = 8
            },
            new DiagnosticService
            {
                ServiceId = 3,
                ServiceName = "Chest X-Ray",
                DailyCapacity = 5
            },
            new DiagnosticService
            {
                ServiceId = 4,
                ServiceName = "Electrocardiogram (ECG)",
                DailyCapacity = 3
            }
        );

        modelBuilder.Entity<Patient>().HasData(
            new Patient { PatientId = 1, PatientCode = "P0001", FullName = "Alex Reyes" },
            new Patient { PatientId = 2, PatientCode = "P0002", FullName = "Jamie Cruz" },
            new Patient { PatientId = 3, PatientCode = "P0003", FullName = "Taylor Santos" },
            new Patient { PatientId = 4, PatientCode = "P0004", FullName = "Morgan Lim" },
            new Patient { PatientId = 5, PatientCode = "P0005", FullName = "Casey Navarro" },
            new Patient { PatientId = 6, PatientCode = "P0006", FullName = "Robin Garcia" },
            new Patient { PatientId = 7, PatientCode = "P0007", FullName = "Sam Villanueva" },
            new Patient { PatientId = 8, PatientCode = "P0008", FullName = "Jordan Mendoza" }
        );
    }
}
