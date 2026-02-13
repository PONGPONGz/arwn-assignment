# Copilot Instructions — Clinic POS Platform

You will record my exact prompt used in AI_PROMPTS.md, separate by iteration (chat turns)

## Project Identity

- **Name:** Clinic POS Platform v1
- **Type:** Multi-tenant, multi-branch B2B SaaS
- **Stage:** v1 — MVP

---

## Domain Model (Core Invariants)

- 1 **Tenant** → many **Branches**
- 1 **Patient** belongs to exactly 1 **Tenant**, may visit multiple **Branches**
- **Tenant data isolation is mandatory** — no cross-tenant data exposure, ever
- Design uses **shared schema with global query filters** — single database, single schema, all tenant-scoped tables have a `tenant_id` column
- EF Core's `HasQueryFilter` automatically injects `WHERE tenant_id = @tenantId` on every query — no manual filtering in business logic
- All tenant-scoped queries, caches, and messages must be tenant-scoped

---

## Technology Stack (Mandatory — Do Not Deviate)

| Layer       | Technology           |
|-------------|----------------------|
| Backend     | .NET 10 / C#         |
| Frontend    | Next.js (React)      |
| Database    | PostgreSQL           |
| Cache       | Redis                |
| Messaging   | RabbitMQ             |
| Containers  | Docker / Docker Compose |

---

## Project Structure

```
/
├── src/
│   ├── backend/       # .NET 10 Web API
│   └── frontend/      # Next.js application
├── docker-compose.yml
├── .env.example
├── README.md
├── AI_PROMPTS.md
├── PLAN.md            # Master plan index
├── plans/             # Per-feature plan documents
│   ├── README.md      # Planning guidelines
│   └── _TEMPLATE.md   # Plan template
└── .github/
    └── copilot-instructions.md
```

---

## Engineering Standards

All code must adhere to:

### Principles
- **SOLID** — Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **DRY** — Don't Repeat Yourself
- **YAGNI** — You Aren't Gonna Need It (no speculative features)
- **KISS** — Keep It Simple, Stupid

### Quality Expectations
- **Readable:** Code should be self-documenting; avoid clever tricks
- **Maintainable:** Favor explicit over implicit; small, focused classes/functions
- **Testable:** All business logic must be unit-testable; depend on abstractions
- **Secure:** Validate at system boundaries; never trust external input; tenant isolation enforced at data access layer

### Naming Conventions
- C#: PascalCase for public members, camelCase for locals/parameters, `_camelCase` for private fields
- TypeScript/JS: camelCase for variables/functions, PascalCase for components/types
- Database: snake_case for tables and columns
- REST endpoints: kebab-case, plural nouns (e.g., `/api/v1/patients`)

---

## Tenant Safety Rules

These rules apply to **every** feature, query, endpoint, cache key, and message:

1. **Database:** All tenant-scoped tables include a `tenant_id` column. EF Core `HasQueryFilter` on every tenant-scoped entity automatically injects `WHERE tenant_id = @tenantId`. No manual filtering in repositories or services — the DbContext handles it.
2. **API:** `tenant_id` is resolved from the authenticated user's claims/context — never from request body or URL path for authorization purposes.
3. **Cache (Redis):** All cache keys must be prefixed with `tenant:{tenant_id}:` to prevent cross-tenant cache leakage.
4. **Messaging (RabbitMQ):** All messages must include `TenantId` in the message envelope/headers. Consumers must validate tenant context before processing.
5. **Frontend:** The frontend receives tenant context from the authenticated session. It must never allow manual tenant switching by non-admin users.

---

## Testing Standards

- **Style:** BDD — tests describe behavior, not implementation
- **Backend:** xUnit + FluentAssertions (or equivalent). Use in-memory or test-container PostgreSQL.
- **Frontend:** Jest / React Testing Library or Playwright for smoke tests
- **Minimum required tests:**
  1. Backend: Tenant scoping enforced (cannot read another tenant's data)
  2. Backend: Duplicate phone number prevented within same tenant
  3. Frontend/Integration: Smoke test (page loads, basic interaction)

---

## Infrastructure Rules

- `docker compose up --build` must start **everything**: PostgreSQL, Redis, RabbitMQ, backend, frontend
- Database migrations must apply **automatically on startup**
- Seed data (users, tenants, branches) must be applied on first run
- `.env.example` must document all required environment variables
- The system must be **runnable by others** with no manual setup beyond `docker compose up --build`

---

## Workflow Rules (For All Agents)

1. **Plan before implementing.** Every feature gets a plan file in `plans/` using `_TEMPLATE.md`. Reference `PLAN.md` (master index) before writing code.
2. **Check existing code first.** Before creating files or features, read existing structure, constraints, and implementations.
3. **Record prompts.** Every user prompt must be appended to `AI_PROMPTS.md` with iteration number and date.
4. **No speculative code.** Only build what is planned and agreed upon.
5. **Test alongside implementation.** Write tests as part of the same iteration, not after.
6. **Keep infrastructure reproducible.** Any infra change must be reflected in `docker-compose.yml` and `.env.example`.
7. **Update plan status.** Mark plan files as In Progress/Done as work progresses. Update `PLAN.md` index accordingly.

---

## Security Baseline

- No SQL string concatenation — use parameterized queries / EF Core
- No secrets in source code — use environment variables
- Input validation at API boundaries
- Authentication required for all non-public endpoints
- Authorization: users can only access their own tenant's data
- CORS configured explicitly (no wildcard in production)
- HTTPS enforced in production (HTTP acceptable in local dev)

---

## Out of Scope for v1

- Schema-per-tenant or database-per-tenant isolation
- Payment processing
- Advanced RBAC (keep roles simple: Admin, Staff)
- Real-time features (WebSockets)
- Internationalization (i18n)
- Mobile app
