# ExamDesk — Razor exam management

Runnable ASP.NET Core MVC application with server-rendered `.cshtml` views. No Node.js or frontend build tooling required.

## Run

Use .NET SDK 9.0.315 or a newer 9.0 patch. From the repository root:

```sh
dotnet restore
dotnet run --project src/ExamManagement.API
```

Open **http://localhost:5080**. Configure a local admin with `SeedAdmin__Email` and `SeedAdmin__Password` environment variables if you need a fresh database. Existing local data persists in `src/ExamManagement.API/exam-management.db`; private files live in `src/ExamManagement.API/App_Data/uploads`. Back up both together.

## Workflow

1. Sign in as admin and create a batch.
2. Register student accounts in another browser/private window. To create an instructor, register an account and assign Instructor using Admin → Manage users; sign in again afterward.
3. As instructor/admin, schedule an exam from a batch. All form times use Bangladesh time (UTC+6).
4. Upload or replace the PDF at any time, including after the exam starts. Students receive the latest upload during the question-access window. Students enroll from their dashboard.
5. Enrolled students download questions during the exam window and submit 1–5 JPG/PNG images (10 MB each maximum) before the submission deadline. Only one final submission is allowed.
6. Instructors open answer pages, save draft marks and feedback, then publish after the deadline and after grading every submission.
7. Missing submissions receive zero. Published marks are locked. Equal scores share ranks. Batch rankings use total marks / total available marks and appear after every exam is published.

## Verification

## Editing and deletion

Admins can manage all batches/exams; instructors can manage only their own. Use Edit batch on a dashboard card and Edit exam on the exam page. Batch name, description, enrollment deadline/status and exam title, full marks, release/end/submission times are editable. Times use Bangladesh time. Existing grades are preserved; full marks cannot be below an existing grade. Changing full marks recalculates leaderboard percentages.

Delete links open confirmation pages. Confirming exam deletion removes its question records, submissions, evaluations and results. Confirming batch deletion also removes every exam and enrollment in it. Database deletion is transactional. Private file bytes remain on disk, inaccessible through the app after their records are removed; storage cleanup is not implemented. Recovering deleted records requires a database backup.

## Verification commands

```sh
dotnet build ExamManagement.slnx
dotnet test ExamManagement.slnx
```

Views: `src/ExamManagement.API/Views`. Styles: `wwwroot/css/site.css`. Existing JWT API routes remain under `/api`. Development OpenAPI: `/openapi/v1.json`.

## Deployment boundary

This is a runnable local MVP. It targets .NET 9, the installed SDK. Upgrading to .NET 10 requires updating global.json, TargetFramework, and framework packages together.

For deployment, use HTTPS, a unique `Jwt__SigningKey` (at least 32 bytes), and private `SeedAdmin__Email` / `SeedAdmin__Password`. Remove or rotate the development admin when reusing a local database. PostgreSQL configuration remains available, but is not integration-tested here. Schema initialization uses EnsureCreated; add migrations before evolving a persistent deployed database.

Private local storage validates file headers and sizes but does not scan for malware. S3, email verification/password reset, notifications, audit-event recording, withdrawal, rescheduling, and grade revision history remain future work. Existing JWTs retain their roles until expiration (one hour); sign in again after role changes.
