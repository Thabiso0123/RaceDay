# RaceDay

A full-stack event management platform for South African road running, walking and cycling events.

> PROG6212 – Portfolio of Evidence (Parts 1 to 3)
> Student: Thabiso Ntlatleng

## Description

RaceDay lets **organisers** create and manage running, cycling and walking events and lets **participants** browse events, enrol in a category and view their results once an event has taken place.

- **Organisers** create, update and delete events (including distance and categories), view enrolments, upload event banners and capture finish times and positions after an event.
- **Participants** register, log in, update their own profile and picture, browse events, enrol by choosing a category and view their own race history.

The system progresses across three parts: database planning (Part 1), an ASP.NET Core Web API (Part 2), and a containerised ASP.NET Core MVC front end with Azure Blob Storage and CI (Part 3).

## Architecture

```
Browser --> RaceDay.Web (MVC, session) --> RaceDay.Api (REST, cookie auth) --> SQL Server
                                                   \--> Azure Blob Storage (images)
```

The MVC app has no database access. It only calls the API, which owns the business rules, the SQL Server database and the Azure Blob container that holds banner images and profile pictures.

## Repository structure

```
RaceDay.Api/
  RaceDay.Api/      Part 2 – ASP.NET Core Web API (EF Core, Swagger, Dockerfile)
  RaceDay.Tests/    xUnit tests for auth, events, enrolments and results
Part3/
  RaceDay.Web/      Part 3 – ASP.NET Core MVC front end (Dockerfile, README)
docs/               Part 1 – ERD, SQL schema and seed data, requirements, API planning
.github/workflows/  GitHub Actions CI (build, test, Docker image builds)
```

See [`Part3/RaceDay.Web/README.md`](Part3/RaceDay.Web/README.md) for the full architecture table and the Part 3 API additions.

## Tech stack

- **ASP.NET Core 8** Web API and MVC
- **Entity Framework Core** with **SQL Server**
- **Azure Blob Storage** (Azurite emulator for local development)
- **Docker** and Docker Compose
- **GitHub Actions** for CI
- **xUnit** tests
- Swagger / OpenAPI

## Database

The schema covers the `Role`, `Participant`, `Event`, `Category`, `Enrolment` and `Result` tables, including all primary keys, foreign keys and constraints. It is defined in [`docs/RaceDayDB.sql`](docs/RaceDayDB.sql), which also seeds sample organisers, participants, events, categories, enrolments and results so you can test straight after setup.

## API overview

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `/login`, `/logout` |
| Events | `GET/POST /api/events`, `GET /api/events/mine`, `GET/PUT/DELETE /api/events/{id}`, `POST /api/events/{id}/banner` |
| Categories | `GET/POST /api/events/{id}/categories` |
| Enrolments | `GET/POST /api/events/{id}/enrolments`, `GET /api/me/enrolments` |
| Results | `GET/POST /api/events/{id}/results`, `GET /api/events/{id}/results/all`, `GET /api/me/results` |
| Profile | `GET/PUT /api/users/{id}`, `POST /api/users/{id}/picture` |
| Files | `GET /api/files/{path}` |

Swagger UI is available at `/swagger` when the API is running.

## Running the project

### Option 1 – Docker (recommended)
Requires Docker Desktop.

1. Copy `.env.example` to `.env` and set `AZURE_STORAGE_CONNECTION_STRING`. For local use, the Azurite development connection string works.
2. From the folder that contains `docker-compose.yml`, run:
   ```bash
   docker compose up --build
   ```
3. Open **http://localhost:8080** for the MVC app and **http://localhost:5100/swagger** for the API.

The first start takes about a minute while SQL Server comes up. The API retries until the database is ready.

### Option 2 – Run locally without Docker
1. Start SQL Server and run `docs/RaceDayDB.sql` to create and seed the database.
2. Start Azurite, or set `AzureBlob:ConnectionString` to a real storage account.
3. Run the API: `dotnet run --project RaceDay.Api/RaceDay.Api`
4. Run the web app: `dotnet run --project Part3/RaceDay.Web`
5. Check that `Api:BaseUrl` in `Part3/RaceDay.Web/appsettings.json` matches the API address (default `http://localhost:5100/`).

Set your own database password and storage connection string through environment variables or user secrets. Do not commit real credentials.

## Tests
```bash
dotnet test RaceDay.Api/RaceDay.Tests/RaceDay.Tests.csproj
```

## Continuous integration
The workflow in `.github/workflows/ci.yml` restores and builds the API and MVC projects, runs the tests and builds both Docker images on every push to `main` or a `part3` branch, and on pull requests to `main`.

<!-- Keep your own screenshots below. Paste the image links from your original README. -->

## Database table screenshot
Paste your database screenshot here.

## GitHub Actions workflow
Paste your workflow screenshots here.

## YouTube video
https://youtu.be/TaWmN6JPLIc

## References
Anthropic (2026) *Claude (Claude Sonnet 5)* [Large language model]. Available at: https://claude.ai (Accessed: 21 September 2026).
