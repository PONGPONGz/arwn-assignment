# Feature Plan: Appointment + RabbitMQ Messaging (C1, C2, C3)

> **Plan ID:** 003
> **Status:** Done
> **Created:** 2026-02-13
> **Last Updated:** 2026-02-13

---

## Summary

Add a Create Appointment API endpoint with duplicate booking prevention (DB unique constraint) and publish an `AppointmentCreated` event to RabbitMQ on success. Backend API only — no frontend, no List endpoint.

---

## Goals

- C1: Create Appointment with TenantId, BranchId, PatientId, StartAt, CreatedAt
- C2: Prevent exact duplicate booking (same Tenant + Patient + Branch + StartAt) — concurrency-safe via DB unique constraint + friendly error
- C3: Publish `AppointmentCreated` event to RabbitMQ with TenantId in payload (no consumer required)

---

## Non-Goals (Out of Scope)

- List / Get / Update / Delete appointment endpoints
- Appointment status, duration, notes, soft delete
- Frontend appointment UI
- RabbitMQ consumer
- Overlapping time-window conflict detection (only exact duplicate prevented)

---

## Dependencies

- Requires `001-patient-thin-slice.md` to be Done (Patient + Branch entities, DbContext, middleware, test infrastructure)

---

## Design Decisions

| Decision | Options Considered | Chosen | Rationale |
|----------|--------------------|--------|-----------|
| Entity fields | Full (Status, Duration, Notes, IsDeleted, UpdatedAt) vs Minimal (spec only) | Minimal: Id, TenantId, BranchId, PatientId, StartAt, CreatedAt | YAGNI — only what the spec requires |
| Duplicate rule | Same Patient+StartAt (any branch) vs Same Patient+Branch+StartAt | Same Patient+Branch+StartAt per tenant | Matches spec exactly: "Same PatientId + same StartAt + same BranchId" |
| Concurrency safety | App-level check only vs DB unique constraint vs Both | Both: app-level check for friendly error + DB unique index as safety net | App check gives `DuplicateBookingException` → 409; DB index catches races |
| RabbitMQ topology | Direct exchange vs Fanout exchange vs Topic exchange | Fanout exchange `clinic-pos-events` | Simplest; no routing needed; future consumers just bind a queue |
| Event failure handling | Fail request vs Fire-and-forget with logging | Fire-and-forget with logging | Event publishing should not break the API request for v1 |
| RabbitMQ in tests | Real RabbitMQ container vs Stub/mock | Stub `IEventPublisher` (in-memory spy) | No external dependency in unit tests; spy captures events for assertions |

---

## Deliverables

- [x] `Appointment` entity implementing `ITenantScoped`
- [x] DbContext registration with query filter + unique composite index
- [x] EF Core migration `AddAppointmentTable`
- [x] `CreateAppointmentRequest` DTO + `AppointmentResponse` DTO
- [x] `CreateAppointmentRequestValidator` (FluentValidation)
- [x] `DuplicateBookingException` + middleware handler → 409
- [x] `IAppointmentService` + `AppointmentService`
- [x] `AppointmentsController` — POST `/api/v1/appointments`
- [x] `IEventPublisher` abstraction + `RabbitMqEventPublisher`
- [x] RabbitMQ NuGet package, config, and docker-compose wiring
- [x] Tests: create, duplicate → 409, tenant isolation, validation, event published

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/v1/appointments` | Create an appointment. Body: `{ branchId, patientId, startAt }`. Returns 201 + appointment JSON. |

### Success Response (201)

```json
{
  "id": "uuid",
  "branchId": "uuid",
  "patientId": "uuid",
  "startAt": "2026-03-01T10:00:00Z",
  "createdAt": "2026-02-13T12:00:00Z"
}