# PLAN.md — Clinic POS Platform v1

> **Master index.** All feature plans live in [`plans/`](plans/). This file tracks overall status.
> Read [`plans/README.md`](plans/README.md) for planning guidelines and [`plans/_TEMPLATE.md`](plans/_TEMPLATE.md) for the plan template.

---

## Planning Workflow

1. **New feature requested** → Create a plan file in `plans/` using `_TEMPLATE.md`
2. **Review & approve** → Mark plan status as `Approved`
3. **Implement** → Mark as `In Progress`, follow the plan
4. **Complete** → Mark as `Done`, update this index

See [`plans/README.md`](plans/README.md) for naming conventions, lifecycle, and rules.

---

## Feature Plan Index

| # | Plan File | Feature | Status |
|---|-----------|---------|--------|
| 001 | [`001-patient-thin-slice.md`](plans/001-patient-thin-slice.md) | Patient Thin Slice (Create + List, infrastructure bootstrap) | Done |
| 002 | [`002-auth-user-management.md`](plans/002-auth-user-management.md) | Authorization & User Management | Done |
| 003 | [`003-appointment-messaging.md`](plans/003-appointment-messaging.md) | Appointment + RabbitMQ Messaging (C1, C2, C3) | Done |
| 004 | [`004-caching-data-access.md`](plans/004-caching-data-access.md) | Caching & Data Access (D1, D2, D3) | Draft |
_Plans will be added here as features are requested and planned._

---

## Domain Entities (Planned)

```
Tenant
├── Id (Guid)
├── Name
├── CreatedAt

Branch
├── Id (Guid)
├── TenantId (FK → Tenant)
├── Name
├── Address
├── CreatedAt

User
├── Id (Guid)
├── TenantId (FK → Tenant)
├── BranchId (FK → Branch)
├── Email
├── PasswordHash
├── Role (Admin | Staff)
├── FullName
├── CreatedAt

Patient
├── Id (Guid)
├── TenantId (FK → Tenant)
├── FirstName
├── LastName
├── PhoneNumber (unique per tenant — composite unique index on tenant_id + phone_number)
├── PrimaryBranchId (FK → Branch, nullable)
├── DateOfBirth
├── Gender
├── Address
├── CreatedAt
├── UpdatedAt
├── IsDeleted
```

---

## Architectural Decisions Record

| Decision | Rationale |
|----------|-----------|
| Shared schema + EF Core `HasQueryFilter` | Simplest approach; leverages .NET 10 / EF Core built-in global query filters; `WHERE tenant_id` injected automatically on every query; no manual filtering needed in business logic; easy to test |
| Single database, single schema | All tables in one schema; `tenant_id` column on all tenant-scoped tables; simpler migrations and deployment |
| JWT with tenant claim | Stateless auth; no session store needed; tenant_id from JWT feeds into DbContext query filter |
| No refresh tokens | YAGNI for v1 |
| BCrypt passwords | Industry standard, simple |
| Soft delete only | Data retention; simplifies audit |
| Two roles (Admin, Staff) | KISS; advanced RBAC is out of scope |
| Auto-migrate on startup | Simplifies deployment for v1 |
| No payment processing | Out of scope |
| No i18n | Out of scope |
