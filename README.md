# ExamDesk

ExamDesk is an ASP.NET Core 9 exam-management system with a server-rendered Razor MVC frontend (`.cshtml` views). It helps administrators, instructors, and students manage batches, exams, questions, submissions, grading, and results.

## Live application

**https://examdesk-36th.onrender.com/Account/Login**

The free Render instance may sleep after inactivity, so the first request can take up to about a minute to wake up.

## Features

- Role-based authentication for Admin, Instructor, and Student users
- Razor MVC login and registration pages
- Admin and instructor dashboards
- Batch creation, editing, deletion, status, and enrollment management
- Exam scheduling with Bangladesh time (UTC+6)
- Exam editing and deletion with confirmation and transactional cleanup
- Question PDF upload and replacement, including after an exam has started
- Student enrollment, question access, and image-based answer submissions
- One final submission per student with file-size/type validation
- Instructor grading, feedback, publishing, and locked results
- Per-exam leaderboard with ties and ranks
- Batch leaderboard with total marks, available marks, and percentage across published exams
- Existing JWT API routes under `/api` for programmatic clients

## Technology stack

- .NET 9 / ASP.NET Core MVC
- Razor `.cshtml` views and custom CSS (no Node.js frontend build required)
- ASP.NET Core Identity for users, passwords, lockout, and roles
- Entity Framework Core
- PostgreSQL for deployment (Neon is supported)
- SQLite for local development
- Docker for reproducible builds and deployment
- Render for the live web service

## Data and storage

The deployed application stores relational data in PostgreSQL. The database contains users, roles, batches, exams, enrollments, question records, submissions, evaluations, results, and audit data.

Local development uses `src/ExamManagement.API/exam-management.db` (SQLite). Uploaded files are currently stored under `src/ExamManagement.API/App_Data/uploads` on local disk. Render's filesystem is ephemeral, so production deployments should use Cloudinary or S3-compatible object storage before relying on permanent uploads.

## Run locally

Use .NET SDK 9.0.315 or a newer 9.0 patch:

```bash
dotnet restore
dotnet run --project src/ExamManagement.API
```

Open http://localhost:5080. A fresh local admin can be created by setting private environment variables:

```bash
export SeedAdmin__Email='admin@example.com'
export SeedAdmin__Password='Use-a-private-password-1'
dotnet run --project src/ExamManagement.API
```

To use PostgreSQL locally instead of SQLite:

```bash
export Database__Provider=Postgres
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=exam_management;Username=postgres;Password=postgres'
```

Never commit passwords, JWT signing keys, or database connection strings to GitHub.

## Run with Docker

Build and run the production container:

```bash
docker build -t examdesk .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Database__Provider=Postgres \
  -e ConnectionStrings__DefaultConnection='YOUR_POSTGRES_CONNECTION_STRING' \
  -e Jwt__SigningKey='YOUR_32_BYTE_OR_LONGER_SECRET' \
  -e SeedAdmin__Email='YOUR_ADMIN_EMAIL' \
  -e SeedAdmin__Password='YOUR_ADMIN_PASSWORD' \
  examdesk
```

Open http://localhost:8080.

## Deploy to Render

1. Push this repository to GitHub.
2. In Render, create a **Web Service** from `SonyZaman/ExamDesk`.
3. Select the Docker runtime and use the repository `Dockerfile`.
4. Add these environment variables in **ExamDesk → Environment**:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `Database__Provider=Postgres`
   - `ConnectionStrings__DefaultConnection` = your Neon pooled PostgreSQL URI
   - `Jwt__SigningKey` = a unique secret of at least 32 bytes
   - `SeedAdmin__Email` = your admin email
   - `SeedAdmin__Password` = your private admin password
   - `SeedAdmin__Name` = your admin display name
5. Save and deploy. Wait for **Deploy succeeded | Live**.
6. Open the live login URL above.

The included `render.yaml` documents the same service and environment-variable contract. The database initializer creates or repairs the configured admin account on startup; it does not store a plaintext password in the repository.

## Verification

```bash
dotnet build ExamManagement.slnx
dotnet test ExamManagement.slnx
```

OpenAPI JSON is available at `/openapi/v1.json` in Development. See [RUNNING.md](RUNNING.md) for the detailed workflow, editing/deletion rules, and current limitations.

## Project structure

```text
src/ExamManagement.Domain          Domain entities and rules
src/ExamManagement.Application     Use cases and contracts
src/ExamManagement.Infrastructure  EF Core, Identity, persistence, services
src/ExamManagement.API             MVC controllers, Razor views, API endpoints
tests/                             Domain tests and smoke checks
Dockerfile                         Multi-stage .NET container build
render.yaml                        Render deployment configuration
```
