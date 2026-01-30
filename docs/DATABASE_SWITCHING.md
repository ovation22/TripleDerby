# Database Provider Switching Guide

This guide provides step-by-step instructions for switching between PostgreSQL and SQL Server in local development.

## Current Configuration

**Active**: PostgreSQL
**Port**: 55432
**Commented**: SQL Server (port 59944)

## Quick Reference Table

| File | PostgreSQL Lines | SQL Server Lines |
|------|-----------------|------------------|
| TripleDerby.AppHost\Program.cs | 17-21 | 10-14 |
| TripleDerby.Api\Config\DatabaseConfig.cs | 20-21 | 15-17 |
| TripleDerby.Api\Program.cs | 117-119 | 114 |
| TripleDerby.Services.Breeding\Program.cs | 38-39, 58-60 | 34-35, 55 |
| TripleDerby.Services.Racing\Program.cs | 38-39, 72-74 | 34-35, 69 |
| TripleDerby.Services.Training\Program.cs | 36-37, 60-62 | 32-33, 57 |
| TripleDerby.Services.Feeding\Program.cs | 36-37, 60-62 | 32-33, 57 |

## Switching from PostgreSQL to SQL Server

### Step 1: Update AppHost Configuration

**File**: `TripleDerby.AppHost\Program.cs`

Comment out PostgreSQL (lines 17-21):
```csharp
// POSTGRESQL (Commented)
// var postgres = builder.AddPostgres("sql", port: 55432)
//     .WithDataVolume()
//     .WithLifetime(ContainerLifetime.Persistent)
//     .WithPgAdmin();
// var sql = postgres.AddDatabase("TripleDerby");
```

Uncomment SQL Server (lines 10-14):
```csharp
// SQL SERVER (Active)
var sql = builder.AddSqlServer("sql", port: 59944)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("TripleDerby");
```

### Step 2: Update API Service

**File**: `TripleDerby.Api\Config\DatabaseConfig.cs`

Comment out PostgreSQL (lines 20-21):
```csharp
// POSTGRESQL (Commented)
// services.AddDbContextPool<TripleDerbyContext>(options =>
//     options.UseNpgsql(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));
```

Uncomment SQL Server (lines 15-17):
```csharp
// SQL SERVER (Active)
services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));
```

**File**: `TripleDerby.Api\Program.cs`

Comment out PostgreSQL comment block (lines 117-119):
```csharp
// POSTGRESQL (Commented)
// Connection string automatically provided by Aspire via .WithReference(sql)
// Manual DbContext configuration in DatabaseConfig.cs
```

Uncomment SQL Server (line 114):
```csharp
// SQL SERVER (Active)
builder.AddSqlServerClient(connectionName: "sql");
```

### Step 3: Update All Microservices

Repeat for each service:
- `TripleDerby.Services.Breeding\Program.cs`
- `TripleDerby.Services.Racing\Program.cs`
- `TripleDerby.Services.Training\Program.cs`
- `TripleDerby.Services.Feeding\Program.cs`

**DbContext Configuration Section** (around line 30-40):

Comment out PostgreSQL:
```csharp
// POSTGRESQL (Commented)
// builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
//     options.UseNpgsql(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));
```

Uncomment SQL Server:
```csharp
// SQL SERVER (Active)
builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));
```

**Aspire Client Section** (around line 55-75):

Comment out PostgreSQL block:
```csharp
// POSTGRESQL (Commented)
// Connection string automatically provided by Aspire via .WithReference(sql)
// Manual DbContext configuration above
```

Uncomment SQL Server:
```csharp
// SQL SERVER (Active)
builder.AddSqlServerClient(connectionName: "sql");
```

### Step 4: Restart Application

1. Stop any running AppHost instance
2. Clean and rebuild solution: `dotnet clean && dotnet build`
3. Start AppHost
4. Verify SQL Server container starts in Aspire dashboard
5. Verify all services connect successfully

## Switching from SQL Server to PostgreSQL

Follow the reverse process:

1. **AppHost**: Comment SQL Server (lines 10-14), uncomment PostgreSQL (lines 17-21)
2. **API DatabaseConfig**: Comment SQL Server (lines 15-17), uncomment PostgreSQL (lines 20-21)
3. **API Program**: Comment SQL Server (line 114), uncomment PostgreSQL block (lines 117-119)
4. **All Microservices**: Comment SQL Server blocks, uncomment PostgreSQL blocks
5. Restart application

## Connection String Details

Both providers use the same connection string pattern:

- **Key**: `ConnectionStrings:TripleDerby` (database resource name)
- **Injected by**: Aspire via `.WithReference(sql)`
- **Retrieved via**: `builder.Configuration.GetConnectionString("TripleDerby")`

### PostgreSQL Connection String (Auto-generated)
```
Host=localhost;Port=55432;Database=TripleDerby;Username=postgres;Password=[auto]
```

### SQL Server Connection String (Auto-generated)
```
Server=localhost,59944;Database=TripleDerby;User Id=sa;Password=[auto];TrustServerCertificate=True
```

## Important Notes

### No Package Changes Required

Both provider packages remain in place:
- `Npgsql.EntityFrameworkCore.PostgreSQL` in Infrastructure.csproj
- `Microsoft.EntityFrameworkCore.SqlServer` in Infrastructure.csproj
- `Aspire.Hosting.PostgreSQL` in AppHost.csproj
- `Aspire.Hosting.SqlServer` in AppHost.csproj

**Do NOT add** `Aspire.Npgsql` packages to service projects - they conflict with manual configuration.

### Schema Compatibility

Both providers work with the same:
- Entity configurations (no provider-specific code)
- Seed data from `ModelBuilderExtensions`
- `EnsureCreated()` approach (no migrations)

### Data Persistence

Each provider maintains separate persistent Docker volumes:
- PostgreSQL: Data persists between switches to PostgreSQL
- SQL Server: Data persists between switches to SQL Server
- Switching providers does NOT copy data between databases

## Troubleshooting

### "ConnectionString is missing" Error

**Cause**: Aspire.Npgsql package interfering with manual configuration

**Solution**:
1. Verify Aspire.Npgsql is NOT in any service .csproj files
2. Check that `GetConnectionString("TripleDerby")` matches database resource name
3. Ensure manual DbContext configuration is present (not using AddNpgsqlDataSource)

### Services Won't Start

**Cause**: Database container not ready

**Solution**:
1. Check Aspire dashboard for container status
2. Verify `.WaitFor(sql)` dependencies in AppHost Program.cs
3. Check container logs in Aspire dashboard

### Schema Differences

**Cause**: Provider-specific SQL generated by EF Core

**Solution**:
1. Delete Docker volumes to reset: `docker volume prune`
2. Restart AppHost to recreate schema
3. Report any seed data incompatibilities

### Performance Issues

**Cause**: Docker resources or provider-specific behavior

**Solution**:
1. Increase Docker memory/CPU allocation
2. Check Docker Desktop resource settings
3. Review query execution plans in pgAdmin or SSMS

## Additional Tools

### PostgreSQL Management
- **pgAdmin**: Available through Aspire dashboard (WithPgAdmin)
- **psql**: Command-line tool (install separately)

### SQL Server Management
- **SQL Server Management Studio (SSMS)**: Windows only
- **Azure Data Studio**: Cross-platform
- **sqlcmd**: Command-line tool

## Related Documentation

- [Feature 027 Specification](features/027-postgresql-local-development.md)
- [.NET Aspire PostgreSQL](https://learn.microsoft.com/en-us/dotnet/aspire/database/postgresql-component)
- [.NET Aspire SQL Server](https://learn.microsoft.com/en-us/dotnet/aspire/database/sql-server-component)
