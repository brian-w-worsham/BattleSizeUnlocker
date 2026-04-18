# Battle Size Unlocker — Bannerlord Mod

Expands Bannerlord's battle size range and applies your chosen value to campaign, sandbox, and custom battles.

This project is a clean-room reimplementation of the original Battle Size Unlocker mod after inspecting the downloaded `BattleSizeUnlocker.dll` with ILSpy. The implementation intentionally mirrors the original behavior closely, with two explicit project changes: the maximum battle size has been raised to `4000`, and the hard launcher dependency on `ModLib` has been removed so the module can be enabled even when `ModLib` is not installed.

- Uses the original ModLib-backed setting metadata when available
- Expands the setting range to **2-4000**
- Defaults the configured battle size to **500**
- Applies the configured size during module-root initialization, game start, and field-battle mission initialization

## Features

- **Expanded battle size range:** Goes beyond the base game's 200-1000 range
- **Launcher-safe by default:** No `ModLib` module dependency is required just to enable the mod
- **Optional Mod Options integration:** If `ModLib` is installed, the original-style setting metadata is still present
- **Campaign, Sandbox, and Custom Battle support:** Matches the original mod's intended scope
- **Minimal runtime behavior:** No Harmony patches or save editing, just writes the configured value into `BannerlordConfig.BattleSize`

## How It Works

The original mod does not patch combat logic directly. After reverse-engineering the downloaded binary, the controlling behavior is:

1. Try to load `ModSettings.Instance`
2. Read `CustomBattleSize`
3. Apply that value to Bannerlord's battle-size configuration
4. Re-apply it at key lifecycle points so Bannerlord keeps the configured cap

If the optional ModLib settings path is unavailable, the mod falls back to its default settings and still loads.

On current Bannerlord builds, `BannerlordConfig.BattleSize` is no longer a raw troop-count value. It is an option index into internal battle-size tables. This implementation adapts to that by rewriting Bannerlord's internal battle-size tables and selecting the highest valid option index, which avoids the siege-load crash caused by writing raw values like `500` or `4000` directly into the config index.

Our implementation follows the same model.

## Prerequisites

- **Mount & Blade II: Bannerlord** installed locally
- **ModLib** is optional. Install it only if you want the in-game Mod Options UI for this module.
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
│       └── ModSettings.cs
└── tests/
    └── BattleSizeUnlocker.Tests/
        ├── BattleSizeUnlocker.Tests.csproj
        ├── BattleSizeRuntimeTests.cs
        ├── MainTests.cs
        └── ModSettingsTests.cs
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
2. Install **ModLib** only if you want the in-game **Mod Options** menu for this module.
3. Make sure the folder contains:
   - `Module\SubModule.xml`
   - `bin\Win64_Shipping_Client\BattleSizeUnlocker.dll`
   - `bin\Win64_Shipping_Client\ModLib.Definitions.dll`
4. Open the Bannerlord launcher.
5. Enable **BattleSizeUnlocker**.
6. If you also use **ModLib**, enable it too and keep **BattleSizeUnlocker** below it in load order.

## Changing the Setting

If ModLib is installed and enabled, change the setting in-game:

1. Launch Bannerlord.
2. Open **Options**.
3. Open **Mod Options**.
4. Select **BattleSizeUnlocker**.
5. Change **Battle size** to the value you want.

Allowed range:

- Minimum: `2`
- Maximum: `4000`
- Default: `500`

Values well above `1000`, especially at the new upper end of the range, can cause performance issues or crashes depending on your hardware and the battle.

If ModLib is not installed, the mod still loads and uses its default battle size of `500`.

## How To Confirm It Works In Game

Use one of these checks:

1. In **Options > Mod Options > BattleSizeUnlocker**, verify the slider or numeric setting allows values above the vanilla cap of `1000`.
2. Set the battle size to something obvious like `1200` or `1500`.
3. Start a **custom battle** with enough troops on both sides to exceed the base-game cap.
4. Observe that more troops spawn into the battle than Bannerlord normally allows at the default upper limit.
5. Repeat with a **campaign** or **sandbox** field battle to confirm the configured size is still applied outside custom battles.

If the mod is not working, first check:

- **BattleSizeUnlocker is enabled**
- The module files were copied into the correct `Modules\BattleSizeUnlocker\` folder
- If you want the in-game setting menu, **ModLib is installed and enabled**
- If you use ModLib, **BattleSizeUnlocker** is loaded after it

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

This project intentionally extends the original maximum from `2048` to `4000` at the user's request.
This project also removes the original hard `ModLib` launcher dependency so the module can be enabled without installing the full ModLib module.
This project also adapts the original 2020 implementation to current Bannerlord builds where battle size is stored as a config index rather than a direct troop-count value.

## Compatibility

- Designed for Bannerlord builds where `BannerlordConfig.BattleSize` is available
- Does not require **ModLib** to enable in the launcher
- **ModLib** remains optional for in-game configuration UI
- High battle sizes can stress the engine and your hardware
