# Feature Plan: Patient Thin Slice (End-to-End)

> **Plan ID:** 001
> **Status:** Done
> **Created:** 2026-02-13
> **Last Updated:** 2026-02-13

---

## Summary

Build the first usable end-to-end flow — Create Patient and List Patients — while establishing all foundational infrastructure (Docker, .NET 10 API, Next.js, PostgreSQL, tenant middleware, migrations, seed data). Uses a hardcoded `X-Tenant-Id` header for tenant resolution (JWT auth layered later). Patient uses `PrimaryBranchId` FK for branch association.

---

## Goals

- Prove a working vertical slice: backend API + frontend UI + database
- Establish foundational infrastructure that all future features build on
- Enforce tenant isolation from day one via EF Core `HasQueryFilter`
- Deliver Create Patient and List Patients with validation and error handling

---

## Non-Goals (Out of Scope)

- JWT authentication / login / registration (next plan)
- User entity CRUD
- Patient edit / delete endpoints
- Advanced RBAC
- Redis caching or RabbitMQ messaging (services provisioned in Docker but unused)
- Heavy UI work or component libraries
- Schema-per-tenant or database-per-tenant

---

## Dependencies

- None — this is the first plan; everything is bootstrapped here

---

## Design Decisions

| Decision | Options Considered | Chosen | Rationale |
|----------|--------------------|--------|-----------|
| Patient name fields | FullName (single) vs FirstName + LastName (two) | FirstName + LastName | Per user requirement; more flexible for display/search |
| Tenant resolution | JWT claims vs X-Tenant-Id header | X-Tenant-Id header | Fastest path to working slice; `ITenantProvider` abstraction means JWT swap requires only new DI registration |
| Patient ↔ Branch | PrimaryBranchId FK vs PatientBranch mapping table | PrimaryBranchId FK (nullable) | Simplest model; mapping table is YAGNI until visit tracking is planned |
| Validation | DataAnnotations vs FluentValidation | FluentValidation | Cleaner separation, independently testable, richer rules |
| Plan scope | Single plan vs two (infra + feature) | Single plan | Infra + feature together since this is the first slice — no pre-existing infra to split from |
| Test approach | In-memory provider vs test containers | WebApplicationFactory + test PostgreSQL (or in-memory for speed) | Pragmatic for v1; validates real query filters |

---

## Deliverables

### Phase 1: Infrastructure Skeleton

- [x] `docker-compose.yml` with 5 services: PostgreSQL, Redis, RabbitMQ, backend, frontend
- [x] `.env.example` with all required environment variables
- [x] Root `README.md` with setup instructions
- [x] Backend project scaffolding (`src/backend/`): `.csproj` targeting `net10.0`, `Program.cs`, `appsettings.json`, `Dockerfile`
- [x] Frontend project scaffolding (`src/frontend/`): Next.js with TypeScript + App Router, `Dockerfile`

### Phase 2: Backend Core Infrastructure

- [x] Entity models: `Tenant`, `Branch`, `Patient`, `ITenantScoped` interface
- [x] `ClinicPosDbContext` with `HasQueryFilter` on all tenant-scoped entities
- [x] Composite unique index on Patient: `(TenantId, PhoneNumber)`
- [x] Soft delete query filter on Patient: `!IsDeleted && TenantId == _tenantId`
- [x] `ITenantProvider` + `HttpHeaderTenantProvider` (reads `X-Tenant-Id` header)
- [x] `TenantResolutionMiddleware` — 400 if header missing/invalid
- [x] `ExceptionHandlingMiddleware` — consistent JSON errors; handles unique constraint → 409, validation → 400
- [x] Initial EF Core migration: `CreateInitialSchema`
- [x] Seed data on startup: 1 Tenant ("Demo Clinic"), 2 Branches ("Main Branch", "Downtown Branch")

### Phase 3: Patient API

- [x] `CreatePatientRequest` DTO + `CreatePatientRequestValidator` (FluentValidation)
- [x] `PatientResponse` DTO
- [x] `IPatientService` + `PatientService` — `CreateAsync`, `ListAsync(branchId?)`
- [x] `PatientsController` — `POST /api/v1/patients`, `GET /api/v1/patients`
- [x] `BranchesController` — `GET /api/v1/branches` (for frontend dropdown)

### Phase 4: Frontend

- [x] API client (`lib/api.ts`) with `X-Tenant-Id` header from env var
- [x] TypeScript types matching backend DTOs
- [x] Patient list page (`/patients`) — table, branch filter dropdown, sorted by CreatedAt DESC
- [x] Create patient page (`/patients/new`) — form with validation, error display (409 duplicate, 400 validation)

### Phase 5: Testing & Verification

- [x] Backend test project: `src/backend/tests/ClinicPos.Api.Tests/` (xUnit + FluentAssertions)
- [x] Test: Tenant isolation — patients from Tenant A not visible to Tenant B
- [x] Test: Duplicate phone within same tenant → 409; same phone different tenant → 201
- [x] Test: Validation — missing required fields → 400
- [x] Frontend smoke test: list page renders, create form renders

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/v1/patients` | Create a patient. Body: `{ firstName, lastName, phoneNumber, primaryBranchId? }`. Returns 201 + patient JSON. |
| `GET` | `/api/v1/patients` | List patients for current tenant. Query: `?branchId=` (optional). Sorted by `CreatedAt DESC`. |
| `GET` | `/api/v1/branches` | List branches for current tenant (for dropdown). |

### Error Response Format

```json
{
  "error": {
    "code": "DUPLICATE_PHONE",
    "message": "Phone number already exists for this tenant"
  }
}
```

Validation errors (400):

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "firstName": ["First name is required"],
      "phoneNumber": ["Phone number is required"]
    }
  }
}
```

---

## Database Changes

### New Tables

**`tenants`**

| Column | Type | Constraints |
|--------|------|-------------|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `name` | `varchar(200)` | NOT NULL |
| `created_at` | `timestamptz` | NOT NULL, default `now()` |

**`branches`**

| Column | Type | Constraints |
|--------|------|-------------|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `tenant_id` | `uuid` | FK → `tenants(id)`, NOT NULL |
| `name` | `varchar(200)` | NOT NULL |
| `address` | `varchar(500)` | nullable |
| `created_at` | `timestamptz` | NOT NULL, default `now()` |

**`patients`**

| Column | Type | Constraints |
|--------|------|-------------|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `tenant_id` | `uuid` | FK → `tenants(id)`, NOT NULL |
| `first_name` | `varchar(100)` | NOT NULL |
| `last_name` | `varchar(100)` | NOT NULL |
| `phone_number` | `varchar(20)` | NOT NULL |
| `primary_branch_id` | `uuid` | FK → `branches(id)`, nullable |
| `created_at` | `timestamptz` | NOT NULL, default `now()` |
| `updated_at` | `timestamptz` | NOT NULL, default `now()` |
| `is_deleted` | `boolean` | NOT NULL, default `false` |

### Indexes

- `ix_patients_tenant_id_phone_number` — UNIQUE composite index on `(tenant_id, phone_number)` WHERE `is_deleted = false`
- `ix_patients_tenant_id_created_at` — for efficient listing sorted by `created_at DESC`
- `ix_branches_tenant_id` — for branch listing

### Migration

- Name: `CreateInitialSchema`
- Auto-applied on startup in `Program.cs` via `dbContext.Database.Migrate()`

### Seed Data

- Tenant: `{ Name: "Demo Clinic" }` (deterministic GUID printed to console)
- Branch 1: `{ Name: "Main Branch" }`
- Branch 2: `{ Name: "Downtown Branch" }`

---

## Tenant Safety Checklist

- [x] All new tenant-scoped tables include `tenant_id` column (`branches`, `patients`)
- [x] EF Core `HasQueryFilter` configured for `Branch` and `Patient` entities
- [x] `Patient` soft delete filter combined: `!IsDeleted && TenantId == _tenantId`
- [ ] Cache keys prefixed with `tenant:{tenant_id}:` — N/A this plan (no caching yet)
- [ ] Messages include `TenantId` in envelope — N/A this plan (no messaging yet)
- [x] No cross-tenant data leakage possible — `TenantId` from header, never from request body; query filter prevents cross-tenant reads; unique index scoped to tenant

---

## Testing Requirements

- [x] **Tenant isolation:** Given patients exist for Tenant A, When queried with Tenant B context, Then return empty list
- [x] **Duplicate phone (same tenant):** Given a patient with phone "0812345678" in Tenant A, When creating another patient with same phone in Tenant A, Then return 409 with DUPLICATE_PHONE error
- [x] **Duplicate phone (different tenant):** Given a patient with phone "0812345678" in Tenant A, When creating a patient with same phone in Tenant B, Then return 201 (success)
- [x] **Validation:** Given a POST with missing firstName, When submitted, Then return 400 with field-level error for firstName
- [x] **Frontend smoke:** Given the patients page, When rendered, Then display the patient list table and branch filter

Tests must be BDD-style (`Given_When_Then` or `Should_`).

---

## Implementation Notes

### Key Patterns to Establish

1. **ITenantScoped interface** — all tenant-scoped entities implement this; `HasQueryFilter` configured generically
2. **ITenantProvider abstraction** — scoped service; header-based now, JWT-based later; swap via DI only
3. **Service layer pattern** — controllers are thin; business logic in services; services depend on `DbContext` + `ITenantProvider`
4. **No manual tenant filtering** — never write `WHERE TenantId == x` in service code; `HasQueryFilter` handles it

### File Structure

```
src/backend/
├── ClinicPos.Api.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── Entities/
│   ├── ITenantScoped.cs
│   ├── Tenant.cs
│   ├── Branch.cs
│   └── Patient.cs
├── Data/
│   └── ClinicPosDbContext.cs
├── Dtos/
│   ├── CreatePatientRequest.cs
│   ├── PatientResponse.cs
│   └── ErrorResponse.cs
├── Validators/
│   └── CreatePatientRequestValidator.cs
├── Services/
│   ├── ITenantProvider.cs
│   ├── HttpHeaderTenantProvider.cs
│   ├── IPatientService.cs
│   └── PatientService.cs
├── Controllers/
│   ├── PatientsController.cs
│   └── BranchesController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── TenantResolutionMiddleware.cs
└── Migrations/
    └── (auto-generated)

src/backend/tests/ClinicPos.Api.Tests/
├── ClinicPos.Api.Tests.csproj
└── Patients/
    ├── TenantIsolationTests.cs
    ├── DuplicatePhoneTests.cs
    └── ValidationTests.cs

src/frontend/
├── package.json
├── next.config.ts
├── tsconfig.json
├── Dockerfile
├── lib/
│   └── api.ts
├── types/
│   ├── patient.ts
│   └── branch.ts
└── app/
    ├── layout.tsx
    ├── page.tsx
    └── patients/
        ├── page.tsx
        └── new/
            └── page.tsx
```

---

## Verification Checklist

1. `docker compose up --build` starts all 5 services with zero manual setup
2. PostgreSQL contains `tenants`, `branches`, `patients` tables (migration auto-applied)
3. Seed data present: 1 tenant, 2 branches
4. `curl -H "X-Tenant-Id:{id}" http://localhost:5000/api/v1/patients` → `[]`
5. `curl -X POST -H "X-Tenant-Id:{id}" -H "Content-Type: application/json" -d '{"firstName":"Jane","lastName":"Doe","phoneNumber":"0812345678"}' http://localhost:5000/api/v1/patients` → 201
6. Repeat same POST → 409 with `DUPLICATE_PHONE`
7. POST with missing firstName → 400 with validation error
8. `http://localhost:3000/patients` → patient list with Jane Doe
9. Create patient via UI → appears in list
10. Branch filter dropdown works
11. `dotnet test` → all tests pass
