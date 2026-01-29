# PostgreSQL for Local Development - Implementation Plan

## Overview

**Feature**: [027-postgresql-local-development.md](../features/027-postgresql-local-development.md)
**Approach**: Configuration Migration with Validation Testing
**Total Phases**: 5

## Summary

This is an infrastructure migration, not a business logic feature, so we'll adapt the TDD approach to focus on configuration validation and smoke testing. Each phase will deliver a working vertical slice of the system (AppHost → API → Microservices) with PostgreSQL while maintaining SQL Server as commented fallback code. The implementation prioritizes incremental validation at each layer to catch compatibility issues early.

---

## Phase 1: Foundation - NuGet Packages & AppHost Configuration

**Goal**: Establish PostgreSQL container infrastructure with pgAdmin in Aspire AppHost

**Vertical Slice**: Aspire orchestration starts PostgreSQL and pgAdmin containers successfully

### Tasks

**Files to Modify**:
- [TripleDerby.AppHost\TripleDerby.AppHost.csproj](TripleDerby.AppHost\TripleDerby.AppHost.csproj) - Add PostgreSQL hosting package
- [TripleDerby.Infrastructure\TripleDerby.Infrastructure.csproj](TripleDerby.Infrastructure\TripleDerby.Infrastructure.csproj) - Add Npgsql EF Core package
- [TripleDerby.AppHost\Program.cs](TripleDerby.AppHost\Program.cs) - Replace SQL Server with PostgreSQL container

#### Step 1: Install NuGet Packages
- [ ] Add `Aspire.Hosting.PostgreSQL` package to AppHost.csproj (keep existing Aspire.Hosting.SqlServer)
- [ ] Add `Npgsql.EntityFrameworkCore.PostgreSQL` package to Infrastructure.csproj (keep existing SqlServer package)

#### Step 2: Configure PostgreSQL Container in AppHost
- [ ] Comment out SQL Server container configuration (lines 5-8 in AppHost/Program.cs)
- [ ] Add PostgreSQL container with persistent volume and pgAdmin (port 55432)
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections
- [ ] Update variable name from `sql` to `postgres` for new container

#### Step 3: Update Service References
- [ ] Update API service reference from `.WithReference(sql)` to `.WithReference(postgres)` (line 18)
- [ ] Comment out old SQL Server reference
- [ ] Update breeding service references (lines 28-29)
- [ ] Update feeding service references (lines 34-35)
- [ ] Update training service references (lines 40-41)
- [ ] Update racing service references (lines 46-47)
- [ ] Update all `.WaitFor(sql)` to `.WaitFor(postgres)`

### Validation

- [ ] Run AppHost: `dotnet run --project TripleDerby.AppHost`
- [ ] Verify PostgreSQL container starts in Aspire dashboard
- [ ] Verify pgAdmin container starts in Aspire dashboard
- [ ] Verify SQL Server container NOT started (commented out)
- [ ] Verify all service projects show database connection string configured
- [ ] Access pgAdmin UI from Aspire dashboard
- [ ] Verify pgAdmin can connect to PostgreSQL container

### Acceptance Criteria

- [ ] AppHost starts without errors
- [ ] Aspire dashboard shows PostgreSQL container running
- [ ] Aspire dashboard shows pgAdmin container running
- [ ] pgAdmin accessible and pre-configured with PostgreSQL connection
- [ ] No SQL Server containers running
- [ ] All service projects receive postgres connection string from Aspire

**Deliverable**: Working Aspire orchestration with PostgreSQL and pgAdmin containers

**Estimated Time**: 30-45 minutes

---

## Phase 2: API Service Configuration

**Goal**: Configure API service to connect to PostgreSQL with DbContext using Npgsql provider

**Vertical Slice**: API service starts, connects to PostgreSQL, and creates database schema with seed data

### Tasks

**Files to Modify**:
- [TripleDerby.Api\Program.cs](TripleDerby.Api\Program.cs) - Update Aspire client integration
- [TripleDerby.Api\Config\DatabaseConfig.cs](TripleDerby.Api\Config\DatabaseConfig.cs) - Replace UseSqlServer with UseNpgsql

#### Step 1: Update Aspire Client Integration (Program.cs)
- [ ] Comment out `builder.AddSqlServerClient(connectionName: "sql");` (line 51)
- [ ] Add `builder.AddNpgsqlDataSource(connectionName: "postgres");`
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections

#### Step 2: Update DbContext Configuration (DatabaseConfig.cs)
- [ ] Comment out `options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure"));` (line 15)
- [ ] Add `options.UseNpgsql(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure"));`
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections
- [ ] Ensure connection string variable name remains `conn` (provider-agnostic)

### Validation

- [ ] Build solution: `dotnet build`
- [ ] Run AppHost with API service
- [ ] Monitor API service logs for database connection
- [ ] Verify `EnsureCreated()` executes successfully (line 108 in API Program.cs)
- [ ] Check pgAdmin to verify database "TripleDerby" created
- [ ] Verify all tables created (Colors, Conditions, Horses, Races, etc.)
- [ ] Verify seed data loaded (check Colors table for "Bay", "Chestnut", etc.)
- [ ] Access API health endpoint if available

### Acceptance Criteria

- [ ] API service starts without errors
- [ ] Database "TripleDerby" created in PostgreSQL
- [ ] All 22+ tables created successfully
- [ ] Seed data loaded (Colors, Conditions, Surfaces, LegTypes, etc.)
- [ ] API service logs show successful database connection
- [ ] No SQL Server connection attempts in logs
- [ ] pgAdmin shows populated database schema

**Deliverable**: API service fully operational with PostgreSQL backend

**Estimated Time**: 30-45 minutes

---

## Phase 3: Microservices Configuration (All 4 Services)

**Goal**: Configure all 4 microservices to connect to PostgreSQL using consistent pattern

**Vertical Slice**: All microservices start and connect to PostgreSQL database successfully

### Tasks

**Files to Modify** (same pattern for each service):
- [TripleDerby.Services.Breeding\Program.cs](TripleDerby.Services.Breeding\Program.cs)
- [TripleDerby.Services.Racing\Program.cs](TripleDerby.Services.Racing\Program.cs)
- [TripleDerby.Services.Training\Program.cs](TripleDerby.Services.Training\Program.cs)
- [TripleDerby.Services.Feeding\Program.cs](TripleDerby.Services.Feeding\Program.cs)

#### Step 1: Update Breeding Service
- [ ] Comment out `UseSqlServer()` in DbContext configuration (line 34)
- [ ] Add `UseNpgsql()` with same connection string and migrations assembly
- [ ] Comment out `builder.AddSqlServerClient(connectionName: "sql");` (line 54)
- [ ] Add `builder.AddNpgsqlDataSource(connectionName: "postgres");`
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections

#### Step 2: Update Racing Service
- [ ] Comment out `UseSqlServer()` in DbContext configuration (line 34)
- [ ] Add `UseNpgsql()` with same connection string and migrations assembly
- [ ] Comment out `builder.AddSqlServerClient(connectionName: "sql");` (line 63)
- [ ] Add `builder.AddNpgsqlDataSource(connectionName: "postgres");`
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections

#### Step 3: Update Training Service
- [ ] Comment out `UseSqlServer()` in DbContext configuration (line 32)
- [ ] Add `UseNpgsql()` with same connection string and migrations assembly
- [ ] Comment out `builder.AddSqlServerClient(connectionName: "sql");` (line 51)
- [ ] Add `builder.AddNpgsqlDataSource(connectionName: "postgres");`
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections

#### Step 4: Update Feeding Service
- [ ] Comment out `UseSqlServer()` in DbContext configuration (line 32)
- [ ] Add `UseNpgsql()` with same connection string and migrations assembly
- [ ] Comment out `builder.AddSqlServerClient(connectionName: "sql");` (line 51)
- [ ] Add `builder.AddNpgsqlDataSource(connectionName: "postgres");`
- [ ] Add clear inline comments marking SQL Server vs PostgreSQL sections

### Validation

- [ ] Build solution: `dotnet build`
- [ ] Run AppHost with all services
- [ ] Verify Breeding service starts and connects to PostgreSQL
- [ ] Verify Racing service starts and connects to PostgreSQL
- [ ] Verify Training service starts and connects to PostgreSQL
- [ ] Verify Feeding service starts and connects to PostgreSQL
- [ ] Monitor all service logs for successful database connections
- [ ] Verify no SQL Server connection attempts in any service logs
- [ ] Check Aspire dashboard shows all services healthy

### Acceptance Criteria

- [ ] All 4 microservices start without errors
- [ ] All services connect to PostgreSQL successfully
- [ ] All services log "Starting [ServiceName] worker host" successfully
- [ ] No SQL Server connection attempts in logs
- [ ] Aspire dashboard shows all services healthy (green status)
- [ ] No database connection errors in any service

**Deliverable**: Complete application stack running on PostgreSQL

**Estimated Time**: 45-60 minutes

---

## Phase 4: Schema & Data Validation Testing

**Goal**: Validate database schema compatibility and seed data integrity on PostgreSQL

**Vertical Slice**: End-to-end operations work (create horse, run race, feeding, training, breeding)

### Tasks

#### Step 1: Schema Inspection
- [ ] Use pgAdmin to inspect database schema
- [ ] Verify all 22+ tables exist with correct structure
- [ ] Check identity columns (should be SERIAL or IDENTITY in PostgreSQL)
- [ ] Verify foreign key constraints created
- [ ] Check indexes created properly
- [ ] Validate data types mapped correctly (text, timestamp, etc.)

#### Step 2: Seed Data Verification
- [ ] Query Colors table - verify 10+ colors present
- [ ] Query Conditions table - verify conditions (Fast, Good, Yielding, etc.)
- [ ] Query Surfaces table - verify surfaces (Dirt, Turf, Synthetic)
- [ ] Query LegTypes table - verify leg types (Sprint, Mid Distance, Long Distance)
- [ ] Query RaceClasses table - verify race classes (Maiden, Allowance, Stakes, etc.)
- [ ] Verify all seed data loaded without truncation or corruption

#### Step 3: Basic CRUD Operations (Manual Testing)
- [ ] Create a new Horse record via API
- [ ] Query Horses to verify persistence
- [ ] Create a Race record
- [ ] Create a RaceRun with horses
- [ ] Trigger a race execution (via RaceRequested message)
- [ ] Verify race results persisted correctly
- [ ] Test breeding operation (BreedingRequested message)
- [ ] Test training operation (TrainingRequested message)
- [ ] Test feeding operation (FeedingRequested message)

#### Step 4: Compatibility Analysis
- [ ] Review application logs for any warnings about data types
- [ ] Check for case-sensitivity issues in queries (PostgreSQL is case-sensitive)
- [ ] Verify GUID handling works correctly
- [ ] Verify DateTime handling works correctly
- [ ] Document any compatibility issues found

### Validation

- [ ] All CRUD operations complete successfully
- [ ] No data corruption or truncation
- [ ] No schema mismatch errors
- [ ] Race execution completes and generates commentary
- [ ] Microservices process messages successfully
- [ ] Compare schema between SQL Server (old) and PostgreSQL (new) for equivalence

### Acceptance Criteria

- [ ] Database schema structurally equivalent to SQL Server version
- [ ] All seed data loaded correctly
- [ ] Basic CRUD operations work on all major entities
- [ ] Race execution completes successfully
- [ ] All 4 microservices process messages successfully
- [ ] No data type compatibility issues
- [ ] No case-sensitivity issues discovered
- [ ] Identity/sequence generation works correctly

**Deliverable**: Validated PostgreSQL database with working application operations

**Estimated Time**: 60-90 minutes

---

## Phase 5: Documentation & Switching Validation

**Goal**: Create comprehensive switching guide and validate bidirectional provider switching

**Vertical Slice**: Developers can switch between PostgreSQL and SQL Server using documentation

### Tasks

**New File**:
- `docs/DATABASE_SWITCHING.md` - Complete switching guide

#### Step 1: Create Switching Documentation
- [ ] Write overview section explaining dual-provider setup
- [ ] Create quick reference table (files to modify with line numbers)
- [ ] Document switching from PostgreSQL → SQL Server (8 files)
- [ ] Document switching from SQL Server → PostgreSQL (8 files)
- [ ] Provide connection string format examples for both providers
- [ ] Add troubleshooting section (common errors and fixes)
- [ ] Add performance testing guidance section
- [ ] Document pgAdmin vs SQL Server Management Studio usage

#### Step 2: Test PostgreSQL → SQL Server Switch
- [ ] Uncomment SQL Server code in AppHost Program.cs (lines 5-8)
- [ ] Comment out PostgreSQL code in AppHost Program.cs
- [ ] Update service references from `postgres` back to `sql`
- [ ] Uncomment SQL Server code in API (Program.cs + DatabaseConfig.cs)
- [ ] Comment out PostgreSQL code in API
- [ ] Repeat for all 4 microservices
- [ ] Build and run application
- [ ] Verify SQL Server container starts
- [ ] Verify application connects to SQL Server
- [ ] Verify database created and seeded

#### Step 3: Test SQL Server → PostgreSQL Switch
- [ ] Comment out SQL Server code in all 6 locations (AppHost, API, 4 microservices)
- [ ] Uncomment PostgreSQL code in all 6 locations
- [ ] Update service references from `sql` to `postgres`
- [ ] Build and run application
- [ ] Verify PostgreSQL container starts
- [ ] Verify application connects to PostgreSQL
- [ ] Verify database created and seeded

#### Step 4: Final Validation
- [ ] Ensure PostgreSQL is the active provider (uncommented)
- [ ] Ensure SQL Server is commented out in all files
- [ ] Verify consistent commenting pattern across all files
- [ ] Verify documentation matches actual file locations and line numbers
- [ ] Review all inline code comments for clarity

### Validation

- [ ] Documentation exists and is comprehensive
- [ ] Successfully switched to SQL Server and back
- [ ] Both providers work equivalently
- [ ] Switching process takes < 10 minutes
- [ ] No schema differences between providers
- [ ] Documentation accurate (file paths and line numbers correct)

### Acceptance Criteria

- [ ] DATABASE_SWITCHING.md created in `/docs/`
- [ ] Documentation includes file-by-file instructions
- [ ] Documentation includes connection string examples
- [ ] Documentation includes troubleshooting section
- [ ] Successfully switched to SQL Server (validated working)
- [ ] Successfully switched back to PostgreSQL (validated working)
- [ ] Both providers use identical seed data
- [ ] PostgreSQL is active provider at phase completion
- [ ] All inline code comments are clear and consistent
- [ ] Code quality meets project standards

**Deliverable**: Complete dual-provider setup with switching documentation

**Estimated Time**: 60-75 minutes

---

## Files Summary

### New Files
- `docs/DATABASE_SWITCHING.md` - Comprehensive switching guide

### Modified Files
- `TripleDerby.AppHost\TripleDerby.AppHost.csproj` - Add PostgreSQL package
- `TripleDerby.AppHost\Program.cs` - Replace SQL Server with PostgreSQL container (lines 5-50)
- `TripleDerby.Infrastructure\TripleDerby.Infrastructure.csproj` - Add Npgsql package
- `TripleDerby.Api\Program.cs` - Replace AddSqlServerClient with AddNpgsqlDataSource (line 51)
- `TripleDerby.Api\Config\DatabaseConfig.cs` - Replace UseSqlServer with UseNpgsql (line 15)
- `TripleDerby.Services.Breeding\Program.cs` - Update DbContext and Aspire client (lines 34, 54)
- `TripleDerby.Services.Racing\Program.cs` - Update DbContext and Aspire client (lines 34, 63)
- `TripleDerby.Services.Training\Program.cs` - Update DbContext and Aspire client (lines 32, 51)
- `TripleDerby.Services.Feeding\Program.cs` - Update DbContext and Aspire client (lines 32, 51)

**Total**: 9 files modified, 1 file created

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Infrastructure Ready | Phase 1 | PostgreSQL and pgAdmin containers running in Aspire |
| API Online | Phase 2 | API service connects to PostgreSQL with schema + seed data |
| Full Stack | Phase 3 | All 6 services running on PostgreSQL |
| Validated | Phase 4 | End-to-end operations tested and working |
| Feature Complete | Phase 5 | Dual-provider setup with switching documentation |

---

## Risks & Mitigation

| Risk | Mitigation | Phase |
|------|------------|-------|
| Aspire PostgreSQL package version conflicts | Use latest stable Aspire.Hosting.PostgreSQL; override conflicting deps | 1 |
| Schema incompatibilities (identity columns) | Validate in Phase 2; EF Core should handle automatically | 2 |
| Seed data fails on PostgreSQL | Test thoroughly in Phase 2; seed data should be provider-agnostic | 2 |
| Case-sensitivity breaks existing queries | Identify in Phase 4; document in DATABASE_SWITCHING.md | 4 |
| Connection string format differences | Test both providers in Phase 5; Aspire should handle this | 5 |
| Developer confusion switching providers | Clear documentation with exact line numbers in Phase 5 | 5 |

---

## Code Pattern Examples

### Commenting Pattern (Used Consistently Across All Files)

```csharp
// ============================================================================
// DATABASE PROVIDER CONFIGURATION
// See docs/DATABASE_SWITCHING.md for switching between SQL Server and PostgreSQL
// ============================================================================

// SQL SERVER (Commented for local dev)
// var sql = builder.AddSqlServer("sql", port: 59944)
//     .WithDataVolume()
//     .WithLifetime(ContainerLifetime.Persistent)
//     .AddDatabase("TripleDerby");

// POSTGRESQL (Active for local dev)
var postgres = builder.AddPostgres("postgres", port: 55432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin()
    .AddDatabase("TripleDerby");
```

### DbContext Configuration Pattern

```csharp
// SQL SERVER
// builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
//     options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

// POSTGRESQL
builder.Services.AddDbContextPool<TripleDerbyContext>(options =>
    options.UseNpgsql(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));
```

### Aspire Client Integration Pattern

```csharp
// SQL SERVER
// builder.AddSqlServerClient(connectionName: "sql");

// POSTGRESQL
builder.AddNpgsqlDataSource(connectionName: "postgres");
```

---

## Success Criteria (Overall)

### Functional
- [ ] Application starts successfully with PostgreSQL
- [ ] All 4 microservices connect to PostgreSQL
- [ ] Database schema created via `EnsureCreated()`
- [ ] All seed data loaded correctly
- [ ] pgAdmin accessible and functional
- [ ] Basic CRUD operations work
- [ ] Race execution works end-to-end
- [ ] Can switch to SQL Server and back

### Code Quality
- [ ] SQL Server code present and clearly commented
- [ ] Consistent commenting pattern across all files
- [ ] Both NuGet packages present (SqlServer + Npgsql)
- [ ] No hardcoded connection strings
- [ ] Clear inline comments marking provider sections

### Documentation
- [ ] DATABASE_SWITCHING.md exists
- [ ] Step-by-step instructions for both directions
- [ ] File list with line numbers
- [ ] Connection string examples
- [ ] Troubleshooting section

---

## Phase Progression Protocol

**CRITICAL**: Stop after each phase for user review and approval.

After completing each phase:
1. ✅ Run validation steps
2. 📊 Report results to user (success/failures)
3. 🛑 **STOP and WAIT** for user review
4. ❓ Ask: "Would you like me to commit these changes?"
5. ⏸️ Wait for explicit approval
6. 📝 Commit if approved
7. ➡️ Wait for approval to start next phase

**DO NOT proceed to the next phase without explicit user approval.**

---

## Next Steps

Ready to begin Phase 1: Foundation - NuGet Packages & AppHost Configuration

Shall I populate the TodoWrite with Phase 1 tasks and begin implementation?
