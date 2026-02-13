# Clinic POS Platform — Operational Runbook

> **Quick reference guide** for developers, operators, and new team members.  
> Last updated: February 13, 2026

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Prerequisites](#prerequisites)
3. [First-Time Setup](#first-time-setup)
4. [Daily Development Workflow](#daily-development-workflow)
5. [Running the Application](#running-the-application)
6. [Testing](#testing)
7. [Database Operations](#database-operations)
8. [Troubleshooting](#troubleshooting)
9. [Common Tasks](#common-tasks)
10. [Architecture Reference](#architecture-reference)
11. [API Reference](#api-reference)
12. [Security Guidelines](#security-guidelines)

---

## Quick Start

**For experienced developers who want to get running immediately:**

```bash
# Clone and enter project
git clone <repository-url>
cd Arwn

# Copy environment file
cp .env.example .env

# Start everything
docker compose up --build
```

**Access points:**
- Frontend UI: http://localhost:3000
- Backend API: http://localhost:5000
- RabbitMQ Management: http://localhost:15672 (guest/guest)

**Default test tenant:** `a0000000-0000-0000-0000-000000000001`

---

## Prerequisites

### Required Software

| Tool | Version | Purpose |
|------|---------|---------|
| Docker | 20.10+ | Container runtime |
| Docker Compose | 2.0+ | Multi-container orchestration |
| Git | 2.30+ | Version control |

### Optional (for local development without Docker)

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0 | Backend development |
| Node.js | 20+ LTS | Frontend development |
| PostgreSQL | 16+ | Database (if running locally) |
| Redis | 7+ | Cache (if running locally) |
| RabbitMQ | 3+ | Message broker (if running locally) |

### Installation Links

- **Docker Desktop:** https://www.docker.com/products/docker-desktop/
- **.NET 10 SDK:** https://dotnet.microsoft.com/download/dotnet/10.0
- **Node.js:** https://nodejs.org/

---

## First-Time Setup

### 1. Clone Repository

```bash
git clone <repository-url>
cd Arwn
```

### 2. Configure Environment

```bash
# Copy the example environment file
cp .env.example .env

# Edit .env if needed (defaults work for local dev)
```

**Important environment variables:**

| Variable | Default | Description |
|----------|---------|-------------|
| `POSTGRES_USER` | `clinicpos` | PostgreSQL username |
| `POSTGRES_PASSWORD` | `clinicpos_dev` | PostgreSQL password |
| `POSTGRES_DB` | `clinicpos` | Database name |
| `RABBITMQ_DEFAULT_USER` | `guest` | RabbitMQ username |
| `RABBITMQ_DEFAULT_PASS` | `guest` | RabbitMQ password |
| `NEXT_PUBLIC_API_URL` | `http://localhost:5000` | Backend API URL for frontend |
| `NEXT_PUBLIC_TENANT_ID` | `a0000000-...0001` | Default tenant ID |

### 3. Start Services

```bash
docker compose up --build
```

**What happens on first run:**
- PostgreSQL, Redis, RabbitMQ containers start
- Backend builds and applies database migrations
- Seed data is inserted (tenant, branches)
- Frontend builds and starts

**Wait for:** "Application started" message in backend logs.

### 4. Verify Installation

**Backend health check:**
```bash
curl http://localhost:5000/health
```

**List seeded branches:**
```bash
curl -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     http://localhost:5000/api/v1/branches
```

**Open frontend:**
```
http://localhost:3000
```

---

## Daily Development Workflow

### Starting Your Day

```bash
# Start all services
docker compose up

# Or run in detached mode
docker compose up -d

# View logs
docker compose logs -f backend
docker compose logs -f frontend
```

### Making Changes

#### Backend Development

```bash
# Enter backend container (optional, for debugging)
docker compose exec backend bash

# Run tests
cd src/backend
dotnet test

# Add new migration
cd src/backend
dotnet ef migrations add MigrationName

# View logs
docker compose logs -f backend
```

#### Frontend Development

```bash
# Frontend has hot-reload enabled
# Just edit files in src/frontend/ and see changes immediately

# Run tests
cd src/frontend
npm test

# Run linter
npm run lint
```

### Stopping Services

```bash
# Stop services (preserves data)
docker compose stop

# Stop and remove containers (preserves volumes)
docker compose down

# Nuclear option: remove everything including volumes
docker compose down -v
```

---

## Running the Application

### Full Stack (Recommended)

```bash
docker compose up --build
```

### Individual Services

```bash
# Start only database
docker compose up postgres

# Start database and backend
docker compose up postgres redis rabbitmq backend

# View specific service logs
docker compose logs -f backend
docker compose logs -f frontend
```

### Local Development (without Docker)

#### Backend

```bash
# Prerequisites: PostgreSQL, Redis, RabbitMQ running locally
cd src/backend

# Update appsettings.Development.json with local connection strings
# Run backend
dotnet run
```

#### Frontend

```bash
cd src/frontend

# Install dependencies
npm install

# Run dev server
npm run dev
```

---

## Testing

### Backend Tests

```bash
# Run all backend tests
cd src/backend
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~TenantScopingTests"

# Run in watch mode
dotnet watch test
```

### Frontend Tests

```bash
cd src/frontend

# Run all tests
npm test

# Run in watch mode
npm test -- --watch

# Run specific test file
npm test -- PatientList.test.tsx

# Run with coverage
npm test -- --coverage
```

### Integration Tests

```bash
# Backend integration tests use test containers
cd src/backend
dotnet test --filter "Category=Integration"
```

---

## Database Operations

### Migrations

#### Create New Migration

```bash
cd src/backend
dotnet ef migrations add MigrationName
```

#### Apply Migrations

```bash
# Automatic on startup (default behavior)
docker compose up backend

# Manual (if needed)
cd src/backend
dotnet ef database update
```

#### Rollback Migration

```bash
cd src/backend
dotnet ef database update PreviousMigrationName
```

#### Remove Last Migration

```bash
cd src/backend
dotnet ef migrations remove
```

### Database Access

#### Connect via psql

```bash
docker compose exec postgres psql -U clinicpos -d clinicpos
```

#### Useful SQL Queries

```sql
-- List all tables
\dt

-- View table structure
\d patients

-- Check tenant data
SELECT id, name, created_at FROM tenants;

-- Check branches
SELECT id, tenant_id, name FROM branches;

-- Count patients per tenant
SELECT tenant_id, COUNT(*) FROM patients GROUP BY tenant_id;

-- View all appointments
SELECT id, tenant_id, patient_id, scheduled_at, status FROM appointments;
```

### Seed Data

**Default seeded data (applied on first run):**

| Entity | ID | Name |
|--------|-----|------|
| Tenant | `a0000000-0000-0000-0000-000000000001` | Demo Clinic |
| Branch | `b0000000-0000-0000-0000-000000000001` | Main Branch |
| Branch | `b0000000-0000-0000-0000-000000000002` | Downtown Branch |

**Reset database and reseed:**

```bash
# Stop services
docker compose down

# Remove volume
docker volume rm arwn_pgdata

# Restart (will recreate and reseed)
docker compose up --build
```

---

## Troubleshooting

### Common Issues

#### Port Already in Use

**Symptom:** Error: "Bind for 0.0.0.0:5432 failed: port is already allocated"

**Solution:**
```bash
# Find process using port
netstat -ano | findstr :5432  # Windows
lsof -i :5432                  # Mac/Linux

# Stop Docker Compose
docker compose down

# Change port in docker-compose.yml or stop conflicting service
```

#### Migrations Not Applying

**Symptom:** Backend starts but tables don't exist

**Solution:**
```bash
# Remove containers and volumes
docker compose down -v

# Rebuild and restart
docker compose up --build
```

#### Backend Can't Connect to Database

**Symptom:** "Connection refused" or "Server not found"

**Solution:**
```bash
# Check if PostgreSQL is healthy
docker compose ps

# View PostgreSQL logs
docker compose logs postgres

# Restart services in order
docker compose down
docker compose up postgres
docker compose up backend
```

#### Frontend Can't Reach Backend

**Symptom:** Frontend shows connection errors

**Solution:**
```bash
# Check backend is running
curl http://localhost:5000/health

# Check environment variable in frontend
docker compose exec frontend env | grep NEXT_PUBLIC_API_URL

# Restart frontend
docker compose restart frontend
```

#### RabbitMQ Connection Failed

**Symptom:** "RabbitMQ connection refused"

**Solution:**
```bash
# Check RabbitMQ is running
docker compose ps rabbitmq

# View RabbitMQ logs
docker compose logs rabbitmq

# Access RabbitMQ management UI
# http://localhost:15672 (guest/guest)
```

### Logs and Debugging

```bash
# View all logs
docker compose logs

# Follow logs in real-time
docker compose logs -f

# View specific service logs
docker compose logs backend
docker compose logs postgres

# View last 100 lines
docker compose logs --tail=100 backend

# Save logs to file
docker compose logs > debug.log
```

### Clean Slate Reset

```bash
# Nuclear option: remove everything
docker compose down -v
docker system prune -a --volumes

# Restart
docker compose up --build
```

---

## Common Tasks

### Create a New Patient

```bash
curl -X POST \
     -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     -H "Content-Type: application/json" \
     -d '{
       "firstName": "John",
       "lastName": "Doe",
       "phoneNumber": "0812345678",
       "primaryBranchId": "b0000000-0000-0000-0000-000000000001"
     }' \
     http://localhost:5000/api/v1/patients
```

### List All Patients

```bash
curl -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     http://localhost:5000/api/v1/patients
```

### Create User

```bash
curl -X POST \
     -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     -H "Content-Type: application/json" \
     -d '{
       "email": "admin@demo.com",
       "password": "Password123!",
       "fullName": "Admin User"
     }' \
     http://localhost:5000/api/v1/users
```

### Create Appointment

```bash
curl -X POST \
     -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     -H "Content-Type: application/json" \
     -d '{
       "patientId": "<patient-guid>",
       "branchId": "b0000000-0000-0000-0000-000000000001",
       "scheduledAt": "2026-02-20T10:00:00Z",
       "reason": "Annual checkup"
     }' \
     http://localhost:5000/api/v1/appointments
```

### List Appointments

```bash
curl -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
     http://localhost:5000/api/v1/appointments
```

### View RabbitMQ Messages

1. Open http://localhost:15672
2. Login: `guest` / `guest`
3. Navigate to "Queues" tab
4. View messages in queues

---

## Architecture Reference

### Multi-Tenancy Model

**Approach:** Shared schema with tenant isolation via EF Core global query filters.

**Key principles:**
- Every tenant-scoped entity has a `tenant_id` column
- `HasQueryFilter` on DbContext automatically injects `WHERE tenant_id = @tenantId`
- `ITenantProvider` resolves current tenant from `X-Tenant-Id` header (v1) or JWT claims (future)
- No manual tenant filtering in business logic

**Tenant-scoped entities:**
- Patient
- Branch
- User
- Appointment

**Global entities:**
- Tenant
- Role

### Data Flow

```
HTTP Request (X-Tenant-Id header)
    ↓
Middleware → ITenantProvider → HttpContext
    ↓
Controller → Service
    ↓
Repository → DbContext (with query filter WHERE tenant_id = @tenantId)
    ↓
PostgreSQL
```

### Technology Stack

| Layer | Technology | Port |
|-------|------------|------|
| Frontend | Next.js 15 (React, App Router, TypeScript) | 3000 |
| Backend | .NET 10 / C# (Web API) | 5000 |
| Database | PostgreSQL 16 | 5432 |
| Cache | Redis 7 (provisioned, unused in v1) | 6379 |
| Messaging | RabbitMQ 3 (active) | 5672, 15672 |

### Project Structure

```
/
├── src/
│   ├── backend/
│   │   ├── Controllers/         # API endpoints
│   │   ├── Services/            # Business logic
│   │   ├── Data/                # DbContext, migrations
│   │   ├── Entities/            # Domain models
│   │   ├── Dtos/                # Request/response objects
│   │   ├── Middleware/          # Tenant resolution, exception handling
│   │   ├── Auth/                # Authentication handlers
│   │   ├── Exceptions/          # Custom exceptions
│   │   ├── Validators/          # Request validation
│   │   └── tests/               # Unit & integration tests
│   │
│   └── frontend/
│       ├── app/                 # Next.js pages (App Router)
│       ├── lib/                 # Utilities, API client
│       ├── types/               # TypeScript types
│       └── __tests__/           # Jest tests
│
├── plans/                       # Feature plans
├── docker-compose.yml           # Infrastructure definition
├── .env.example                 # Environment template
├── README.md                    # Project overview
├── PLAN.md                      # Feature plan index
├── AI_PROMPTS.md                # Prompt history
└── RUNBOOK.md                   # This file
```

---

## API Reference

### Base URL

```
http://localhost:5000/api/v1
```

### Required Headers

All requests must include:

```
X-Tenant-Id: a0000000-0000-0000-0000-000000000001
```

### Endpoints

#### Patients

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/patients` | List all patients (filtered by tenant) |
| `GET` | `/patients?branchId={guid}` | List patients by branch |
| `GET` | `/patients/{id}` | Get patient by ID |
| `POST` | `/patients` | Create new patient |
| `PUT` | `/patients/{id}` | Update patient |
| `DELETE` | `/patients/{id}` | Soft delete patient |

#### Branches

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/branches` | List all branches (filtered by tenant) |
| `GET` | `/branches/{id}` | Get branch by ID |

#### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/users` | List all users (filtered by tenant) |
| `GET` | `/users/{id}` | Get user by ID |
| `POST` | `/users` | Create new user |
| `POST` | `/users/{id}/assign-role` | Assign role to user |
| `POST` | `/users/{id}/associate-branches` | Associate user with branches |

#### Appointments

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/appointments` | List appointments (filtered by tenant) |
| `GET` | `/appointments?date={yyyy-MM-dd}` | List by date |
| `GET` | `/appointments?branchId={guid}` | List by branch |
| `GET` | `/appointments/{id}` | Get appointment by ID |
| `POST` | `/appointments` | Create appointment |
| `PUT` | `/appointments/{id}` | Update appointment |

### Request Examples

**Create Patient:**
```bash
curl -X POST http://localhost:5000/api/v1/patients \
  -H "X-Tenant-Id: a0000000-0000-0000-0000-000000000001" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "phoneNumber": "0899999999",
    "primaryBranchId": "b0000000-0000-0000-0000-000000000001",
    "dateOfBirth": "1990-05-15",
    "gender": "Female",
    "address": "123 Main St"
  }'
```

**Response:**
```json
{
  "id": "...",
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "0899999999",
  "primaryBranchId": "b0000000-0000-0000-0000-000000000001",
  "dateOfBirth": "1990-05-15",
  "gender": "Female",
  "address": "123 Main St",
  "createdAt": "2026-02-13T12:00:00Z"
}
```

### Error Responses

**400 Bad Request:**
```json
{
  "title": "One or more validation errors occurred",
  "errors": {
    "PhoneNumber": ["Phone number is required"]
  }
}
```

**404 Not Found:**
```json
{
  "title": "Not Found",
  "detail": "Patient not found"
}
```

**409 Conflict:**
```json
{
  "title": "Duplicate Phone Number",
  "detail": "A patient with phone number 0812345678 already exists"
}
```

---

## Security Guidelines

### Tenant Isolation

**Critical rules:**
1. **Never** trust tenant ID from request body or URL path for authorization
2. **Always** resolve tenant ID from authenticated context (`X-Tenant-Id` header in v1)
3. **Never** manually filter by tenant in services — DbContext handles it via `HasQueryFilter`
4. **Test** tenant isolation for every new feature

### Input Validation

- All requests are validated at controller level using FluentValidation
- Phone numbers validated against format
- Required fields enforced
- GUIDs validated

### Authentication (v1)

- **Current:** Tenant ID from `X-Tenant-Id` header (development only)
- **Future:** JWT tokens with tenant claim, role-based access control

### Database

- No SQL injection possible (using EF Core parameterized queries)
- Soft delete only (`is_deleted` flag)
- Composite unique indexes are tenant-scoped

### Environment Variables

- **Never** commit secrets to version control
- Use `.env` for local development
- Use secure secret management in production

---

## Production Considerations

**Note:** This is v1 MVP. The following are recommendations for production deployment:

### Deployment Checklist

- [ ] Use JWT authentication instead of `X-Tenant-Id` header
- [ ] Configure CORS with specific origins (no wildcard)
- [ ] Enable HTTPS (TLS certificates via Let's Encrypt or cloud provider)
- [ ] Use managed database (AWS RDS, Azure Database for PostgreSQL)
- [ ] Use managed Redis (AWS ElastiCache, Azure Cache for Redis)
- [ ] Use managed message broker (AWS MQ, Azure Service Bus)
- [ ] Store secrets in vault (AWS Secrets Manager, Azure Key Vault)
- [ ] Run database migrations via deployment pipeline (not auto-migrate)
- [ ] Configure health checks for load balancer
- [ ] Set up monitoring and alerting (Prometheus, Grafana, Datadog)
- [ ] Configure logging aggregation (ELK stack, CloudWatch)
- [ ] Set up backup and disaster recovery
- [ ] Load test for expected traffic
- [ ] Enable rate limiting
- [ ] Configure CDN for frontend assets

### Environment-Specific Configuration

**Development:**
- Auto-migrate on startup
- Seed data on first run
- Debug logging enabled
- HTTP allowed

**Production:**
- Manual migration via pipeline
- No seed data
- Info/Warning logging
- HTTPS required
- Database connection pooling
- Redis connection string with TLS

---

## Support and Resources

### Documentation

- [README.md](README.md) — Project overview
- [PLAN.md](PLAN.md) — Feature plan index
- [plans/](plans/) — Detailed feature plans
- [.github/copilot-instructions.md](.github/copilot-instructions.md) — AI agent guidelines

### Quick Commands Reference

```bash
# Start everything
docker compose up --build

# Stop everything
docker compose down

# Reset database
docker compose down -v && docker compose up --build

# Run backend tests
cd src/backend && dotnet test

# Run frontend tests
cd src/frontend && npm test

# View logs
docker compose logs -f backend

# Connect to database
docker compose exec postgres psql -U clinicpos -d clinicpos

# Access RabbitMQ UI
open http://localhost:15672
```

### Health Check Endpoints

```bash
# Backend health
curl http://localhost:5000/health

# PostgreSQL
docker compose exec postgres pg_isready -U clinicpos

# Redis
docker compose exec redis redis-cli ping

# RabbitMQ
docker compose exec rabbitmq rabbitmq-diagnostics check_running
```

---

**Last Updated:** February 13, 2026  
**Version:** 1.0  
**Maintained By:** Development Team
