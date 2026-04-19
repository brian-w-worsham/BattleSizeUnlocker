# BattleSizeUnlocker - Copilot Instructions

## Project Overview

Bannerlord mod that expands the battle size range by loading the selected value from a local XML settings file, with the original ModLib-style setting path retained only as a fallback compatibility source. On current Bannerlord builds, the mod rewrites the internal battle-size tables, selects a valid `BannerlordConfig.BattleSize` option index, uses Harmony to override the early `BannerlordConfig.GetRealBattleSize*` reads that mission spawn logic captures before the first deployment wave, and lifts Bannerlord's conservative half-agent mission clamp for all mission types — siege and sally-out openings use the full agent ceiling, field battles use two-thirds of the ceiling to reserve agent slots for cavalry mounts. The user-facing configuration path is a built-in hotkey screen opened with `Ctrl + Shift + F8` on the campaign map.

## Tech Stack

- **Language:** C# 9.0 targeting .NET Framework 4.7.2
- **Game SDK:** TaleWorlds Bannerlord assemblies (`TaleWorlds.Core`, `TaleWorlds.Library`, `TaleWorlds.MountAndBlade`)
- **Runtime patching:** Harmony (`Lib.Harmony` / `0Harmony.dll`)
- **Settings UI:** Built-in inquiry UI with `ModLib.Definitions.dll` retained only for fallback compatibility metadata
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

- The project now exposes a `CustomBattleSize` maximum of `2040`, matching the native mission-agent ceiling reported by the supported Bannerlord build instead of the original `2048`
- The project no longer declares `ModLib` as a hard launcher dependency; it falls back to default settings when ModLib-backed settings resolution is unavailable
- The project adapts the original direct `BannerlordConfig.BattleSize` assignment to current Bannerlord builds by rewriting the internal battle-size tables and using the highest valid battle-size option index
- The rewritten field, siege, and sally-out tables now all use the configured value as their top-end entry
- The active battle size is now primarily stored in `BattleSizeUnlocker.settings.xml`, edited through a built-in `Ctrl + Shift + F8` hotkey screen on the campaign map
- The mod now also reapplies the configured battle size during application ticks because current Bannerlord builds can reset the runtime tables before siege spawn logic captures them
- The mod now also patches `BannerlordConfig.GetRealBattleSize`, `GetRealBattleSizeForSiege`, and `GetRealBattleSizeForSallyOut` so current Bannerlord builds capture the configured troop count before `MissionAgentSpawnLogic` freezes the opening deployment size
- The mod now also patches the `MissionAgentSpawnLogic` constructor so Bannerlord no longer cuts the opening troop cap to half of the native mission-agent ceiling — siege and sally-out missions use the full ceiling, field battles use two-thirds to account for cavalry mounts

## Architecture

| File | Role |
|------|------|
| `Main.cs` | Bannerlord submodule entry point; caches settings and applies the configured battle size during lifecycle events |
| `BattleSizeConfig.cs` | Resolves the active settings from the local XML file first, then falls back to ModLib or defaults |
| `BattleSizeSettingsStore.cs` | Reads and writes `BattleSizeUnlocker.settings.xml` in the deployed module folder |
| `BattleSizeHotkeyController.cs` | Detects the `Ctrl + Shift + F8` hotkey on the campaign map and opens the built-in settings selector |
| `ModSettings.cs` | Settings definition that preserves the original ModLib metadata surface for optional in-game configuration |
| `BattleSizeRuntime.cs` | Small, testable decision layer for loading settings, falling back to defaults, and deciding when to apply them |
| `Patches/BattleSizeGetterPatches.cs` | Harmony prefixes that force early battle-size reads to use the configured value before mission spawn logic captures them |
| `Patches/MissionAgentSpawnLogicPatches.cs` | Harmony constructor postfix that lifts the opening troop cap from the conservative half-agent clamp — full ceiling for siege/sally-out, two-thirds for field battles (mount reserve) |

### Key Design Decisions

- **Parity over invention:** The module shape, setting name, default, range, and lifecycle hooks intentionally follow the decompiled original DLL.
- **Measured Harmony use:** The original mod did not need Harmony, but current Bannerlord builds capture siege deployment size too early for the original lifecycle hooks. Keep Harmony usage limited to the `GetRealBattleSize*` compatibility patch unless the user asks for a broader change.
- **Native ceiling awareness:** Mission openings are still bounded by Bannerlord's native agent ceiling. The managed patch removes the conservative `/ 2` troop clamp for siege-style opening deployments, but it should not pretend to exceed the engine's real agent limit.
- **Thin submodule, testable logic:** `Main` stays close to the original implementation while `BattleSizeRuntime` carries the logic that can be unit-tested without the live game runtime.
- **Cached settings model:** `Main` caches the resolved settings instance so later lifecycle callbacks reuse the same configured value. The original DLL stores that cache in a private static field; keep behavior aligned even if internal implementation details change for testability.
- **Local settings first:** `BattleSizeUnlocker.settings.xml` is the primary source of truth. ModLib is only a compatibility fallback if the local file does not exist yet.
- **Hotkey-driven configuration:** The supported user flow is `Ctrl + Shift + F8` on the campaign map, which opens a built-in multi-selection inquiry and saves the chosen value immediately.
- **Current build compatibility:** Bannerlord `v1.3.15` stores battle size as an index into internal battle-size tables. Do not assign raw troop counts directly to `BannerlordConfig.BattleSize`; rewrite the private tables and set a valid index instead.
- **Requested maximum semantics:** Treat the configured `CustomBattleSize` as the top-end troop count for field, siege, and sally-out tables, not just field battles.
- **Early getter override plus constructor override:** Mission spawn logic captures battle size before `OnMissionBehaviorInitialize`, and current builds also clamp opening deployments to half of the native agent ceiling. Keep both the Harmony getter patch and the constructor override in place. Siege and sally-out missions use the full agent ceiling (no mounts); field battles use `FieldBattleMountReserveFraction` (2/3) of the ceiling to stay crash-safe when cavalry mounts consume extra agent slots.

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