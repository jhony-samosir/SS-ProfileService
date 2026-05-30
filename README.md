# SS-ProfileService

## Overview

SS-ProfileService is a microservice responsible for handling customer profile and address data for the SamStore e-commerce platform. Built with ASP.NET Core 10 using a Vertical Slice Architecture approach, the service is specialized for optimal, independent data handling around user demographics and shipping locations.

Operating as a downstream component, it communicates securely with the `SS-APIGateway` via internal origin signatures and synchronizes its local dataset by processing domain events (like user registration) fetched from RabbitMQ.

## Features

- **Profile Management**: Perform CRUD operations on customer profiles (name, bio, avatar upload, contact data).
- **Address Books**: Add, remove, and update shipping/billing locations linked directly to active profiles, including enforcing default addressing logic.
- **Vertical Slice Architecture**: Decouples dependencies by organizing business logic geographically by feature instead of traditional layers.
- **Idempotency with Inbox**: Prevents the duplicate processing of RabbitMQ messages using an Inbox table design.
- **Observability & Health Checks**: Exposes a `/health` probe and incorporates complete OpenTelemetry distributed tracing and Serilog.
- **Zero Trust Integration**: Implements a `GatewaySignatureMiddleware` to reject requests lacking the valid HMAC signature from the API Gateway.

## Tech Stack

| Category       | Technology                                     |
| -------------- | ---------------------------------------------- |
| Backend        | .NET 10.0 (C#)                                 |
| Architecture   | Vertical Slice Architecture (CQRS via MediatR) |
| Database       | PostgreSQL                                     |
| ORM            | Entity Framework Core (Npgsql)                 |
| Validation     | FluentValidation                               |
| Message Broker | RabbitMQ (.NET Client)                         |
| Telemetry      | OpenTelemetry, Serilog                         |

## Project Structure

```text
SS-ProfileService/
├── src/
│   └── SS.ProfileService.API/   # Application Core, Commands, Handlers, Endpoints
│       ├── Features/            # Vertical slice grouping (e.g. Profiles, Addresses)
│       ├── Middleware/          # Origin gateway security middleware
│       └── Program.cs           # Minimal API entry point
├── test/
│   └── SS.ProfileService.Tests/ # Unit and integration testing configurations
├── db/                          # Database connection or initial scripts
└── SS-ProfileService.slnx       # C# Project Solution manifest
```

## Requirements

- .NET 10.0 SDK
- PostgreSQL
- RabbitMQ

## Installation

```bash
git clone <repository>
cd SamStore/SS-ProfileService
```

Build the dependencies:

```bash
dotnet restore
```

## Configuration

Configuration parameters are mapped inside `appsettings.json` alongside system environment variables. Important fields include:

```env
ConnectionStrings__DefaultConnection= # PostgreSQL database connection format (e.g. Host=localhost;Port=5432;Database=ss_profile_db...)
RabbitMQ__Host=                       # RabbitMQ instance hostname
RabbitMQ__Port=                       # RabbitMQ connection port
RabbitMQ__Username=                   # RabbitMQ credentials
RabbitMQ__Password=                   # RabbitMQ credentials
GATEWAY_HMAC_SECRET=                  # Secret matched against the gateway origin signature middleware
ASPNETCORE_ENVIRONMENT=               # Development, Testing or Production
```

## Running Locally

Run locally (Uses configurations mapped in `launchSettings.json`):

```bash
dotnet run --project src/SS.ProfileService.API/SS.ProfileService.API.csproj
```

## Build

Compile using the .NET CLI:

```bash
dotnet build
```

## Testing

Run unit tests via xUnit framework (The tests simulate a real flow using the in-memory Entity Framework testing provider):

```bash
dotnet test
```

## API Documentation

Not identified from source code.

## Database

- **Database Type**: PostgreSQL.
- **ORM**: Entity Framework Core.
- **Migrations**: Executed as code-first entity schemas.
- **Data Properties**: Relies on a schema definition featuring `user_profiles`, `user_addresses`, `inbox_events` and `outbox_events`. All data utilizes UUID/GUID based mapping and enforces logical soft-deletion structures via audit trail timestamps.

## Deployment

- **Docker**: Packaged using the standard multi-stage .NET `Dockerfile`.
- **Docker Compose**: Preconfigured inside the gateway repository.

## Architecture Notes

- **Vertical Slice Architecture**: Enhances maintainability and feature isolation by grouping queries and commands with their respective endpoint handlers.

## Known Issues

Not identified from source code.

## Future Improvements

- Introduce a dedicated photo uploading service integration.

## License

```text
License information not specified.
```
