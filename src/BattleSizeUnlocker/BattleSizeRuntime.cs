using System;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Contains the isolated decision logic for loading and applying the configured battle size.
    /// </summary>
    internal static class BattleSizeRuntime
    {
        /// <summary>
        /// Resolves the mod settings from the provided source.
        /// </summary>
        /// <param name="settingsProvider">Provider that returns the current settings instance.</param>
        /// <returns>The resolved settings instance, which may be <see langword="null" />.</returns>
        internal static ModSettings LoadSettings(Func<ModSettings> settingsProvider)
        {
            if (settingsProvider == null)
            {
                throw new ArgumentNullException(nameof(settingsProvider));
            }

            return settingsProvider();
        }

        /// <summary>
        /// Applies the configured battle size when settings are available.
        /// </summary>
        /// <param name="settings">Resolved settings instance.</param>
        /// <param name="battleSizeApplier">Action that writes the battle size into the game configuration.</param>
        internal static void ApplyConfiguredBattleSize(ModSettings settings, Action<int> battleSizeApplier)
        {
            if (battleSizeApplier == null)
            {
                throw new ArgumentNullException(nameof(battleSizeApplier));
            }

            if (settings != null)
            {
                battleSizeApplier(settings.CustomBattleSize);
            }
        }

        /// <summary>
        /// Determines whether the configured battle size should be applied for the current mission.
        /// </summary>
        /// <param name="isFieldBattle">Whether the mission is a field battle.</param>
        /// <param name="settings">Resolved settings instance.</param>
        /// <returns><see langword="true" /> when the mission qualifies and settings are available.</returns>
        internal static bool ShouldApplyToMission(bool isFieldBattle, ModSettings settings)
        {
            return isFieldBattle && settings != null;
        }
    }
}