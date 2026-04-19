# Battle Size Unlocker — Bannerlord Mod

Expands Bannerlord's battle size range and applies your chosen value to campaign, sandbox, and custom battles.

This project is a clean-room reimplementation of the original Battle Size Unlocker mod after inspecting the downloaded `BattleSizeUnlocker.dll` with ILSpy. The implementation intentionally mirrors the original behavior closely, with three explicit project changes: the maximum battle size is capped at `2040` to match the current engine ceiling observed on the supported Bannerlord build, the hard launcher dependency on `ModLib` has been removed so the module can be enabled even when `ModLib` is not installed, and a built-in hotkey settings screen now saves the selected battle size without requiring ModLib.

- Uses a local XML settings file for the active battle size and still preserves the original ModLib-backed metadata as a fallback path
- Expands the setting range to **2-2040**
- Defaults the configured battle size to **500**
- Applies the configured size during module-root initialization, game start, mission startup, and runtime ticks so Bannerlord keeps the chosen cap active

## Features

- **Expanded battle size range:** Goes beyond the base game's 200-1000 range
- **Launcher-safe by default:** No `ModLib` module dependency is required just to enable the mod
- **Built-in hotkey settings screen:** Press **Ctrl + Shift + F8** on the campaign map to choose and save a battle size without ModLib
- **Optional Mod Options compatibility:** If `ModLib` is installed, the original-style setting metadata still exists as a fallback path
- **Campaign, Sandbox, and Custom Battle support:** Matches the original mod's intended scope
- **Pre-spawn compatibility fix:** Uses a small Harmony patch so current Bannerlord builds read the configured battle size before mission spawn logic captures the initial deployment cap
- **Opening troop cap fix:** Lifts Bannerlord's conservative half-agent mission clamp. Siege and sally-out openings use the engine's full agent ceiling; field battles use two-thirds of the ceiling to reserve agent slots for cavalry mounts, giving ~33 % more troops than vanilla while staying crash-safe

## How It Works

The original mod does not patch combat logic directly. After reverse-engineering the downloaded binary, the controlling behavior is:

1. Try to load `ModSettings.Instance`
2. Read `CustomBattleSize`
3. Apply that value to Bannerlord's battle-size configuration
4. Re-apply it at key lifecycle points and runtime ticks so Bannerlord keeps the configured cap active
5. Override `BannerlordConfig.GetRealBattleSize*` through Harmony so mission spawn logic captures the configured value before the first deployment wave is calculated

This reimplementation now loads settings in this order:

1. `BattleSizeUnlocker.settings.xml` in the module folder
2. The original ModLib-backed `ModSettings.Instance` path, if available
3. The built-in default value of `500`

On the campaign map, press **Ctrl + Shift + F8** to open the built-in battle size selector. Choosing a value saves it to `BattleSizeUnlocker.settings.xml` and applies it immediately.

On current Bannerlord builds, `BannerlordConfig.BattleSize` is no longer a raw troop-count value. It is an option index into internal battle-size tables. This implementation adapts to that by rewriting Bannerlord's internal battle-size tables and selecting the highest valid option index, which avoids the siege-load crash caused by writing raw values like `500` or `2040` directly into the config index.

Current Bannerlord builds also capture the opening mission battle size before `OnMissionBehaviorInitialize` runs. To keep the configured value active for the first siege deployment wave, this reimplementation now patches `BannerlordConfig.GetRealBattleSize`, `GetRealBattleSizeForSiege`, and `GetRealBattleSizeForSallyOut` with Harmony so those early reads return the configured size.

Current Bannerlord builds also clamp `MissionAgentSpawnLogic` to half of the native mission-agent ceiling, which keeps opening deployments near the vanilla ~1000-troop limit on many installs. This reimplementation raises that opening cap: siege and sally-out missions use the full engine agent ceiling (no mounts in sieges), while field battles use two-thirds of the ceiling to reserve agent slots for cavalry mounts (~1360 troops at the 2040 ceiling, up from vanilla's ~1020).

The rewritten tables now use your configured value as the top end for field battles, sieges, and sally-out battles alike. Setting the mod to `2040` therefore makes the highest option in each category resolve to `2040`.

Our implementation follows the same model.

## Prerequisites

- **Mount & Blade II: Bannerlord** installed locally
- **ModLib** is not required. The mod includes its own hotkey-driven settings flow.
- **.NET Framework 4.7.2 targeting pack**
- **Visual Studio 2022** or the **.NET SDK**
- The downloaded original mod kept at `downloaded_mod/BattleSizeUnlocker/` for local source-reference work and the `ModLib.Definitions.dll` build reference

## Project Structure

```text
BattleSizeUnlocker/
├── BattleSizeUnlocker.sln
├── deploy.ps1
├── README.md
├── Module/
│   └── SubModule.xml
├── src/
│   └── BattleSizeUnlocker/
│       ├── BattleSizeUnlocker.csproj
│       ├── BattleSizeRuntime.cs
│       ├── Main.cs
│       ├── Patches/
│       │   └── BattleSizeGetterPatches.cs
│       │   └── MissionAgentSpawnLogicPatches.cs
│       └── ModSettings.cs
└── tests/
    └── BattleSizeUnlocker.Tests/
        ├── BattleSizeUnlocker.Tests.csproj
        ├── BattleSizeConfigTests.cs
        ├── BattleSizeGetterPatchTests.cs
        ├── BattleSizeHotkeyControllerTests.cs
        ├── BattleSizeRuntimeTests.cs
        ├── MainTests.cs
        └── MissionAgentSpawnLogicPatchTests.cs
```

## Build

```powershell
dotnet build src\BattleSizeUnlocker\BattleSizeUnlocker.csproj -c Release
```

## Run Tests

```powershell
dotnet test tests\BattleSizeUnlocker.Tests\BattleSizeUnlocker.Tests.csproj
```

## Deploy

```powershell
.\deploy.ps1
```

Or with a custom Bannerlord path:

```powershell
.\deploy.ps1 -GameFolder "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord"
```

## Player Installation

1. Copy this module into `<Bannerlord>\Modules\BattleSizeUnlocker\`.
2. Make sure the folder contains:
   - `Module\SubModule.xml`
   - `bin\Win64_Shipping_Client\BattleSizeUnlocker.dll`
    - `bin\Win64_Shipping_Client\0Harmony.dll`
   - `bin\Win64_Shipping_Client\ModLib.Definitions.dll`
3. Open the Bannerlord launcher.
4. Enable **BattleSizeUnlocker**.
5. Launch a campaign or sandbox save.
6. On the campaign map, press **Ctrl + Shift + F8** to open the settings screen.

## Changing the Setting

Change the setting with the built-in hotkey screen:

1. Launch Bannerlord.
2. Load into the **campaign map**.
3. Press **Ctrl + Shift + F8**.
4. Select the battle size you want.
5. Click **Apply**.

The selected value is saved to `BattleSizeUnlocker.settings.xml` in the module folder and will be reused the next time the game starts.

Allowed range:

- Minimum: `2`
- Maximum: `2040`
- Default: `500`

Values well above `1000`, especially at the new upper end of the range, can cause performance issues or crashes depending on your hardware and the battle.

If you never open the settings screen, the mod still loads and uses its default battle size of `500`.

## How To Confirm It Works In Game

Use one of these checks:

1. On the campaign map, press **Ctrl + Shift + F8** and choose something obvious like `1200`, `1800`, or `2040`.
2. Start a **custom battle** with enough troops on both sides to exceed the base-game cap.
3. Observe that more troops spawn into the battle than Bannerlord normally allows at the default upper limit.
4. Repeat with a **campaign** or **sandbox** field battle to confirm the configured size is still applied outside custom battles.
5. Reopen the game and press **Ctrl + Shift + F8** again to confirm the previously selected value is marked as current.

If the mod is not working, first check:

- **BattleSizeUnlocker is enabled**
- The module files were copied into the correct `Modules\BattleSizeUnlocker\` folder
- You are on the **campaign map** when pressing **Ctrl + Shift + F8**
- The file `BattleSizeUnlocker.settings.xml` is writable inside the module folder

If siege or sally-out openings still do not reach the configured value, Bannerlord is likely hitting its native agent ceiling for the current build or scene. The mod removes the conservative managed half-cap for those mission openings, but it still does not bypass the engine's real agent limit.

## Reverse-Engineering Notes

The implementation was guided by the downloaded original mod assets stored locally in:

- `downloaded_mod/BattleSizeUnlocker/SubModule.xml`
- `downloaded_mod/BattleSizeUnlocker/bin/Win64_Shipping_Client/BattleSizeUnlocker.dll`
- `downloaded_mod/BattleSizeUnlocker/bin/Win64_Shipping_Client/ModLib.Definitions.dll`

Using ILSpy, the original DLL was confirmed to:

- Define `BattleSizeUnlocker.Main : MBSubModuleBase`
- Define `BattleSizeUnlocker.ModSettings : SettingsBase`
- Expose `CustomBattleSize` with the range `2-2048` and default `500`
- Write `BannerlordConfig.BattleSize` in `OnBeforeInitialModuleScreenSetAsRoot`, `OnMissionBehaviourInitialize` for field battles, and `OnGameStart`

This project intentionally caps the user-facing maximum at `2040`, matching the native mission-agent ceiling reported by the supported Bannerlord build used for this project.
This project also removes the original hard `ModLib` launcher dependency so the module can be enabled without installing the full ModLib module.
This project also adapts the original 2020 implementation to current Bannerlord builds where battle size is stored as a config index rather than a direct troop-count value.
This project also adds a local hotkey-driven settings screen so the user can configure battle size without the full ModLib module.
This project also adds a small Harmony compatibility patch because current Bannerlord builds capture opening deployment size before the original lifecycle hook timing can affect sieges.

## Compatibility

- Designed for Bannerlord builds where `BannerlordConfig.BattleSize` is available
- Does not require **ModLib** to enable in the launcher
- Does not require **ModLib** for configuration; **Ctrl + Shift + F8** opens the built-in settings selector on the campaign map
- **ModLib** remains optional only as a fallback compatibility path
- High battle sizes can stress the engine and your hardware
