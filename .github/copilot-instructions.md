# BattleSizeUnlocker - Copilot Instructions

## Project Overview

Bannerlord mod that expands the battle size range by reading the original ModLib-style setting when available and applying it to `BannerlordConfig.BattleSize` during key Bannerlord lifecycle events. If the ModLib settings path is unavailable, the mod falls back to default settings so the module can still be enabled and used.

## Tech Stack

- **Language:** C# 9.0 targeting .NET Framework 4.7.2
- **Game SDK:** TaleWorlds Bannerlord assemblies (`TaleWorlds.Core`, `TaleWorlds.Library`, `TaleWorlds.MountAndBlade`)
- **Settings UI:** ModLib (`ModLib.Definitions.dll`)
- **Testing:** xUnit 2.6.6
- **Nullable:** Disabled project-wide

## Build, Test & Deploy Commands

```powershell
# Build
dotnet build src\BattleSizeUnlocker\BattleSizeUnlocker.csproj -c Release

# Run tests
dotnet test tests\BattleSizeUnlocker.Tests\BattleSizeUnlocker.Tests.csproj

# Deploy to game
./deploy.ps1
```

Both the main project and the test project depend on:

- `GameFolder` pointing at a valid local Bannerlord install
- `downloaded_mod/BattleSizeUnlocker/bin/Win64_Shipping_Client/ModLib.Definitions.dll` being present locally for build-time reference

## Reverse-Engineering Reference

These downloaded files are available locally and should be consulted before changing behavior that might affect parity with the original mod:

- `downloaded_mod/BattleSizeUnlocker/SubModule.xml`
- `downloaded_mod/BattleSizeUnlocker/bin/Win64_Shipping_Client/BattleSizeUnlocker.dll`
- `downloaded_mod/BattleSizeUnlocker/bin/Win64_Shipping_Client/ModLib.Definitions.dll`
- `downloaded_mod/BattleSizeUnlocker/bin/Win64_Shipping_Client/BattleSizeUnlocker.pdb`

ILSpy inspection of the original DLL confirmed:

- The entry point class is `BattleSizeUnlocker.Main`
- The settings class is `BattleSizeUnlocker.ModSettings : SettingsBase`
- `CustomBattleSize` has default `500` and range `2-2048`
- The original mod writes `BannerlordConfig.BattleSize` in:
  - `OnBeforeInitialModuleScreenSetAsRoot`
  - `OnMissionBehaviourInitialize` for field battles only
  - `OnGameStart`

Preserve that behavior unless the user explicitly asks for a functional change.

Current intentional divergence from the original mod:

- The project now exposes a `CustomBattleSize` maximum of `4000` instead of the original `2048`
- The project no longer declares `ModLib` as a hard launcher dependency; it falls back to default settings when ModLib-backed settings resolution is unavailable
- The project adapts the original direct `BannerlordConfig.BattleSize` assignment to current Bannerlord builds by rewriting the internal battle-size tables and using the highest valid battle-size option index

## Architecture

| File | Role |
|------|------|
| `Main.cs` | Bannerlord submodule entry point; caches settings and applies the configured battle size during lifecycle events |
| `ModSettings.cs` | Settings definition that preserves the original ModLib metadata surface for optional in-game configuration |
| `BattleSizeRuntime.cs` | Small, testable decision layer for loading settings, falling back to defaults, and deciding when to apply them |

### Key Design Decisions

- **Parity over invention:** The module shape, setting name, default, range, and lifecycle hooks intentionally follow the decompiled original DLL.
- **No Harmony patches:** The original mod does not need Harmony, so do not add it unless Bannerlord version drift makes it necessary and the user asks for that tradeoff.
- **Thin submodule, testable logic:** `Main` stays close to the original implementation while `BattleSizeRuntime` carries the logic that can be unit-tested without the live game runtime.
- **Cached settings model:** `Main` caches the resolved settings instance so later lifecycle callbacks reuse the same configured value. The original DLL stores that cache in a private static field; keep behavior aligned even if internal implementation details change for testability.
- **Optional settings integration:** The module still references `ModLib.Definitions.dll`, but `Module/SubModule.xml` no longer depends on the `ModLib` module. If settings resolution fails, the mod falls back to default settings instead of blocking launcher enablement.
- **Current build compatibility:** Bannerlord `v1.3.15` stores battle size as an index into internal battle-size tables. Do not assign raw troop counts directly to `BannerlordConfig.BattleSize`; rewrite the private tables and set a valid index instead.

## Code Conventions

- **Namespace:** `BattleSizeUnlocker`
- **XML documentation:** Keep `<summary>` docs on public and internal types and methods
- **Behavioral changes:** Before changing any lifecycle method or setting metadata, compare against the downloaded original mod assets
- **Testing style:** Prefer direct unit tests of `BattleSizeRuntime`, settings metadata reflection tests, and focused lifecycle tests on `Main` through test doubles rather than trying to stand up a full Bannerlord runtime
- **Module metadata:** Keep `Module/SubModule.xml` aligned with the entry point class and dependency list

## Module Metadata

`Module/SubModule.xml` currently defines:

- **Name / Id:** `BattleSizeUnlocker`
- **Dependencies:** `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `StoryMode`
- **Entry point:** `BattleSizeUnlocker.Main`

Do not reintroduce a hard `ModLib` launcher dependency unless the user explicitly asks for it.

## Post-Change Workflow

After making code changes, follow this order:

1. **Build:** `dotnet build src\BattleSizeUnlocker\BattleSizeUnlocker.csproj -c Release`
2. **Write or update tests:** Cover changed logic and any changed settings metadata
3. **Test:** `dotnet test tests\BattleSizeUnlocker.Tests\BattleSizeUnlocker.Tests.csproj`
4. **Deploy:** `./deploy.ps1`

Do not deploy if build or tests are failing.