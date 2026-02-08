# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

BoincWatcherService is a monitoring system for BOINC (Berkeley Open Infrastructure for Network Computing) clients. The solution consists of three main projects:

1. **BoincWatcherService** - A .NET Worker Service that monitors BOINC hosts via RPC, collects statistics, and stores them in PostgreSQL
2. **BoincStatsFunctionApp** - An Azure Functions app that exposes statistics via HTTP API using Azure Table Storage
3. **Common** - Shared models used by both projects

## Architecture

### Data Flow

```
BOINC Clients (RPC)
  ↓
BoincWatcherService (Jobs via Quartz)
  ↓
PostgreSQL (EF Core) → Azure Functions HTTP API
  ↓
Azure Table Storage
```

- **BoincService** connects to BOINC clients using the BoincRpc library to gather host states, project stats, and task information
- **Quartz scheduler** runs periodic jobs:
  - `StatsJob` - Collects and aggregates statistics from BOINC hosts
  - `BoincTaskJob` - Collects task-level information
  - `MailNotificationJob` - Sends email notifications based on host states (optional)
- **StatsService** stores data in PostgreSQL and optionally uploads aggregated stats to Azure Functions
- **FunctionAppService** communicates with the Azure Functions API to publish stats to Azure Table Storage

### Database

PostgreSQL database managed via EF Core with migrations. Key entities:
- `HostStats` - Per-host daily statistics (composite key: YYYYMMDD, HostName)
- `ProjectStats` - Per-project daily statistics (composite key: YYYYMMDD, ProjectName)
- `HostProjectStats` - Per-host-per-project statistics (composite key: YYYYMMDD, HostName, ProjectName)
- `BoincTask` - Task-level data (composite key: ProjectName, TaskName, HostName)
- `BoincApp` - Application information (composite key: ProjectName, Name)

All entities inherit from `Entity` base class with CreatedAt/UpdatedAt timestamps managed by `EntityInterceptor`.

## Development Commands

### Build and Run

```bash
# Build the entire solution
dotnet build

# Build specific project
dotnet build BoincWatcherService/BoincWatcherService.csproj
dotnet build BoincStatsFunctionApp/BoincStatsFunctionApp.csproj

# Run the worker service
dotnet run --project BoincWatcherService/BoincWatcherService.csproj

# Run Azure Functions locally
cd BoincStatsFunctionApp
func start
# or use VS Code task: "build (functions)" then "func: host start"
```

### Database Migrations

```bash
# Create a new migration (run from solution root)
dotnet ef migrations add MigrationName --project BoincWatcherService/BoincWatcherService.csproj

# Apply migrations (happens automatically on startup, but manual command is:)
dotnet ef database update --project BoincWatcherService/BoincWatcherService.csproj

# Remove last migration (if not applied)
dotnet ef migrations remove --project BoincWatcherService/BoincWatcherService.csproj
```

Note: Migrations are automatically applied on service startup via `context.Database.Migrate()` in Program.cs:23-32.

### Docker

```bash
# Build Docker image
docker build -f BoincWatcherService/Dockerfile -t boinc-watcher-service .

# Run container
docker run -d --name boinc-watcher boinc-watcher-service
```

## Configuration

Configuration uses `appsettings.json` with user secrets support in development. See `appsettings.example.json` for reference.

Required sections:
- `ConnectionStrings:BoincWatcher` - PostgreSQL connection string
- `BoincHosts[]` - Array of BOINC hosts to monitor (IP, Port, Password)
- `SchedulingOptions` - Cron schedules for StatsJob and BoincTaskJob
- `MailSettings` - SMTP configuration (optional, controlled by IsEnabled)
- `FunctionAppSettings` - Azure Functions endpoint (optional, controlled by IsEnabled)

The worker service uses user secrets in development mode (configured in Program.cs:39-41).

### Azure Functions Configuration

For local development, use `local.settings.json` in BoincStatsFunctionApp:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

For production, configure either:
- Connection string via `AzureWebJobsStorage`
- Managed identity via `AzureWebJobsStorage__tableServiceUri` and optional `AzureWebJobsStorage__clientId`

## Key Patterns

### Dependency Injection

Services are registered in Program.cs:43-72:
- `IBoincService` - Singleton (manages RPC connections)
- `IMailService` - Singleton
- `IStatsService` - Scoped (uses DbContext)
- `IFunctionAppService` - Scoped
- `StatsDbContext` - Scoped with retry logic and EntityInterceptor

### Job Scheduling

Quartz jobs are configured in Program.cs:74-120:
- Jobs fire immediately when debugger is attached (Program.cs:98-107)
- Production uses cron schedules from configuration
- Jobs are scoped and receive dependencies via constructor injection

### Stats Aggregation

StatsJob (BoincWatcherService/Jobs/StatsJob.cs) aggregates data from multiple BOINC hosts:
- Filters out hosts in "Down" state
- Maps host data to HostStats, HostProjectStats, and ProjectStats entities
- Stores in PostgreSQL via StatsService
- Optionally uploads to Azure Functions via FunctionAppService

## Important Notes

- The solution targets .NET 10.0
- EF Core migrations use PostgreSQL provider (Npgsql)
- The worker service uses Quartz for job scheduling with cron expressions
- BOINC RPC communication is handled by the BoincRpc NuGet package
- Azure Functions use isolated worker process model
- Stats are partitioned by date (YYYYMMDD format) for efficient querying
