# Battle Size Unlocker — Bannerlord Mod

Expands Bannerlord's battle size range and applies your chosen value to campaign, sandbox, and custom battles.

This project is a clean-room reimplementation of the original Battle Size Unlocker mod after inspecting the downloaded `BattleSizeUnlocker.dll` with ILSpy. The implementation intentionally mirrors the original behavior closely:

- Uses a ModLib-backed in-game setting named **Battle size**
- Keeps the original setting range of **2-2048**
- Defaults the configured battle size to **500**
- Applies the configured size during module-root initialization, game start, and field-battle mission initialization

## Features

- **Expanded battle size range:** Goes beyond the base game's 200-1000 range
- **Mod Options integration:** Uses ModLib so players can change the value in-game
- **Campaign, Sandbox, and Custom Battle support:** Matches the original mod's intended scope
- **Minimal runtime behavior:** No Harmony patches or save editing, just writes the configured value into `BannerlordConfig.BattleSize`

## How It Works

The original mod does not patch combat logic directly. After reverse-engineering the downloaded binary, the controlling behavior is:

1. Load `ModSettings.Instance`
2. Read `CustomBattleSize`
3. Assign that value to `BannerlordConfig.BattleSize`
4. Re-apply it at key lifecycle points so Bannerlord keeps the configured cap

Our implementation follows the same model.

## Prerequisites

- **Mount & Blade II: Bannerlord** installed locally
- **ModLib** installed in Bannerlord, because this module depends on it for the in-game settings UI
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

1. Install **ModLib** in Bannerlord first.
2. Copy this module into `<Bannerlord>\Modules\BattleSizeUnlocker\`.
3. Make sure the folder contains:
   - `Module\SubModule.xml`
   - `bin\Win64_Shipping_Client\BattleSizeUnlocker.dll`
   - `bin\Win64_Shipping_Client\ModLib.Definitions.dll`
4. Open the Bannerlord launcher.
5. Enable **ModLib** and **BattleSizeUnlocker**.
6. Keep **BattleSizeUnlocker** below the official TaleWorlds modules and below **ModLib** in load order.

## Changing the Setting

If ModLib is installed correctly, change the setting in-game:

1. Launch Bannerlord.
2. Open **Options**.
3. Open **Mod Options**.
4. Select **BattleSizeUnlocker**.
5. Change **Battle size** to the value you want.

Allowed range:

- Minimum: `2`
- Maximum: `2048`
- Default: `500`

Values well above `1000` can cause performance issues or crashes depending on your hardware and the battle.

## How To Confirm It Works In Game

Use one of these checks:

1. In **Options > Mod Options > BattleSizeUnlocker**, verify the slider or numeric setting allows values above the vanilla cap of `1000`.
2. Set the battle size to something obvious like `1200` or `1500`.
3. Start a **custom battle** with enough troops on both sides to exceed the base-game cap.
4. Observe that more troops spawn into the battle than Bannerlord normally allows at the default upper limit.
5. Repeat with a **campaign** or **sandbox** field battle to confirm the configured size is still applied outside custom battles.

If the mod is not working, first check:

- **ModLib is installed and enabled**
- **BattleSizeUnlocker is enabled**
- **BattleSizeUnlocker is loaded after ModLib**
- The module files were copied into the correct `Modules\BattleSizeUnlocker\` folder

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

## Compatibility

- Designed for Bannerlord builds where `BannerlordConfig.BattleSize` is available
- Requires **ModLib** for the in-game configuration menu
- High battle sizes can stress the engine and your hardware