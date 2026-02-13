# Clinic POS Platform v1

> Multi-tenant, multi-branch B2B SaaS for clinic point-of-sale operations.

---

## Architecture Overview

### Tenant Safety

- **Shared schema** with a `tenant_id` column on all tenant-scoped tables
- EF Core `HasQueryFilter` automatically injects `WHERE tenant_id = @tenantId` on every query
- `ITenantProvider` abstraction resolves the current tenant per request
  - **v1:** reads `X-Tenant-Id` HTTP header
  - **Future:** reads from JWT claims (swap DI registration only — no business logic changes)
- No manual tenant filtering in service code — the DbContext handles it
- Composite unique indexes are tenant-scoped (e.g., phone number unique per tenant, not globally)

### Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10 / C# — Web API |
| Frontend | Next.js (React, App Router, TypeScript) |
| Database | PostgreSQL 16 |
| Cache | Redis 7 (provisioned, unused in v1) |
| Messaging | RabbitMQ 3 (provisioned, unused in v1) |
| Containers | Docker / Docker Compose |

---

## Assumptions & Trade-offs

- **No JWT auth in v1** — tenant resolved from `X-Tenant-Id` header for fast iteration. Auth layered in next plan.
- **Shared schema** — simplest multi-tenancy; trades per-tenant schema customization for operational simplicity.
- **PrimaryBranchId FK** — patients linked to branches via simple FK, not a mapping table. Sufficient until visit tracking is planned.
- **Soft delete only** — `is_deleted` flag; no hard deletes.
- **Auto-migrate on startup** — acceptable for v1; production would use a migration runner.

---

## How to Run

### Prerequisites

- Docker & Docker Compose

### One Command

```bash
docker compose up --build
```

This starts **all 5 services**: PostgreSQL, Redis, RabbitMQ, backend API, frontend UI.

- **Backend API:** http://localhost:5000
- **Frontend UI:** http://localhost:3000
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)

Database migrations apply automatically on startup. Seed data is created on first run.

---

## Environment Variables

See [`.env.example`](.env.example). Copy to `.env` before running:

```bash
cp .env.example .env
```

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_USER` | PostgreSQL username | `clinicpos` |
| `POSTGRES_PASSWORD` | PostgreSQL password | `clinicpos_dev` |
| `POSTGRES_DB` | Database name | `clinicpos` |
| `RABBITMQ_DEFAULT_USER` | RabbitMQ username | `guest` |
| `RABBITMQ_DEFAULT_PASS` | RabbitMQ password | `guest` |
| `NEXT_PUBLIC_API_URL` | Backend URL for frontend | `http://localhost:5000` |
| `NEXT_PUBLIC_TENANT_ID` | Tenant ID for frontend requests | `a0000000-...0001` |

---

## Seeded Data

On first startup, the following data is seeded:

| Entity | Name | ID |
|--------|------|----|
| Tenant | Demo Clinic | `a0000000-0000-0000-0000-000000000001` |
| Branch | Main Branch | `b0000000-0000-0000-0000-000000000001` |
| Branch | Downtown Branch | `b0000000-0000-0000-0000-000000000002` |

Use the Tenant ID as the `X-Tenant-Id` header for API requests.

---

## API Examples

### List Patients

```bash
curl -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     http://localhost:5000/api/v1/patients
```

### Create Patient

```bash
curl -X POST \
     -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     -H "Content-Type: application/json" \
     -d '{"firstName":"Jane","lastName":"Doe","phoneNumber":"0812345678"}' \
     http://localhost:5000/api/v1/patients
```

### Create Patient with Branch

```bash
curl -X POST \
     -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     -H "Content-Type: application/json" \
     -d '{"firstName":"John","lastName":"Smith","phoneNumber":"0898765432","primaryBranchId":"b0000000-0000-0000-0000-000000000001"}' \
     http://localhost:5000/api/v1/patients
```

### List Patients (filtered by branch)

```bash
curl -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     "http://localhost:5000/api/v1/patients?branchId=b0000000-0000-0000-0000-000000000001"
```

### List Branches

```bash
curl -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     http://localhost:5000/api/v1/branches
```

---

## How to Run Tests

### Backend Tests

```bash
cd src/backend
dotnet test
```

### Frontend Tests

```bash
cd src/frontend
npm test
```

---

## Project Structure

```
/
├── docker-compose.yml
├── .env.example
├── README.md
├── AI_PROMPTS.md
├── PLAN.md
├── plans/
│   └── 001-patient-thin-slice.md
└── src/
    ├── backend/     # .NET 10 Web API
    └── frontend/    # Next.js (App Router, TypeScript)
```
