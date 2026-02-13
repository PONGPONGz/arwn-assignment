# AI Prompts Log

This document records the exact prompts used during the development of the Clinic POS platform, separated by iteration (chat turns).

---

## Iteration 1 — Project Initialization & Planning Setup

**Date:** 2026-02-13

**Prompt:**

> You are going to record my exact prompt used in new AI_PROMPTS.md file, separate by iteration (chat turns)
>
> You are building v1 of a Clinic POS platform.
> This is a multi-tenant, multi-branch B2B system: - 1 Tenant → many Branches - 1 Patient
> belongs to exactly 1 Tenant, and may visit multiple Branches - Tenant data isolation is
> mandatory (no cross-tenant exposure)
> You are not required to implement enterprise-grade tenancy enforcement, but your design
> must be tenant-safe by default.
>
> Technology constraints (mandatory)
> • Backend: .NET 10 / C#
> • Frontend: Next.js
> • Database: PostgreSQL
> • Cache: Redis
> • Messaging: RabbitMQ
>
> Required deliverables structure:
> /src
> - /backend
> - /frontend
> README.md
> AI_PROMPTS.md
>
> README.md (required)
> Must include: - Architecture overview (how tenant safety works) - Assumptions and trade-
> offs - How to run (one command) - Environment variables (.env.example) - Seeded users
> and how to login - API examples (curl) - How to run tests
>
> Include small test suites, test for behavior in BDD
>
> Your submission must be runnable by others.
> Docker & one-command run (required)
> Provide: - docker compose up --build - Starts: PostgreSQL, Redis, RabbitMQ, backend,
> frontend
> Migrations
> Database migrations must apply automatically on startup or via documented command.
> Minimal tests (required)
> Provide at least 3 automated tests: - 1 backend test: tenant scoping enforced (cannot
> read other tenant) - 1 backend test: duplicate phone prevented within tenant - 1 frontend
> or integration smoke test (can be minimal)
>
> For now, initialize and define copilot-instruction.md for this project so all agents working on this project are aligned with the constraints and scope. From now on, we will work by planning first, then implement after via a living planning document that should be generated explicitly. Each plans/feature implementation after that should take into account of existing structure/feature/constraint/rules and implementation, prefer Engineering Standards: Reference SOLID, DRY, YAGNI, KISS. Specify testability, maintainability, security. Expect readable, maintainable code.

---

## Iteration 2 — Planning Refinement & Schema Isolation Decision

**Date:** 2026-02-13

**Prompt:**

> 2 areas to be refined:
> 1. Plan is to be created per feature I am going to request in the near future. By living documents, I mean a prepared folder, and guideline/template for plan creation to follow from.
> 2. The way we're doing data isolation is using table-level isolation, instead, I prefer schema-level isolation. Mark this decision made in README.md, we trade simplicity and less resource-intensive approach with good balance between isolation and resource efficiency, but with better data isolation, allowing for customization at the schema level, and is easier to scale compared to separating by database-level.
>
> Again, no implementation needed, this is project documentation and guideline refinement.

---

## Iteration 3 — Revert to Shared Schema with EF Core HasQueryFilter

**Date:** 2026-02-13

**Prompt:**

> My bad, let's stick with Shared schema with global query filter to use what .NET 10 has to offer. How about using EF Core's HasQueryFilter in DbContext, so it automatically injects WHERE clause.

---

## Iteration 4 — Patient Thin Slice Planning (A1 + A2 + A3)

**Date:** 2026-02-13

**Prompt:**

> Core slice (mandatory)
> A1. Implement a working thin slice (backend + frontend)
> Build a usable flow end-to-end: - Create Patient - List Patients
> Minimum backend requirements
> • REST API with request validation and consistent error responses
> • Persistence in PostgreSQL (migrations required)
> • Tenant-safe filtering on all reads/writes
> Minimum frontend requirements
> • Next.js UI that can:
> o Create a patient
> o List patients
> o Filter by Branch (optional)
> • Basic usability: fast, simple forms; no heavy UI work required
> A2. Create Patient
> Patient fields (minimum): - FirstName (required) - LastName (required) - PhoneNumber
> (required) - TenantId (required) - CreatedAt (server-generated) - (Optional)
> PrimaryBranchId
> Rules: - PhoneNumber must be unique within the same Tenant (not globally) - Return a
> safe error on duplicate
>
> A3. List Patients
> List with: - Required filter: TenantId - Optional filter: BranchId - Sorted by CreatedAt DESC
> You may model "patient visits branches" minimally (e.g., PrimaryBranchId or a
> separate mapping table). Explain your choice.

**Decisions made:**
- Patient name: FirstName + LastName (two fields), not FullName
- Tenant resolution: X-Tenant-Id header (JWT deferred)
- Patient ↔ Branch: PrimaryBranchId FK (nullable), not mapping table
- Plan scope: Single plan (001-patient-thin-slice)

**Plan created:** `plans/001-patient-thin-slice.md`

---

## Iteration 5 — Patient Thin Slice Implementation

**Date:** 2026-02-13

**Prompt:**

> Start implementation of #file:001-patient-thin-slice.md. Install whatever tool is required to run the project.

**Work performed:**
- Phase 1: Created `docker-compose.yml` (5 services), `.env.example`, `.env`, `README.md`, backend scaffolding (`dotnet new webapi`), frontend scaffolding (`npx create-next-app`)
- Phase 2: Entity models (`ITenantScoped`, `Tenant`, `Branch`, `Patient`), `ClinicPosDbContext` with `HasQueryFilter`, `ITenantProvider` + `HttpHeaderTenantProvider`, `TenantResolutionMiddleware`, `ExceptionHandlingMiddleware`, EF Core migration (`CreateInitialSchema`), seed data
- Phase 3: DTOs (`CreatePatientRequest`, `PatientResponse`, `BranchResponse`), `CreatePatientRequestValidator` (FluentValidation), `IPatientService` + `PatientService`, `PatientsController`, `BranchesController`
- Phase 4: API client (`lib/api.ts`), TypeScript types, patient list page (`/patients`), create patient page (`/patients/new`), Dockerfiles
- Phase 5: Backend tests (5/5 passing — tenant isolation, duplicate phone same/different tenant, validation, missing header), frontend smoke tests (3/3 passing — list page renders, create form renders)
- Verification: `docker compose up --build` starts all services, API endpoints verified (branches, create patient, list patients, tenant isolation, duplicate phone 409)

**Result:** Plan 001 marked as Done. All deliverables and tests complete.
