# SS-ProfileService (Vertical Slice Architecture in .NET 10)

This is the `ProfileService` microservice for the `samstore` ecosystem, built using **.NET 10 Minimal API** following the **Vertical Slice Architecture (VSA)** pattern.

## Tech Stack
- **Framework:** .NET 10 (C# 14 features)
- **Database:** PostgreSQL (with Npgsql EF Core provider)
- **CQRS:** MediatR
- **Validation:** FluentValidation
- **Testing:** xUnit & WebApplicationFactory

## Project Structure (Vertical Slice Architecture)
```
SS-ProfileService/
├── SS-ProfileService.slnx
├── src/
│   └── SS.ProfileService.API/
│       ├── Domain/
│       │   ├── Common/
│       │   │   └── BaseEntity.cs       # Base entity with audit trail properties
│       │   └── Entities/
│       │       └── Profile.cs          # Profile entity definition
│       ├── Features/
│       │   └── Profiles/
│       │       ├── CreateProfile/      # Self-contained slice for creating a profile
│       │       ├── GetProfileById/     # Self-contained slice for getting a profile
│       │       └── UpdateProfile/      # Self-contained slice for updating a profile
│       ├── Infrastructure/
│       │   └── Data/
│       │       └── ApplicationDbContext.cs # Entity Framework DbContext
│       ├── Program.cs                  # Main entry point with dependency injection and routing
│       └── appsettings.json            # Database connection configuration
└── test/
    └── SS.ProfileService.Tests/
        └── ProfileEndpointsTests.cs    # Integration tests using WebApplicationFactory
```

## Running the Project
1. Configure your PostgreSQL connection string in `src/SS.ProfileService.API/appsettings.json`.
2. Start the service:
   ```bash
   dotnet run --project src/SS.ProfileService.API
   ```

## Running Tests
To execute integration and unit tests:
```bash
dotnet test
```
