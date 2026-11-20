# CareFlow — Acceptance Verification Checklist

**Performance Task:** ITSD81 Performance Task 4 · CareFlow — Diagnostic Booking Management  
**Team:** Stephen Joseph D. Rubin; Gil Scudge B. Ybañez  
**Verification basis:** Developer-reported UI/manual test results, PostgreSQL checks and console test output; evidence screenshots should be copied into `docs/screenshots/` before submission.

## Acceptance criteria (instructor-defined)

| ID | Scenario | How verified | Recorded result |
|---|---|---|---|
| **AC-01** | Service availability for selected date | Checked seeded capacities and updated per-date confirmed/remaining counts; date switching and refresh | **PASS** |
| **AC-02** | Multi-service booking saved as one booking | Confirmed one-, two-, and three-service bookings; checked history and associated services | **PASS** |
| **AC-03** | Full/invalid requests rejected without partial records | Tested zero/four services, missing patient, past date, fully booked service, and user-facing feedback | **PASS** |
| **AC-04** | Recheck stale availability | In two desktop instances, both targeted the final ECG slot; first confirmation succeeded, second was rejected because ECG became full. PostgreSQL integration tests also cover stale availability and concurrency | **PASS** |
| **AC-05** | Rollback if a database operation fails | PostgreSQL integration test `DatabaseFailure_RollsBackEntireBooking`; full automated suite passed with the test configured against `careflow_test` | **PASS** |
| **AC-06** | Bookings survive application restart | Confirmed booking, closed and reopened the desktop application, retrieved associated patient/date/services in history | **PASS** |

## Detailed manual test record

| Group | Test IDs | Recorded outcome |
|---|---|---|
| Navigation and data loading | UI-01–UI-05 | 5/5 passed |
| Availability | AV-01–AV-04 | 4/4 passed |
| New Booking | BK-01–BK-10 | 10/10 passed |
| Booking History | HI-01–HI-04 | 4/4 passed |
| Transaction safety | TX-01, TX-02 | TX-01 manually passed; TX-02 covered by passing integration suite |

**Latest developer-reported CLI results:**

```text
Build succeeded in 2.6s
Test summary: total: 20, failed: 0, succeeded: 20, skipped: 0
```

These are recorded from the developer's local execution, not from a build run by the documentation author. Attach a screenshot of the actual terminal result as submission evidence.

## Mandatory business-rule coverage

| Rule | Enforcement / evidence |
|---|---|
| BR-01 | Patient must exist; missing/invalid patient rejected |
| BR-02 | One to three distinct services required |
| BR-03 | Booking date must not be before the current Asia/Manila date |
| BR-04 | Each selected service uses one unit of daily capacity |
| BR-05 | Confirmation prevents confirmed count from exceeding daily service capacity |
| BR-06 | Repeating a service in a single request is invalid; composite key also prevents duplicate junction rows within a booking |
| BR-07 | Booking and association inserts are committed together in a PostgreSQL transaction |
| BR-08 | Actual availability is rechecked during confirmation, not trusted from the screen |
| BR-09 | EF Core PostgreSQL persistence; booking survives application restart |
| BR-10 | UI communicates unsuccessful requests and database failures |

## Evidence to capture before submission

- [ ] `docs/screenshots/01-availability.png` — date selected; capacity, confirmed, remaining visible
- [ ] `docs/screenshots/02-new-booking.png` — existing patient and multiple services selected
- [ ] `docs/screenshots/03-booking-success.png` — confirmed booking feedback (no sensitive information)
- [ ] `docs/screenshots/04-booking-history.png` — persisted booking with all associated services
- [ ] `docs/screenshots/05-stale-availability.png` — second instance rejected when final ECG slot consumed
- [ ] `docs/screenshots/06-test-results.png` — full 20/20 automated test results
- [ ] Check the merged `main` branch with `dotnet build CareFlow.slnx` and `dotnet test CareFlow.slnx`
- [ ] Confirm README and ERD match the actual submitted source
- [ ] Confirm no database passwords or real patient information are committed

**Submission status:** All six acceptance scenarios are reported as passed. Screenshots and final merged-branch re-verification must still be attached/performed before declaring the submission bundle complete.
