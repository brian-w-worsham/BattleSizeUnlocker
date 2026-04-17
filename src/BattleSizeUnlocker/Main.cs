using TaleWorlds.Core;
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

        private ModSettings _settings;

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

        /// <summary>
        /// Loads and caches settings, then applies the configured battle size.
        /// </summary>
        internal void InitializeSettings()
        {
            _settings = BattleSizeRuntime.LoadSettings(ResolveSettings);
            BattleSizeRuntime.ApplyConfiguredBattleSize(_settings, ApplyBattleSize);
        }

        /// <summary>
        /// Re-applies the configured battle size for the current mission when appropriate.
        /// </summary>
        /// <param name="isFieldBattle">Whether the current mission is a field battle.</param>
        internal void ApplyBattleSizeForMission(bool isFieldBattle)
        {
            if (BattleSizeRuntime.ShouldApplyToMission(isFieldBattle, _settings))
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
        /// Resolves the live settings instance from ModLib.
        /// </summary>
        /// <returns>The current mod settings instance.</returns>
        protected virtual ModSettings ResolveSettings()
        {
            return ModSettings.Instance;
        }

        /// <summary>
        /// Applies the supplied battle size to Bannerlord's configuration.
        /// </summary>
        /// <param name="battleSize">Battle size value to write.</param>
        internal virtual void ApplyBattleSize(int battleSize)
        {
            BannerlordConfig.BattleSize = battleSize;
        }

        /// <inheritdoc />
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InitializeSettings();
        }

        /// <inheritdoc />
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            ApplyBattleSizeForMission(mission != null && mission.IsFieldBattle);
        }

        /// <inheritdoc />
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            ApplyBattleSizeForGameStart();
        }
    }
}