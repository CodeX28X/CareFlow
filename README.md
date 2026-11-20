# CareFlow — Diagnostic Booking Management

**ITSD81 · Desktop Application Development · Performance Task 4**  
**Team:** Stephen Joseph D. Rubin and Gil Scudge B. Ybañez

CareFlow is a desktop application for outpatient diagnostic staff to check service availability, confirm bookings for **one to three distinct services**, and review saved bookings. It focuses on correct booking validation, daily capacity accounting, and all-or-nothing database transactions. Patient and service information is fictional seed data.

## Features

- **Service Availability:** Choose a date and view each service's daily capacity, confirmed bookings, and remaining slots. Refresh to retrieve updated information.
- **New Booking:** Choose an existing patient, a valid date, and 1–3 distinct services. Confirmation rechecks availability and either saves the entire booking or rejects it.
- **Booking History:** Review confirmed bookings, including patient, date, and associated diagnostic services. Records survive application restart.
- **Failure handling:** Validation and database failures do not create partial bookings. Errors are reported through the interface.

## Technology and architecture

- **C# / .NET 10**, pinned SDK `10.0.400` (`global.json`)
- **Avalonia UI 12** with XAML and MVVM
- **Entity Framework Core 10** with **Npgsql / PostgreSQL**
- **Dependency injection**, asynchronous database operations, **xUnit** testing

| Project | Responsibility |
|---|---|
| `src/CareFlow.Desktop` | Avalonia views, view models, navigation and UI feedback |
| `src/CareFlow.Application` | Booking requests, validation, results and service contracts |
| `src/CareFlow.Domain` | Core booking, patient and diagnostic service entities |
| `src/CareFlow.Infrastructure` | EF Core persistence, migrations, seeds and transactional booking services |
| `tests/CareFlow.Tests` | Unit and PostgreSQL integration tests |

**Transaction strategy:** The booking service checks validity, begins a PostgreSQL transaction, locks requested service rows in a stable order, rechecks availability, and saves the booking with its service associations before committing. An unsuccessful operation does not commit partial data. Remaining capacity is calculated from saved bookings rather than stored as a separately maintained counter. Booking dates are validated against the current date in the **Asia/Manila** time zone.

## Requirements

1. Windows with Visual Studio or PowerShell and the **.NET 10 SDK** compatible with `global.json` (SDK `10.0.400`).
2. A running **PostgreSQL** server (development environment uses PostgreSQL 18 on `localhost:5432`).
3. A PostgreSQL role allowed to connect to, create objects in, and modify the CareFlow databases. Database schema creation permissions are required for migrations.
4. Network access to restore NuGet dependencies on first build.

## Database initialization

Create separate empty PostgreSQL databases named **`careflow_dev`** for the desktop application and **`careflow_test`** for integration tests. For example, connect using `psql` with a sufficiently privileged PostgreSQL role and run:

```sql
create database careflow_dev;
create database careflow_test;
```

If the databases already exist, **do not recreate or delete them**. The database account used by the application must have the appropriate permissions for the target database's `public` schema and tables.

CareFlow uses three separate environment variables:

| Environment variable | Used by | Database |
|---|---|---|
| `CAREFLOW_DB_CONNECTION` | EF Core design-time migrations | `careflow_dev` |
| `CAREFLOW_CONNECTION_STRING` | Running the desktop application | `careflow_dev` |
| `CAREFLOW_TEST_DB_CONNECTION` | PostgreSQL integration tests | `careflow_test` |

For a **temporary PowerShell session**, set the variables without putting passwords into source files or Git history:

```powershell
$pgUser = Read-Host "PostgreSQL username"
$securePassword = Read-Host "PostgreSQL password" -AsSecureString
$credential = [System.Net.NetworkCredential]::new($pgUser, $securePassword)

$devBuilder = [Npgsql.NpgsqlConnectionStringBuilder]::new()
```

If `Npgsql` is not loaded in your standalone PowerShell session, use this self-contained alternative instead:

```powershell
$pgUser = Read-Host "PostgreSQL username"
$securePassword = Read-Host "PostgreSQL password" -AsSecureString
$credential = [System.Net.NetworkCredential]::new($pgUser, $securePassword)
$builder = [System.Data.Common.DbConnectionStringBuilder]::new()
$builder["Host"] = "localhost"
$builder["Port"] = 5432
$builder["Username"] = $pgUser
$builder["Password"] = $credential.Password
$builder["Database"] = "careflow_dev"
$env:CAREFLOW_DB_CONNECTION = $builder.ConnectionString
$env:CAREFLOW_CONNECTION_STRING = $builder.ConnectionString
$builder["Database"] = "careflow_test"
$env:CAREFLOW_TEST_DB_CONNECTION = $builder.ConnectionString
Remove-Variable securePassword, credential, builder
```

Keep this terminal open for the next commands. Variables set using `$env:` are inherited by processes launched from this terminal only. If launching directly from Visual Studio, configure the app's environment in its launch environment or via Windows User Environment Variables and restart Visual Studio. **Do not commit any connection strings or credentials.**

From the repository root, restore the local EF Core tool and apply the initial migration:

```powershell
dotnet tool restore
dotnet ef database update --project src/CareFlow.Infrastructure --startup-project src/CareFlow.Infrastructure
```

The initial EF Core migration (`20261116152157_InitialCreate`) creates **Patients**, **DiagnosticServices**, **Bookings**, and **BookingServices**, plus the EF migrations history table. It also seeds **8 fictional patients** and **4 diagnostic services**:

| ID | Diagnostic service | Daily capacity |
|---:|---|---:|
| 1 | Complete Blood Count | 10 |
| 2 | Urinalysis | 8 |
| 3 | Chest X-Ray | 5 |
| 4 | Electrocardiogram (ECG) | 3 |

> Migration commands use `CAREFLOW_DB_CONNECTION`; the desktop uses `CAREFLOW_CONNECTION_STRING`. Both should reference `careflow_dev` for ordinary development.

## Build and run

Run from the repository root:

```powershell
dotnet restore CareFlow.slnx
dotnet build CareFlow.slnx
dotnet run --project src/CareFlow.Desktop/CareFlow.Desktop.csproj
```

If the configuration-required screen appears, check that `CAREFLOW_CONNECTION_STRING` is set **in the environment launching the app**. Check the PostgreSQL service, database name, account permissions, and password if a database connection fails.

## Automated testing

Integration tests require a **separate database named `careflow_test`**, specified by `CAREFLOW_TEST_DB_CONNECTION`. The integration test fixture verifies that database name to avoid running test cleanup against the development database. Never point it at `careflow_dev`.

```powershell
dotnet test CareFlow.slnx
```

Run only the rollback case:

```powershell
dotnet test CareFlow.slnx --filter "FullyQualifiedName~DatabaseFailure_RollsBackEntireBooking"
```

**Recorded verification:** The development team reported a successful solution build and **20/20 automated tests passing**, including PostgreSQL integration tests. Manual acceptance testing passed **AC-01 through AC-06**, including a two-instance stale-availability scenario. See [`docs/Verification-Checklist.md`](docs/Verification-Checklist.md) for individual evidence and checks.

## Relational design

See [`docs/ERD.png`](docs/ERD.png) and [`docs/ERD.md`](docs/ERD.md). `BookingServices` is the junction table for the many-to-many relationship between bookings and diagnostic services. Its composite primary key prevents a service from appearing twice **in the same booking**.

## Known scope limitations

CareFlow intentionally does not include patient registration, diagnostic-service administration, authentication, appointment time slots, billing, cancellation, or rescheduling. Confirmed bookings are permanent for this assignment. The system does **not** prohibit a patient from making two separate bookings of the same service on the same date; the instructor's distinct-service rule applies within a **single** booking request, and each confirmed booking consumes capacity.

## Academic integrity and acknowledgments

This application was developed as pair work by **Stephen Joseph D. Rubin** and **Gil Scudge B. Ybañez**. AI-assisted development (ChatGPT) was used for architecture planning, implementation guidance, debugging, code review, UI correction, test planning, and documentation drafting. The team is responsible for reviewing, understanding, testing, and submitting the final solution.
