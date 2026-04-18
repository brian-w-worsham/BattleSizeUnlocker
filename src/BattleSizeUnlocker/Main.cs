using HarmonyLib;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Bannerlord submodule entry point that keeps the configured battle size applied.
    /// </summary>
    public class Main : MBSubModuleBase
    {
        /// <summary>Stable module identifier used by the original mod.</summary>
        public const string ModuleId = "BattleSizeUnlocker";

        internal const string HarmonyId = "com.battlesizeunlocker.bannerlord";

        private ModSettings _settings;
        private Harmony _harmony;

        /// <summary>
        /// Gets the cached settings instance used by lifecycle callbacks.
        /// </summary>
        internal ModSettings CurrentSettings => _settings;

        /// <summary>
        /// Clears the cached settings instance.
        /// </summary>
        internal void ClearCachedSettings()
        {
            _settings = null;
        }

        /// <inheritdoc />
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            BattleSizeConfig.Initialize(ResolveSettings, CreateDefaultSettings, GetSettingsFilePath());
            _settings = BattleSizeConfig.Current;

            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll();
            Patches.MissionAgentSpawnLogicPatches.ApplyPatch(_harmony);
        }

        /// <inheritdoc />
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            _harmony?.UnpatchAll(HarmonyId);
        }

        /// <summary>
        /// Loads and caches settings, then applies the configured battle size.
        /// </summary>
        internal void InitializeSettings()
        {
            BattleSizeConfig.Initialize(ResolveSettings, CreateDefaultSettings, GetSettingsFilePath());
            _settings = BattleSizeConfig.Current;
            BattleSizeRuntime.ApplyConfiguredBattleSize(_settings, ApplyBattleSize);
        }

        /// <summary>
        /// Re-applies the configured battle size for the current mission when appropriate.
        /// </summary>
        /// <param name="hasMission">Whether a mission is currently being initialized.</param>
        internal void ApplyBattleSizeForMission(bool hasMission)
        {
            if (BattleSizeRuntime.ShouldApplyToMission(hasMission, _settings))
            {
                ApplyBattleSize(_settings.CustomBattleSize);
            }
        }

        /// <summary>
        /// Re-applies the cached battle size when a game starts.
        /// </summary>
        internal void ApplyBattleSizeForGameStart()
        {
            BattleSizeRuntime.ApplyConfiguredBattleSize(_settings, ApplyBattleSize);
        }

        /// <summary>
        /// Re-applies the cached battle size during application ticks whenever Bannerlord has reset its runtime tables.
        /// </summary>
        internal void ApplyBattleSizeForApplicationTick()
        {
            if (_settings != null && !IsBattleSizeCurrentlyApplied(_settings.CustomBattleSize))
            {
                ApplyBattleSize(_settings.CustomBattleSize);
            }
        }

        /// <summary>
        /// Shows the non-ModLib settings screen and persists the selected battle size.
        /// </summary>
        internal void ShowSettingsScreen()
        {
            BattleSizeHotkeyController.Show(SaveSettings, ApplyBattleSize);
        }

        internal void SaveSettings(ModSettings settings)
        {
            BattleSizeConfig.ReplaceCurrent(settings);
            BattleSizeConfig.Save(GetSettingsFilePath());
            _settings = BattleSizeConfig.Current;
        }

        /// <summary>
        /// Resolves the live settings instance from ModLib.
        /// </summary>
        /// <returns>The current mod settings instance.</returns>
        protected virtual ModSettings ResolveSettings()
        {
            return ModSettings.Instance;
        }

        /// <summary>
        /// Creates the fallback settings instance used when external settings resolution is unavailable.
        /// </summary>
        /// <returns>A default settings instance.</returns>
        protected virtual ModSettings CreateDefaultSettings()
        {
            return new ModSettings();
        }

        /// <summary>
        /// Resolves the path used for the local settings file.
        /// </summary>
        /// <returns>The settings file path override, or <see langword="null" /> to use the default module path.</returns>
        protected virtual string GetSettingsFilePath()
        {
            return null;
        }

        /// <summary>
        /// Determines whether Bannerlord's current runtime config already matches the desired battle size.
        /// </summary>
        /// <param name="battleSize">Desired maximum battle size.</param>
        /// <returns><see langword="true" /> when Bannerlord is already using the desired values.</returns>
        protected virtual bool IsBattleSizeCurrentlyApplied(int battleSize)
        {
            return BattleSizeRuntime.IsBattleSizeApplied(
                battleSize,
                BannerlordConfig.GetRealBattleSize,
                BannerlordConfig.GetRealBattleSizeForSiege,
                BannerlordConfig.GetRealBattleSizeForSallyOut);
        }

        /// <summary>
        /// Applies the supplied battle size to Bannerlord's configuration.
        /// </summary>
        /// <param name="battleSize">Battle size value to write.</param>
        internal virtual void ApplyBattleSize(int battleSize)
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(battleSize);

            // Current Bannerlord builds store BattleSize as an option index, so we rewrite
            // the internal size tables and select the highest valid index instead of writing
            // the raw troop count directly into BannerlordConfig.BattleSize.
            BannerlordConfig.BattleSize = battleSizeOverride.BattleSizeIndex;
            SetBattleSizeTable("_battleSizes", battleSizeOverride.BattleSizes);
            SetBattleSizeTable("_siegeBattleSizes", battleSizeOverride.SiegeBattleSizes);
            SetBattleSizeTable("_sallyOutBattleSizes", battleSizeOverride.SallyOutBattleSizes);
        }

        private static void SetBattleSizeTable(string fieldName, int[] values)
        {
            FieldInfo field = typeof(BannerlordConfig).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                field.SetValue(null, values);
            }
        }

        /// <inheritdoc />
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InitializeSettings();
        }

        /// <inheritdoc />
        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
            InformationManager.DisplayMessage(
                new InformationMessage(
                    $"BattleSizeUnlocker loaded. Press {BattleSizeHotkeyController.HotkeyDisplayText} on the campaign map to open settings.",
                    Colors.Green));
        }

        /// <inheritdoc />
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            ApplyBattleSizeForMission(mission != null);
        }

        /// <inheritdoc />
        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            ApplyBattleSizeForApplicationTick();
            BattleSizeHotkeyController.Tick(Game.Current, ShowSettingsScreen);
        }

        /// <inheritdoc />
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            ApplyBattleSizeForGameStart();
        }
    }
}