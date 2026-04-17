# BattleSizeUnlocker - Copilot Instructions

## Project Overview

Bannerlord mod that expands the battle size range by reading a ModLib-backed setting and applying it to `BannerlordConfig.BattleSize` during key Bannerlord lifecycle events. This project is intended to closely mirror the behavior of the original Battle Size Unlocker mod after reverse-engineering its downloaded binary with ILSpy.

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

## Architecture

| File | Role |
|------|------|
| `Main.cs` | Bannerlord submodule entry point; caches settings and applies the configured battle size during lifecycle events |
| `ModSettings.cs` | ModLib-backed settings definition that exposes the in-game `Battle size` option |
| `BattleSizeRuntime.cs` | Small, testable decision layer for loading settings and deciding when to apply them |

### Key Design Decisions

- **Parity over invention:** The module shape, setting name, default, range, and lifecycle hooks intentionally follow the decompiled original DLL.
- **No Harmony patches:** The original mod does not need Harmony, so do not add it unless Bannerlord version drift makes it necessary and the user asks for that tradeoff.
- **Thin submodule, testable logic:** `Main` stays close to the original implementation while `BattleSizeRuntime` carries the logic that can be unit-tested without the live game runtime.
- **Cached settings model:** `Main` caches the resolved settings instance so later lifecycle callbacks reuse the same configured value, matching the original DLL's private static field approach.
- **ModLib is a runtime dependency:** The module depends on `ModLib` in `Module/SubModule.xml`; keep that dependency and document it when user-facing behavior changes.

## Code Conventions

- **Namespace:** `BattleSizeUnlocker`
- **XML documentation:** Keep `<summary>` docs on public and internal types and methods
- **Behavioral changes:** Before changing any lifecycle method or setting metadata, compare against the downloaded original mod assets
- **Testing style:** Prefer direct unit tests of `BattleSizeRuntime`, settings metadata reflection tests, and focused lifecycle tests on `Main` through test doubles rather than trying to stand up a full Bannerlord runtime
- **Module metadata:** Keep `Module/SubModule.xml` aligned with the entry point class and dependency list

## Module Metadata

`Module/SubModule.xml` currently defines:

- **Name / Id:** `BattleSizeUnlocker`
- **Dependencies:** `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `StoryMode`, `ModLib`
- **Entry point:** `BattleSizeUnlocker.Main`

Keep the `ModLib` dependency unless the settings approach is intentionally changed.

## Post-Change Workflow

After making code changes, follow this order:

1. **Build:** `dotnet build src\BattleSizeUnlocker\BattleSizeUnlocker.csproj -c Release`
2. **Write or update tests:** Cover changed logic and any changed settings metadata
3. **Test:** `dotnet test tests\BattleSizeUnlocker.Tests\BattleSizeUnlocker.Tests.csproj`
4. **Deploy:** `./deploy.ps1`

Do not deploy if build or tests are failing.