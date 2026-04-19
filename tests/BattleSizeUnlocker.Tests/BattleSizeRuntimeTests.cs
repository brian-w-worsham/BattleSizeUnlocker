using System;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the isolated battle size decision logic.
    /// </summary>
    public class BattleSizeRuntimeTests
    {
        [Fact]
        public void LoadSettings_ReturnsProviderResult()
        {
            var expected = new ModSettings { CustomBattleSize = 777 };

            var settings = BattleSizeRuntime.LoadSettings(() => expected, () => new ModSettings());

            Assert.Same(expected, settings);
        }

        [Fact]
        public void LoadSettings_Throws_WhenProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => BattleSizeRuntime.LoadSettings(null, () => new ModSettings()));
        }

        [Fact]
        public void LoadSettings_Throws_WhenDefaultFactoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => BattleSizeRuntime.LoadSettings(() => new ModSettings(), null));
        }

        [Fact]
        public void LoadSettings_ReturnsDefaultSettings_WhenProviderThrows()
        {
            var expected = new ModSettings { CustomBattleSize = 500 };

            var settings = BattleSizeRuntime.LoadSettings(
                () => throw new InvalidOperationException("ModLib settings unavailable."),
                () => expected);

            Assert.Same(expected, settings);
        }

        [Fact]
        public void ApplyConfiguredBattleSize_InvokesApplier_WhenSettingsExist()
        {
            var settings = new ModSettings { CustomBattleSize = 1200 };
            int appliedBattleSize = 0;

            BattleSizeRuntime.ApplyConfiguredBattleSize(settings, battleSize => appliedBattleSize = battleSize);

            Assert.Equal(1200, appliedBattleSize);
        }

        [Fact]
        public void ApplyConfiguredBattleSize_DoesNothing_WhenSettingsAreMissing()
        {
            bool applied = false;

            BattleSizeRuntime.ApplyConfiguredBattleSize(null, _ => applied = true);

            Assert.False(applied);
        }

        [Fact]
        public void ApplyConfiguredBattleSize_Throws_WhenApplierIsNull()
        {
            var settings = new ModSettings();

            Assert.Throws<ArgumentNullException>(() => BattleSizeRuntime.ApplyConfiguredBattleSize(settings, null));
        }

        [Fact]
        public void IsBattleSizeApplied_ReturnsTrue_WhenAllRuntimeValuesMatch()
        {
            bool isApplied = BattleSizeRuntime.IsBattleSizeApplied(
                BattleSizeRuntime.MaximumBattleSize,
                () => BattleSizeRuntime.MaximumBattleSize,
                () => BattleSizeRuntime.MaximumBattleSize,
                () => BattleSizeRuntime.MaximumBattleSize);

            Assert.True(isApplied);
        }

        [Fact]
        public void IsBattleSizeApplied_ReturnsFalse_WhenAnyRuntimeValueDiffers()
        {
            bool isApplied = BattleSizeRuntime.IsBattleSizeApplied(
                BattleSizeRuntime.MaximumBattleSize,
                () => BattleSizeRuntime.MaximumBattleSize,
                () => 1000,
                () => BattleSizeRuntime.MaximumBattleSize);

            Assert.False(isApplied);
        }

        [Fact]
        public void NormalizeBattleSize_ClampsValuesAboveSupportedMaximum()
        {
            int normalizedBattleSize = BattleSizeRuntime.NormalizeBattleSize(BattleSizeRuntime.MaximumBattleSize + 1960);

            Assert.Equal(BattleSizeRuntime.MaximumBattleSize, normalizedBattleSize);
        }

        [Fact]
        public void GetEffectiveOpeningBattleSize_ReturnsConfiguredBattleSize_WhenNativeAgentLimitIsHigher()
        {
            int battleSize = BattleSizeRuntime.GetEffectiveOpeningBattleSize(new ModSettings { CustomBattleSize = 1800 }, 2500);

            Assert.Equal(1800, battleSize);
        }

        [Fact]
        public void GetEffectiveOpeningBattleSize_UsesNativeAgentLimit_WhenConfiguredBattleSizeIsHigher()
        {
            int battleSize = BattleSizeRuntime.GetEffectiveOpeningBattleSize(new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize }, 2000);

            Assert.Equal(2000, battleSize);
        }

        [Fact]
        public void GetEffectiveOpeningBattleSize_FallsBackToConfiguredBattleSize_WhenAgentLimitIsUnavailable()
        {
            int battleSize = BattleSizeRuntime.GetEffectiveOpeningBattleSize(new ModSettings { CustomBattleSize = 1400 }, 0);

            Assert.Equal(1400, battleSize);
        }

        [Fact]
        public void GetEffectiveFieldBattleSize_AppliesMountReserveFraction_WhenConfiguredSizeExceedsSafeCeiling()
        {
            // 2040 * 2/3 = 1360
            int battleSize = BattleSizeRuntime.GetEffectiveFieldBattleSize(
                new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize }, 2040);

            Assert.Equal(1360, battleSize);
        }

        [Fact]
        public void GetEffectiveFieldBattleSize_ReturnsConfiguredSize_WhenBelowSafeCeiling()
        {
            int battleSize = BattleSizeRuntime.GetEffectiveFieldBattleSize(
                new ModSettings { CustomBattleSize = 800 }, 2040);

            Assert.Equal(800, battleSize);
        }

        [Fact]
        public void GetEffectiveFieldBattleSize_FallsBackToConfiguredSize_WhenAgentLimitIsUnavailable()
        {
            int battleSize = BattleSizeRuntime.GetEffectiveFieldBattleSize(
                new ModSettings { CustomBattleSize = 1400 }, 0);

            Assert.Equal(1400, battleSize);
        }

        [Fact]
        public void GetEffectiveFieldBattleSize_IsLowerThanOpeningBattleSize_AtSameAgentCeiling()
        {
            var settings = new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize };
            int agentCeiling = 2040;

            int fieldSize = BattleSizeRuntime.GetEffectiveFieldBattleSize(settings, agentCeiling);
            int siegeSize = BattleSizeRuntime.GetEffectiveOpeningBattleSize(settings, agentCeiling);

            Assert.True(fieldSize < siegeSize,
                $"Field battle size ({fieldSize}) should be lower than siege size ({siegeSize}) to reserve agent slots for mounts.");
        }

        [Fact]
        public void ShouldApplyToMission_ReturnsTrue_WhenMissionExistsAndSettingsExist()
        {
            bool shouldApply = BattleSizeRuntime.ShouldApplyToMission(true, new ModSettings());

            Assert.True(shouldApply);
        }

        [Fact]
        public void ShouldApplyToMission_ReturnsFalse_WhenMissionDoesNotExist()
        {
            bool shouldApply = BattleSizeRuntime.ShouldApplyToMission(false, new ModSettings());

            Assert.False(shouldApply);
        }

        [Fact]
        public void ShouldApplyToMission_ReturnsFalse_WhenSettingsMissing()
        {
            bool shouldApply = BattleSizeRuntime.ShouldApplyToMission(true, null);

            Assert.False(shouldApply);
        }

        [Fact]
        public void CreateBattleSizeOverride_UsesHighestValidOptionIndex()
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(BattleSizeRuntime.MaximumBattleSize);

            Assert.Equal(6, battleSizeOverride.BattleSizeIndex);
        }

        [Fact]
        public void CreateBattleSizeOverride_UsesRequestedMaximumForEveryBattleCategory_ForBattleSize1000()
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(1000);

            Assert.Equal(new[] { 200, 300, 400, 500, 600, 800, 1000 }, battleSizeOverride.BattleSizes);
            Assert.Equal(new[] { 150, 230, 320, 425, 540, 625, 1000 }, battleSizeOverride.SiegeBattleSizes);
            Assert.Equal(new[] { 375, 500, 600, 700, 800, 900, 1000 }, battleSizeOverride.SallyOutBattleSizes);
        }

        [Fact]
        public void CreateBattleSizeOverride_ScalesBattleTables_ForSupportedMaximum()
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(BattleSizeRuntime.MaximumBattleSize);

            Assert.Equal(new[] { 408, 612, 816, 1020, 1224, 1632, 2040 }, battleSizeOverride.BattleSizes);
            Assert.Equal(new[] { 306, 469, 653, 867, 1102, 1275, 2040 }, battleSizeOverride.SiegeBattleSizes);
            Assert.Equal(new[] { 765, 1020, 1224, 1428, 1632, 1836, 2040 }, battleSizeOverride.SallyOutBattleSizes);
        }

        [Fact]
        public void CreateBattleSizeOverride_ClampsVerySmallValues()
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(1);

            Assert.All(battleSizeOverride.BattleSizes, value => Assert.True(value >= 2));
            Assert.All(battleSizeOverride.SiegeBattleSizes, value => Assert.True(value >= 2));
            Assert.All(battleSizeOverride.SallyOutBattleSizes, value => Assert.True(value >= 2));
        }
    }
}