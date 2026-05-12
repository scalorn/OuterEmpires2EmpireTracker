# OE2EmpireTracker.Common — Spec

Shared library containing domain models, constants, services, and interfaces used by both the WinForms Tool and the Remote Faction Server.

## Target Framework
.NET Standard 2.0 — compatible with .NET Framework 4.8.1 (Tool) and .NET 8.0 (Server).

## Contents

| Directory | Purpose |
|-----------|---------|
| Models/ | All domain POCOs, enums, ReadOnly wrappers, request/response DTOs |
| Constants/ | Game constants, blueprint types, refining recipes, research times |
| Services/ | Pure business logic (reference counters, calculators, serialization) |
| Interfaces/ | IColonyProcessingContext, IGameConfig |

## Design Principle
Common contains ONLY code with zero dependencies on:
- System.Windows.Forms (WinForms-specific)
- System.Drawing (UI-specific)
- PlayerContext / EmpireContext singletons (Tool-specific)

Models that previously referenced singletons were refactored to use interfaces (IGameConfig, IColonyProcessingContext) that are implemented by the consuming projects.

## Relationship to Other Projects
- **Tool** references Common for domain models and business logic
- **Server** references Common for colony processing (Colony.ProcessColony) and shared model serialization
- **Tests** reference Common for model/service unit tests
