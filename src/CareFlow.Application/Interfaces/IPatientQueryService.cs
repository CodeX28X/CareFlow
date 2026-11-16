
using CareFlow.Application.Patients;

namespace CareFlow.Application.Interfaces;

public interface IPatientQueryService
{
    Task<IReadOnlyList<PatientSummary>> GetPatientsAsync(
        CancellationToken cancellationToken = default);
}
