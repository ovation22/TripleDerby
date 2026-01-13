# TripleDerby - Claude Code Instructions

## Project Overview

TripleDerby is a horse racing simulation game built with C# and .NET. Players own, breed, train, and race horses with realistic simulation mechanics.

## Architecture

```
TripleDerby.sln
├── TripleDerby.Core/           # Domain entities, services, abstractions
├── TripleDerby.Infrastructure/ # EF Core, repositories, external services
├── TripleDerby.Api/            # REST API controllers
├── TripleDerby.AppHost/        # .NET Aspire orchestration
├── TripleDerby.ServiceDefaults/# Shared service configuration
├── TripleDerby.Services.*/     # Microservices (Breeding, Racing, Training)
└── TripleDerby.Tests.Unit/     # Unit tests (xUnit)
```

## Key Locations

| Purpose | Location |
|---------|----------|
| Feature specs | `/docs/features/` |
| Implementation plans | `/docs/implementation/` |
| Unit tests | `TripleDerby.Tests.Unit/` |
| Race balance config | `TripleDerby.Core/Configuration/` |

## Key Patterns

- **Repository Pattern**: Generic `IEFRepository<T>` with Ardalis.Specification
- **Specification Pattern**: `FilterSpecification<T>` for composable queries
- **Service Layer**: Business logic in `I*Service` interfaces
- **Microservices**: Azure Service Bus for async processing (Racing, Breeding)

## Testing Conventions

- **Framework**: xUnit with Moq
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- **Pattern**: Arrange-Act-Assert
- **Builders**: Use test builders for complex objects (e.g., `CreateHorse()`, `CreateRaceRun()`)

## Common Commands

```bash
dotnet build                    # Build solution
dotnet test                     # Run all tests
dotnet ef migrations add <Name> # Add EF migration (from Infrastructure project)
```

## Git Workflow

**NEVER commit or push without explicit user approval.**

1. Implement feature / fix
2. Run tests to verify
3. Report results to user
4. **ASK**: "Would you like me to commit these changes?"
5. **WAIT** for explicit approval
6. Only then run git commands

## Commit Messages

Write commit messages as if written by the developer:
- Focus on what changed and why
- Use professional, technical language
- No AI attribution or co-author tags

## Code Style

- Follow existing patterns in the codebase
- Use async/await with CancellationToken
- Prefer specifications over raw expressions for queries
- Keep services focused (single responsibility)

## Documentation

- **Diagrams**: Always use Mermaid (renders in GitHub, version control friendly)
  - Architecture: `graph TB` or `graph LR`
  - Sequences: `sequenceDiagram`
  - State machines: `stateDiagram-v2`
  - Data models: `erDiagram`

## Game Balance

Race simulation involves multiple modifier systems. Key config files:
- `RaceModifierConfig.cs` - Speed, stamina, environment modifiers
- `CommentaryConfig.cs` - Play-by-play thresholds

When modifying race mechanics, consider balance implications and run validation tests.
