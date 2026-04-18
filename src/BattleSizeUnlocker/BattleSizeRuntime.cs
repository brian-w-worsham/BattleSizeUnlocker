using System;
using System.Linq;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Contains the isolated decision logic for loading and applying the configured battle size.
    /// </summary>
    internal static class BattleSizeRuntime
    {
        internal sealed class BattleSizeOverride
        {
            internal BattleSizeOverride(int battleSizeIndex, int[] battleSizes, int[] siegeBattleSizes, int[] sallyOutBattleSizes)
            {
                BattleSizeIndex = battleSizeIndex;
                BattleSizes = battleSizes;
                SiegeBattleSizes = siegeBattleSizes;
                SallyOutBattleSizes = sallyOutBattleSizes;
            }

            internal int BattleSizeIndex { get; }

            internal int[] BattleSizes { get; }

            internal int[] SiegeBattleSizes { get; }

            internal int[] SallyOutBattleSizes { get; }
        }

        private static readonly int[] DefaultBattleSizes = { 200, 300, 400, 500, 600, 800, 1000 };

        private static readonly int[] DefaultSiegeBattleSizes = { 150, 230, 320, 425, 540, 625, 1000 };

        private static readonly int[] DefaultSallyOutBattleSizes = { 150, 200, 240, 280, 320, 360, 400 };

        /// <summary>
        /// Resolves the mod settings from the provided source.
        /// </summary>
        /// <param name="settingsProvider">Provider that returns the current settings instance.</param>
        /// <param name="defaultSettingsFactory">Factory used when the provider fails.</param>
        /// <returns>The resolved settings instance, which may be <see langword="null" />.</returns>
        internal static ModSettings LoadSettings(Func<ModSettings> settingsProvider, Func<ModSettings> defaultSettingsFactory)
        {
            if (settingsProvider == null)
            {
                throw new ArgumentNullException(nameof(settingsProvider));
            }

            if (defaultSettingsFactory == null)
            {
                throw new ArgumentNullException(nameof(defaultSettingsFactory));
            }

            try
            {
                return settingsProvider();
            }
            catch
            {
                return defaultSettingsFactory();
            }
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

        /// <summary>
        /// Creates replacement battle-size tables for current Bannerlord builds that store battle size as an option index.
        /// </summary>
        /// <param name="desiredMaximumBattleSize">Desired actual maximum battle size.</param>
        /// <returns>The computed override values to apply to Bannerlord's config tables.</returns>
        internal static BattleSizeOverride CreateBattleSizeOverride(int desiredMaximumBattleSize)
        {
            int normalizedMaximumBattleSize = Math.Max(2, desiredMaximumBattleSize);

            return new BattleSizeOverride(
                battleSizeIndex: DefaultBattleSizes.Length - 1,
                battleSizes: ScaleBattleSizeTable(DefaultBattleSizes, normalizedMaximumBattleSize),
                siegeBattleSizes: ScaleBattleSizeTable(DefaultSiegeBattleSizes, normalizedMaximumBattleSize),
                sallyOutBattleSizes: ScaleBattleSizeTable(DefaultSallyOutBattleSizes, normalizedMaximumBattleSize));
        }

        private static int[] ScaleBattleSizeTable(int[] baseValues, int desiredMaximumBattleSize)
        {
            if (baseValues == null)
            {
                throw new ArgumentNullException(nameof(baseValues));
            }

            return baseValues
                .Select(baseValue => Math.Max(2, (int)Math.Round(baseValue * (desiredMaximumBattleSize / 1000d), MidpointRounding.AwayFromZero)))
                .ToArray();
        }
    }
}