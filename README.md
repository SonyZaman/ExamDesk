# Exam Management System

**The Razor MVC frontend is now implemented. See [RUNNING.md](RUNNING.md) for current setup, development login, workflows, and limitations.** The backend foundation notes below describe the initial implementation; local startup now uses persistent SQLite.

Deployment starter files are included in `Dockerfile`, `.dockerignore`, and `render.yaml`. The deployed environment still needs a hosted PostgreSQL database and persistent object storage for uploaded PDFs/images; the current controller uses local disk storage for development.

Backend foundation for an exam platform with Admin, Instructor, and Student roles.

## Implemented

- ASP.NET Core Web API with JWT authentication
- ASP.NET Core Identity roles and users
- Clean Architecture project boundaries
- Domain model for batches, exams, enrollments, submissions, evaluations, results, and audit logs
- Instructor/Admin batch creation
- Authenticated batch discovery
- Student enrollment with duplicate and deadline protection
- PostgreSQL support, with an in-memory database for local startup
- Domain tests for timing, marks, and enrollment rules

## Run locally

The solution targets `net9.0`. A .NET 10 upgrade requires updating the SDK pin, target framework, and framework package versions together.

```bash
dotnet restore
dotnet run --project src/ExamManagement.API
```

OpenAPI JSON is available at `http://localhost:5080/openapi/v1.json` in Development.

Local startup now uses persistent SQLite. To use PostgreSQL, set:

```bash
Database__Provider=Postgres
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=exam_management;Username=postgres;Password=postgres'
```

The development signing key lives only in `appsettings.Development.json`. Set `Jwt__SigningKey` to a strong secret (at least 32 bytes) in every deployed environment; startup fails when it is missing.

The starter currently creates the schema with `EnsureCreated`. Add and deploy the first EF Core migration before using a long-lived production database.

## First API flow

1. `POST /api/auth/register` creates a Student account.
2. `POST /api/auth/login` returns a bearer token.
3. An Instructor/Admin creates a batch with `POST /api/batches`.
4. Students browse `GET /api/batches` and enroll with `POST /api/batches/{id}/enroll`.

Public registration creates only students. The Razor admin user-management page can assign roles. See RUNNING.md for development admin credentials and deployment settings.
