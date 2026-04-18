using System;
using System.Linq;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Contains the isolated decision logic for loading and applying the configured battle size.
    /// </summary>
    internal static class BattleSizeRuntime
    {
        internal const int MinimumBattleSize = 2;

        internal const int MaximumBattleSize = 2040;

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
        /// Determines whether the supplied battle size is already active in the current Bannerlord config tables.
        /// </summary>
        /// <param name="desiredBattleSize">Desired maximum battle size.</param>
        /// <param name="currentBattleSizeProvider">Provider for the normal battle maximum.</param>
        /// <param name="currentSiegeBattleSizeProvider">Provider for the siege battle maximum.</param>
        /// <param name="currentSallyOutBattleSizeProvider">Provider for the sally-out battle maximum.</param>
        /// <returns><see langword="true" /> when all current battle-size tables already match the desired maximum.</returns>
        internal static bool IsBattleSizeApplied(
            int desiredBattleSize,
            Func<int> currentBattleSizeProvider,
            Func<int> currentSiegeBattleSizeProvider,
            Func<int> currentSallyOutBattleSizeProvider)
        {
            if (currentBattleSizeProvider == null)
            {
                throw new ArgumentNullException(nameof(currentBattleSizeProvider));
            }

            if (currentSiegeBattleSizeProvider == null)
            {
                throw new ArgumentNullException(nameof(currentSiegeBattleSizeProvider));
            }

            if (currentSallyOutBattleSizeProvider == null)
            {
                throw new ArgumentNullException(nameof(currentSallyOutBattleSizeProvider));
            }

            int normalizedBattleSize = NormalizeBattleSize(desiredBattleSize);
            return currentBattleSizeProvider() == normalizedBattleSize
                && currentSiegeBattleSizeProvider() == normalizedBattleSize
                && currentSallyOutBattleSizeProvider() == normalizedBattleSize;
        }

        /// <summary>
        /// Determines whether the configured battle size should be applied for the current mission.
        /// </summary>
        /// <param name="hasMission">Whether a mission is currently being initialized.</param>
        /// <param name="settings">Resolved settings instance.</param>
        /// <returns><see langword="true" /> when a mission exists and settings are available.</returns>
        internal static bool ShouldApplyToMission(bool hasMission, ModSettings settings)
        {
            return hasMission && settings != null;
        }

        /// <summary>
        /// Clamps the configured battle size to the supported range.
        /// </summary>
        /// <param name="desiredBattleSize">Requested battle size value.</param>
        /// <returns>A battle size value within the supported range.</returns>
        internal static int NormalizeBattleSize(int desiredBattleSize)
        {
            return Math.Min(MaximumBattleSize, Math.Max(MinimumBattleSize, desiredBattleSize));
        }

        /// <summary>
        /// Resolves the effective configured battle size from the active settings source.
        /// </summary>
        /// <param name="settings">Resolved settings instance.</param>
        /// <returns>The normalized effective battle size.</returns>
        internal static int GetEffectiveBattleSize(ModSettings settings)
        {
            return NormalizeBattleSize(settings?.CustomBattleSize ?? new ModSettings().CustomBattleSize);
        }

        /// <summary>
        /// Calculates the maximum opening siege battle size that can fit within Bannerlord's native agent ceiling.
        /// </summary>
        /// <param name="settings">Resolved settings instance.</param>
        /// <param name="maxNumberOfAgentsForMission">Native engine agent limit for the current mission.</param>
        /// <returns>The desired siege opening battle size, capped by the agent ceiling.</returns>
        internal static int GetEffectiveSiegeOpeningBattleSize(ModSettings settings, int maxNumberOfAgentsForMission)
        {
            int desiredBattleSize = GetEffectiveBattleSize(settings);
            if (maxNumberOfAgentsForMission <= 0)
            {
                return desiredBattleSize;
            }

            return Math.Min(desiredBattleSize, maxNumberOfAgentsForMission);
        }

        /// <summary>
        /// Creates replacement battle-size tables for current Bannerlord builds that store battle size as an option index.
        /// </summary>
        /// <param name="desiredMaximumBattleSize">Desired actual maximum battle size.</param>
        /// <returns>The computed override values to apply to Bannerlord's config tables.</returns>
        internal static BattleSizeOverride CreateBattleSizeOverride(int desiredMaximumBattleSize)
        {
            int normalizedMaximumBattleSize = NormalizeBattleSize(desiredMaximumBattleSize);

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

            int baseMaximumBattleSize = baseValues[baseValues.Length - 1];

            return baseValues
                .Select(baseValue => Math.Max(2, (int)Math.Round(baseValue * (desiredMaximumBattleSize / (double)baseMaximumBattleSize), MidpointRounding.AwayFromZero)))
                .ToArray();
        }
    }
}