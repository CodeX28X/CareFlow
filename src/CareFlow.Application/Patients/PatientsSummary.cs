
namespace CareFlow.Application.Patients;

public sealed record PatientSummary(
    int PatientId,
    string PatientCode,
    string FullName);
