
using CareFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Persistence;

public class CareFlowDbContext : DbContext
{
    public CareFlowDbContext(
        DbContextOptions<CareFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices
        => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices
        => Set<BookingService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(p => p.PatientId);

            entity.Property(p => p.PatientCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(p => p.PatientCode)
                .IsUnique();

            entity.Property(p => p.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasMany(p => p.Bookings)
                .WithOne(b => b.Patient)
                .HasForeignKey(b => b.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiagnosticService>(entity =>
        {
            entity.HasKey(s => s.ServiceId);

            entity.Property(s => s.ServiceName)
                .IsRequired()
                .HasMaxLength(120);

            entity.HasIndex(s => s.ServiceName)
                .IsUnique();

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_DiagnosticServices_DailyCapacity",
                    "\"DailyCapacity\" > 0"));
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.BookingId);

            entity.Property(b => b.BookingDate)
                .IsRequired();

            entity.Property(b => b.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            entity.HasIndex(b => b.BookingDate);
            entity.HasIndex(b => b.PatientId);
        });

        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(bs => new
            {
                bs.BookingId,
                bs.ServiceId
            });

            entity.HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingServices)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(bs => bs.DiagnosticService)
                .WithMany(s => s.BookingServices)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(bs => bs.ServiceId);
        });

        SeedData.Configure(modelBuilder);
    }
}
