# Feature Plan: Caching & Data Access (D1, D2, D3)

> **Plan ID:** 004
> **Status:** Done
> **Created:** 2026-02-13
> **Last Updated:** 2026-02-13

---

## Summary

Add Redis caching to the List Patients read path with tenant-scoped cache keys that prevent cross-tenant cache leakage. Invalidate cache on Create Patient and Create Appointment mutations. Uses `Microsoft.Extensions.Caching.StackExchangeRedis` (the standard ASP.NET Core distributed cache backed by StackExchange.Redis).

---

## Goals

- **D1:** Cache the List Patients GET endpoint to reduce database load on repeated reads
- **D2:** Implement tenant-scoped cache key strategy (`tenant:{tenantId}:patients:list:{branchId|all}`) ensuring no cross-tenant cache leakage
- **D3:** Invalidate relevant cache keys when Create Patient or Create Appointment is called (wildcard prefix deletion via Redis `SCAN`)

---

## Non-Goals (Out of Scope)

- Caching other endpoints (branches, users, appointments)
- Versioned/generation-based cache keys (direct invalidation chosen for simplicity)
- Cache-aside patterns with sliding expiration tuning (use a single fixed TTL)
- Read-through or write-through cache patterns
- Frontend caching or HTTP cache headers
- Redis Cluster or Sentinel configuration

---

## Dependencies

- Requires `001-patient-thin-slice.md` to be Done (Patient list endpoint, `PatientService`)
- Requires `003-appointment-messaging.md` to be Done or In Progress (Appointment entity, `AppointmentService` exist)
- Redis is already provisioned in `docker-compose.yml` with a health check

---

## Design Decisions

| Decision | Options Considered | Chosen | Rationale |
|----------|--------------------|--------|-----------|
| Cache library | `IDistributedCache` + StackExchangeRedis vs raw `IConnectionMultiplexer` | `IConnectionMultiplexer` (StackExchange.Redis) directly | Need `SCAN` for key pattern invalidation; `IDistributedCache` doesn't support key enumeration/deletion by pattern |
| Cache key format | Flat key vs hierarchical | Hierarchical: `tenant:{tenantId}:patients:list:{branchId\|all}` | Clear structure, easy pattern matching for invalidation, tenant-scoped by design |
| Invalidation strategy | Versioned keys (bump version) vs Direct key deletion | Direct key deletion via `SCAN` + `DEL` on pattern `tenant:{tenantId}:patients:*` | Simpler implementation; version-based requires additional key tracking; `SCAN` is non-blocking and safe for production |
| What triggers invalidation | Only Create Patient vs Create Patient + Create Appointment | Both: Create Patient invalidates `tenant:{tenantId}:patients:*`; Create Appointment also invalidates `tenant:{tenantId}:patients:*` (patient list may show appointment-related data in future) | Per assignment spec — both mutations must invalidate |
| Cache TTL | Short (30s) vs Medium (5min) vs Long (30min) | 5 minutes (300 seconds) | Balance between freshness and performance; explicit invalidation handles mutation staleness |
| Serialization | `System.Text.Json` vs MessagePack | `System.Text.Json` | Already used in the project; no extra dependency; good enough for v1 |
| Cache layer placement | Controller-level vs Service-level decorator vs Inline in service | Service-level: dedicated `ICacheService` abstraction wrapping Redis operations; cache logic in `PatientService` | Keeps caching testable/mockable; service owns its cache behavior; avoids decorator complexity |
| Redis connection in tests | Real Redis container vs Mock/stub | Mock `ICacheService` in unit tests | No external dependency in tests; caching behavior tested via integration tests or mocked interface |

---

## Deliverables

- [x] NuGet package: `StackExchange.Redis` added to `ClinicPos.Api.csproj`
- [x] `ICacheService` interface — `GetAsync<T>`, `SetAsync<T>`, `InvalidateByPrefixAsync`
- [x] `RedisCacheService` implementation using `IConnectionMultiplexer`
- [x] Redis connection string wired in `Program.cs` (via `ConnectionStrings:Redis` config)
- [x] Redis connection string added to `docker-compose.yml` backend environment
- [x] Redis connection string added to `.env.example`
- [x] `appsettings.json` / `appsettings.Development.json` updated with Redis connection string
- [x] `PatientService.ListAsync` — check cache first, return cached if hit, populate cache on miss
- [x] `PatientService.CreateAsync` — invalidate `tenant:{tenantId}:patients:*` after successful create
- [x] `AppointmentService.CreateAsync` — invalidate `tenant:{tenantId}:patients:*` after successful create
- [x] Tests: cache hit returns cached data, cache miss queries DB and populates cache
- [x] Tests: Create Patient invalidates patient cache keys
- [x] Tests: Create Appointment invalidates patient cache keys
- [x] Tests: tenant A cache keys never leak to tenant B

---

## API Endpoints

No new endpoints. Behavior changes to existing endpoints:

| Method | Path | Change |
|--------|------|--------|
| `GET` | `/api/v1/patients?branchId=` | Now served from Redis cache when available; falls back to DB on cache miss |
| `POST` | `/api/v1/patients` | After successful create, invalidates `tenant:{tenantId}:patients:*` cache keys |
| `POST` | `/api/v1/appointments` | After successful create, invalidates `tenant:{tenantId}:patients:*` cache keys |

---

## Database Changes

None. This plan only adds a caching layer — no schema or migration changes.

---

## Cache Key Strategy (D2)

### Format

```
tenant:{tenantId}:patients:list:{branchId|all}
```

### Examples

| Scenario | Cache Key |
|----------|-----------|
| List all patients for tenant `abc-123` | `tenant:abc-123:patients:list:all` |
| List patients filtered by branch `def-456` for tenant `abc-123` | `tenant:abc-123:patients:list:def-456` |

### Invalidation Pattern

On Create Patient or Create Appointment, delete all keys matching:
```
tenant:{tenantId}:patients:*
```

This uses Redis `SCAN` with pattern matching to find and delete all patient cache keys for the affected tenant. Only the mutating tenant's cache is invalidated — other tenants' caches remain intact.

### TTL

All cache entries expire after **300 seconds (5 minutes)** as a safety net against stale data in case invalidation is missed.

---

## Implementation Details

### ICacheService Interface

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task InvalidateByPrefixAsync(string prefix);
}
```

### RedisCacheService

- Uses `IConnectionMultiplexer` injected via DI
- `GetAsync` — `StringGetAsync` → deserialize with `JsonSerializer`
- `SetAsync` — serialize with `JsonSerializer` → `StringSetAsync` with TTL
- `InvalidateByPrefixAsync` — `SCAN` for keys matching `{prefix}*`, batch `DEL`

### PatientService Changes

```csharp
// ListAsync — cache-aside pattern
public async Task<List<PatientResponse>> ListAsync(Guid? branchId)
{
    var tenantId = _tenantProvider.TenantId;
    var cacheKey = $"tenant:{tenantId}:patients:list:{branchId?.ToString() ?? "all"}";

    var cached = await _cacheService.GetAsync<List<PatientResponse>>(cacheKey);
    if (cached is not null)
        return cached;

    // ... existing DB query ...

    await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromSeconds(300));
    return result;
}

// CreateAsync — invalidate after create
public async Task<PatientResponse> CreateAsync(CreatePatientRequest request)
{
    // ... existing create logic ...

    await _cacheService.InvalidateByPrefixAsync($"tenant:{request.TenantId}:patients:");
    return response;
}
```

### AppointmentService Changes

```csharp
// CreateAsync — invalidate patient cache after appointment create
public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request)
{
    // ... existing create logic ...

    var tenantId = _tenantProvider.TenantId;
    await _cacheService.InvalidateByPrefixAsync($"tenant:{tenantId}:patients:");
    return response;
}
```

### DI Registration in Program.cs

```csharp
// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
```

---

## Tenant Safety Checklist

- [ ] All new tenant-scoped tables include `tenant_id` column — N/A (no new tables)
- [ ] EF Core `HasQueryFilter` configured for new entities — N/A (no new entities)
- [x] Cache keys prefixed with `tenant:{tenant_id}:` — enforced by key format
- [ ] Messages include `TenantId` in envelope — N/A (no new messages)
- [x] No cross-tenant data leakage possible — cache keys are tenant-scoped; invalidation only affects the mutating tenant; test verifies isolation

---

## Testing Requirements

- [x] **Cache hit:** Given patients are cached for Tenant A, When listing patients for Tenant A, Then return cached data without querying DB
- [x] **Cache miss:** Given no cache exists for Tenant A, When listing patients for Tenant A, Then query DB, return results, and populate cache
- [x] **Create Patient invalidation:** Given patients are cached for Tenant A, When a new patient is created for Tenant A, Then patient cache keys for Tenant A are invalidated
- [x] **Create Appointment invalidation:** Given patients are cached for Tenant A, When a new appointment is created for Tenant A, Then patient cache keys for Tenant A are invalidated
- [x] **Tenant cache isolation:** Given patients are cached for Tenant A and Tenant B, When Tenant A's cache is invalidated, Then Tenant B's cache remains intact

Tests must be BDD-style (`Given_When_Then` or `Should_`). Tests should mock `ICacheService` to verify caching behavior without requiring a running Redis instance.

---

## Configuration

### Environment Variables (added)

| Variable | Example | Description |
|----------|---------|-------------|
| `REDIS_HOST` | `redis` | Redis hostname (for docker-compose) |
| `REDIS_PORT` | `6379` | Redis port |

### Connection Strings

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

```yaml
# docker-compose.yml backend environment
ConnectionStrings__Redis: "redis:6379"
```

---

## File Changes Summary

| File | Change |
|------|--------|
| `ClinicPos.Api.csproj` | Add `StackExchange.Redis` NuGet package |
| `Program.cs` | Register `IConnectionMultiplexer` and `ICacheService` in DI |
| `Services/ICacheService.cs` | New — cache abstraction interface |
| `Services/RedisCacheService.cs` | New — Redis-backed implementation |
| `Services/PatientService.cs` | Add cache-aside to `ListAsync`, invalidation to `CreateAsync` |
| `Services/AppointmentService.cs` | Add cache invalidation to `CreateAsync` |
| `appsettings.json` | Add `ConnectionStrings:Redis` |
| `appsettings.Development.json` | Add `ConnectionStrings:Redis` |
| `docker-compose.yml` | Add `ConnectionStrings__Redis` to backend environment |
| `.env.example` | Document Redis connection (if applicable) |
| Tests | New test class for caching behavior |

---

## Verification Checklist

1. `docker compose up --build` — backend connects to Redis without errors
2. `GET /api/v1/patients` — first call hits DB, second call within 5min served from cache (observable via logs or timing)
3. `POST /api/v1/patients` — subsequent `GET /api/v1/patients` reflects new patient (cache invalidated)
4. `POST /api/v1/appointments` — patient cache invalidated
5. Two different tenant tokens — each tenant gets its own cached data, no cross-tenant leakage
6. `dotnet test` — all new and existing tests pass

---

## Notes / Open Questions

- Redis `SCAN` is O(N) over key space but non-blocking (cursor-based). For v1 with small key space this is perfectly acceptable. If key count grows significantly, consider Redis key tags or hash-based grouping.
- `ICacheService` is registered as Singleton because `IConnectionMultiplexer` is thread-safe and singleton by design.
- If Redis is temporarily unavailable, the cache service should degrade gracefully (log warning, fall through to DB). This prevents Redis outages from taking down the API.
