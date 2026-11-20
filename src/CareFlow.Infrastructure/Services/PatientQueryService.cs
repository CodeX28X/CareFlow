
using CareFlow.Application.Interfaces;
using CareFlow.Application.Patients;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Services;

public sealed class PatientQueryService : IPatientQueryService
{
    private readonly IDbContextFactory<CareFlowDbContext> _contextFactory;

    public PatientQueryService(
        IDbContextFactory<CareFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<PatientSummary>> GetPatientsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Patients
            .AsNoTracking()
            .OrderBy(p => p.PatientCode)
            .Select(p => new PatientSummary(
                p.PatientId,
                p.PatientCode,
                p.FullName))
            .ToListAsync(cancellationToken);
    }
}
