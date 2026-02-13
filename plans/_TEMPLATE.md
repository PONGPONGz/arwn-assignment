# Feature Plan: [Feature Name]

> **Plan ID:** NNN
> **Status:** Draft | Approved | In Progress | Done | Revised
> **Created:** YYYY-MM-DD
> **Last Updated:** YYYY-MM-DD

---

## Summary

_One or two sentences describing what this feature/phase delivers and why._

---

## Goals

- Goal 1
- Goal 2

---

## Non-Goals (Out of Scope)

- Explicitly list what this plan does NOT cover

---

## Dependencies

- List any plans, features, or infrastructure that must exist before this work begins
- e.g., "Requires `001-infrastructure-skeleton.md` to be Done"

---

## Design Decisions

| Decision | Options Considered | Chosen | Rationale |
|----------|--------------------|--------|-----------|
| Example  | A, B, C            | B      | Because… |

---

## Deliverables

- [ ] Deliverable 1
- [ ] Deliverable 2
- [ ] Deliverable 3

---

## API Endpoints (if applicable)

| Method | Path | Description |
|--------|------|-------------|
| POST   | `/api/v1/...` | ... |

---

## Database Changes (if applicable)

- New tables / columns / indexes / schema changes
- Migration naming convention: descriptive, e.g., `AddPatientTable`

---

## Tenant Safety Checklist

- [ ] All new tenant-scoped tables include `tenant_id` column
- [ ] EF Core `HasQueryFilter` configured for new entities (automatic WHERE injection)
- [ ] Cache keys prefixed with `tenant:{tenant_id}:`
- [ ] Messages include `TenantId` in envelope
- [ ] No cross-tenant data leakage possible

---

## Testing Requirements

- [ ] Test 1: _describe behavior_
- [ ] Test 2: _describe behavior_
- Tests must be BDD-style (`Given_When_Then` or `Should_`)

---

## Notes / Open Questions

- Any unresolved questions or risks
