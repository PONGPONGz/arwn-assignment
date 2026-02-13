# Feature Plan: Authorization & User Management

> **Plan ID:** 002
> **Status:** Done
> **Created:** 2026-02-13
> **Last Updated:** 2026-02-13

---

## Summary

Add stub token-based authentication, role-based authorization (Admin/User/Viewer), user management API endpoints, and a CLI-triggered seeder. Permissions are enforced server-side via ASP.NET Core authorization policies. The existing auto-seeder is replaced by a `dotnet run -- --seed` command.

---

## Goals

- Implement three roles: Admin, User, Viewer
- Define and enforce permissions: CanCreatePatients, CanViewPatients, CanCreateAppointments
- User management API: create user, assign role, associate user with tenant and branches
- All requests require authenticated identity (stub API token in Bearer header)
- Viewer cannot create patients (enforced server-side)
- Replace auto-seeder with CLI-triggered seeder (`dotnet run -- --seed`)
- Seed: 1 Tenant, 2 Branches, 3 Users (one per role) with correct associations

---

## Non-Goals (Out of Scope)

- Full JWT authentication (stub token is sufficient for v1)
- Refresh tokens, password reset, session management
- Appointments entity or endpoints (only the permission is defined)
- Frontend login page or token handling
- Advanced RBAC beyond the three roles
- Internationalization

---

## Dependencies

- Requires `001-patient-thin-slice.md` to be Done

---

## Design Decisions

| Decision | Options Considered | Chosen | Rationale |
|----------|--------------------|--------|-----------|
| Auth mechanism | JWT, Cookie session, Stub API token | Stub API token | Simplest; permissions still enforced via real ASP.NET Core policies. Easy to swap for JWT later. |
| Roles | Admin/Staff, Admin/User/Viewer | Admin/User/Viewer | Assignment requirement. Overrides PLAN.md's Admin/Staff. |
| Tenant resolution | JWT claims, X-Tenant-Id header, User record lookup | User record lookup via auth handler | Token identifies user → user has tenant_id → tenant resolved from claims. Eliminates X-Tenant-Id header. |
| Seeder trigger | Auto on startup, CLI flag, Separate project, API endpoint | CLI flag (`--seed`) | Clean separation; auto-seed removed per requirement. One command. |
| User-Branch relationship | Single branch, Many-to-many | Many-to-many (UserBranch join table) | Requirement: "associate user with tenant and one or more branches" |
| User management access | Any authenticated, Admin only | Admin only | Only admins should manage users/roles |

---

## Deliverables

- [x] `User` entity with Role enum, ApiToken, tenant scoping
- [x] `UserBranch` join entity (many-to-many User ↔ Branch)
- [x] EF Core migration `AddUserAndRoleTables`
- [x] `ApiTokenAuthHandler` (custom ASP.NET Core auth handler)
- [x] Authorization policies: CanCreatePatients, CanViewPatients, CanCreateAppointments
- [x] `[Authorize]` attributes on PatientsController and BranchesController
- [x] UsersController with CRUD endpoints (Admin only)
- [x] UserService / IUserService
- [x] `SeedDataRunner` class and `--seed` CLI flag
- [x] Remove auto-seed from Program.cs startup
- [x] Refactor TenantResolutionMiddleware to read tenant from auth claims
- [x] Update existing tests to use auth tokens
- [x] New authorization tests (Viewer blocked, unauthenticated blocked)
- [x] Update README with seeder usage and API tokens

---

## API Endpoints

| Method | Path | Policy | Description |
|--------|------|--------|-------------|
| POST | `/api/v1/users` | Admin only | Create user (email, name, password, role, branchIds) |
| GET | `/api/v1/users` | Admin only | List tenant users |
| PUT | `/api/v1/users/{id}/role` | Admin only | Assign role to user |
| PUT | `/api/v1/users/{id}/branches` | Admin only | Set branch associations |

Existing endpoints updated with auth:

| Method | Path | Policy | Change |
|--------|------|--------|--------|
| GET | `/api/v1/patients` | CanViewPatients | Add `[Authorize]` |
| POST | `/api/v1/patients` | CanCreatePatients | Add `[Authorize]` |
| GET | `/api/v1/branches` | Authenticated | Add `[Authorize]` |

---

## Database Changes

### New: `users` table

| Column | Type | Constraints |
|--------|------|-------------|
| `id` | `uuid` | PK, default `gen_random_uuid()` |
| `tenant_id` | `uuid` | FK → tenants, NOT NULL |
| `email` | `varchar(200)` | NOT NULL |
| `full_name` | `varchar(200)` | NOT NULL |
| `password_hash` | `varchar(200)` | NOT NULL |
| `role` | `integer` | NOT NULL (enum: 0=Admin, 1=User, 2=Viewer) |
| `api_token` | `varchar(100)` | NOT NULL, UNIQUE |
| `created_at` | `timestamp` | NOT NULL, default `now()` |

Indexes: unique on `(tenant_id, email)`, unique on `api_token`

### New: `user_branches` table

| Column | Type | Constraints |
|--------|------|-------------|
| `user_id` | `uuid` | FK → users, NOT NULL |
| `branch_id` | `uuid` | FK → branches, NOT NULL |

PK: composite `(user_id, branch_id)`

Migration name: `AddUserAndRoleTables`

---

## Tenant Safety Checklist

- [x] `users` table includes `tenant_id` column
- [x] EF Core `HasQueryFilter` configured for `User` entity
- [x] `UserBranch` scoped transitively through `User` query filter (no independent filter needed)
- [x] Cache keys: N/A (no cache usage in this feature)
- [x] Messages: N/A (no messaging in this feature)
- [x] Tenant resolved from authenticated user's record — no cross-tenant leakage

---

## Implementation Phases

### Phase 1 — Data Model & Migration (steps 1-5)

1. Create `Role` enum in `src/backend/Entities/Role.cs` — values: `Admin = 0`, `User = 1`, `Viewer = 2`
2. Create `User` entity in `src/backend/Entities/User.cs` implementing `ITenantScoped`
3. Create `UserBranch` entity in `src/backend/Entities/UserBranch.cs`
4. Update `src/backend/Data/ClinicPosDbContext.cs`:
   - Add `DbSet<User>` and `DbSet<UserBranch>`
   - Configure `User` with `HasQueryFilter(u => u.TenantId == _tenantId)` — same pattern as Patient/Branch
   - Configure `UserBranch` with composite PK, FKs
   - Add unique indexes: `(TenantId, Email)` on User, global unique on `ApiToken`
5. Generate migration: `dotnet ef migrations add AddUserAndRoleTables`

### Phase 2 — Authentication (*depends on Phase 1*)

6. Add NuGet: `BCrypt.Net-Next` to `src/backend/ClinicPos.Api.csproj`
7. Create `src/backend/Auth/ApiTokenAuthHandler.cs`:
   - Extends `AuthenticationHandler<AuthenticationSchemeOptions>`
   - Reads `Authorization: Bearer <token>` header
   - Queries `User` by `ApiToken` using `IgnoreQueryFilters()` (bypasses tenant filter)
   - Creates `ClaimsPrincipal` with claims: `sub`, `tenant_id`, `role`, `email`
8. Register in `src/backend/Program.cs`:
   - `builder.Services.AddAuthentication("ApiToken").AddScheme<...>("ApiToken", null)`
   - `app.UseAuthentication()` + `app.UseAuthorization()` before TenantResolutionMiddleware
9. Refactor `src/backend/Middleware/TenantResolutionMiddleware.cs`:
   - Read `tenant_id` from `User.Identity` claims instead of `X-Tenant-Id` header
   - If user not authenticated, skip (auth returns 401)

### Phase 3 — Authorization Policies (*depends on Phase 2*)

10. Define policies in `Program.cs` via `AddAuthorization(options => ...)`:
    - `"CanCreatePatients"` → role Admin or User
    - `"CanViewPatients"` → role Admin, User, or Viewer
    - `"CanCreateAppointments"` → role Admin or User
    - `"AdminOnly"` → role Admin
11. Apply `[Authorize(Policy = "...")]` on controller actions:
    - `PatientsController`: `CanViewPatients` on GET, `CanCreatePatients` on POST
    - `BranchesController`: `[Authorize]` (any authenticated)

### Phase 4 — User Management API (*parallel with Phase 5, depends on Phase 3*)

12. Create DTOs in `src/backend/Dtos/`:
    - `CreateUserRequest`, `AssignRoleRequest`, `AssociateBranchesRequest`, `UserResponse`
13. Create `IUserService` / `UserService` in `src/backend/Services/`
    - `CreateAsync`: validate, hash password (BCrypt), generate `ApiToken = Guid.NewGuid().ToString()`, save
    - `AssignRoleAsync`: find user, update role
    - `AssociateBranchesAsync`: replace user-branch associations
    - `ListAsync`: list users for current tenant
14. Create `CreateUserRequestValidator` in `src/backend/Validators/`
15. Create `UsersController` in `src/backend/Controllers/UsersController.cs` with Admin-only policy

### Phase 5 — Seeder (*parallel with Phase 4, depends on Phase 1*)

16. Remove inline auto-seed block from `src/backend/Program.cs` (the block after `app.Build()` that checks for existing tenant)
17. Create `src/backend/Data/SeedDataRunner.cs`:
    - Idempotent: checks if tenant exists before seeding
    - Uses `IgnoreQueryFilters()` for all reads/writes
    - Seeds:
      - Tenant: "Demo Clinic" (`a0000000-0000-0000-0000-000000000001`)
      - Branches: "Main Branch" (`b0000000-...0001`), "Downtown Branch" (`b0000000-...0002`)
      - Admin user: `admin@demo.clinic`, token `admin-token-00000001`, Role.Admin
      - User: `user@demo.clinic`, token `user-token-00000002`, Role.User
      - Viewer: `viewer@demo.clinic`, token `viewer-token-00000003`, Role.Viewer
      - All users associated with both branches
    - Prints summary table with tokens to console
18. Wire in `Program.cs`: if `args.Contains("--seed")` → migrate + seed + exit(0)

### Phase 6 — Tests (*parallel with Phase 4-5, depends on Phase 2-3*)

19. Update `TestWebApplicationFactory.cs`:
    - Seed test users (one per role) with known tokens during test setup
    - Add `CreateAuthenticatedClient(string apiToken)` helper
20. Update existing tests in `Patients/` to use authenticated clients
21. New `Authorization/PermissionTests.cs`:
    - `Given_ViewerToken_When_CreatePatient_Then_Returns403`
    - `Given_UserToken_When_CreatePatient_Then_Returns201`
    - `Given_AdminToken_When_CreatePatient_Then_Returns201`
    - `Given_NoToken_When_AnyRequest_Then_Returns401`
22. New `Authorization/TenantIsolationWithAuthTests.cs`:
    - `Given_UserFromTenantA_When_QueryPatients_Then_OnlySeeTenantAData`

### Phase 7 — Documentation (*depends on all above*)

23. Update `.env.example` with `--seed` usage note
24. Update `README.md`: seeder usage, API token table, auth header format
25. Append prompt to `AI_PROMPTS.md`
26. Update `PLAN.md` index with plan 002

---

## Testing Requirements

- [x] `Given_ViewerToken_When_CreatePatient_Then_Returns403` — Viewer role cannot POST patients
- [x] `Given_UserToken_When_CreatePatient_Then_Returns201` — User role can POST patients
- [x] `Given_AdminToken_When_CreatePatient_Then_Returns201` — Admin role can POST patients
- [x] `Given_NoToken_When_AnyRequest_Then_Returns401` — Unauthenticated request rejected
- [x] `Given_UserFromTenantA_When_QueryPatients_Then_OnlySeeTenantAData` — Tenant isolation with auth
- [x] All existing patient tests updated and passing with auth tokens
- [x] Seeder runs idempotently without errors

---

## Seed Data Reference

| Role | Email | API Token | Tenant | Branches |
|------|-------|-----------|--------|----------|
| Admin | admin@demo.clinic | `admin-token-00000001` | Demo Clinic | Main, Downtown |
| User | user@demo.clinic | `user-token-00000002` | Demo Clinic | Main, Downtown |
| Viewer | viewer@demo.clinic | `viewer-token-00000003` | Demo Clinic | Main, Downtown |

---

## Notes / Open Questions

- Stub auth is intentionally simple — swap `ApiTokenAuthHandler` for `JwtBearerHandler` in a future iteration
- Password is hashed with BCrypt but not used for login in stub mode (tokens are pre-assigned)
- The `X-Tenant-Id` header is eliminated; tenant comes from authenticated user's record
- Frontend changes (login, token storage) deferred to a separate plan