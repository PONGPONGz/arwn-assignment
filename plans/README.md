# Feature Plans

This folder contains individual feature/phase plans for the Clinic POS platform.

---

## How It Works

- Each feature or implementation phase gets its **own plan file** in this folder.
- Plans are created **before** implementation begins.
- Use [_TEMPLATE.md](_TEMPLATE.md) as the starting point for every new plan.
- Plans are living documents — update them as scope or decisions change during implementation.

## Naming Convention

```
NNN-short-feature-name.md
```

- `NNN` — Zero-padded sequential number (e.g., `001`, `002`)
- `short-feature-name` — Kebab-case, descriptive (e.g., `infrastructure-skeleton`, `auth-tenant-resolution`, `patient-crud`)

**Examples:**
- `001-infrastructure-skeleton.md`
- `002-auth-tenant-resolution.md`
- `003-patient-crud.md`

## Plan Lifecycle

| Status | Meaning |
|--------|---------|
| **Draft** | Plan created, not yet reviewed/approved |
| **Approved** | Ready for implementation |
| **In Progress** | Work has started |
| **Done** | Fully implemented and verified |
| **Revised** | Scope changed after approval — re-review needed |

## Rules

1. **Every feature request gets a plan first** — no implementation without a plan file.
2. **Read existing plans** before creating a new one — check for overlap or dependencies.
3. **Reference constraints** from `copilot-instructions.md` — especially tenant safety, engineering standards, and testing requirements.
4. **Update status** in the plan file as work progresses.
5. **Link plans** from `PLAN.md` (the master index) so there's a single source of truth for project status.
