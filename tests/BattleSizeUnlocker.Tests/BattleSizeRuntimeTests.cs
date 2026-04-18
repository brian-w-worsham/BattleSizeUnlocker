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
        public void ShouldApplyToMission_ReturnsTrue_ForFieldBattleWithSettings()
        {
            bool shouldApply = BattleSizeRuntime.ShouldApplyToMission(true, new ModSettings());

            Assert.True(shouldApply);
        }

        [Fact]
        public void ShouldApplyToMission_ReturnsFalse_ForNonFieldBattle()
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
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(4000);

            Assert.Equal(6, battleSizeOverride.BattleSizeIndex);
        }

        [Fact]
        public void CreateBattleSizeOverride_PreservesVanillaTables_ForBattleSize1000()
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(1000);

            Assert.Equal(new[] { 200, 300, 400, 500, 600, 800, 1000 }, battleSizeOverride.BattleSizes);
            Assert.Equal(new[] { 150, 230, 320, 425, 540, 625, 1000 }, battleSizeOverride.SiegeBattleSizes);
            Assert.Equal(new[] { 150, 200, 240, 280, 320, 360, 400 }, battleSizeOverride.SallyOutBattleSizes);
        }

        [Fact]
        public void CreateBattleSizeOverride_ScalesBattleTables_ForBattleSize4000()
        {
            var battleSizeOverride = BattleSizeRuntime.CreateBattleSizeOverride(4000);

            Assert.Equal(new[] { 800, 1200, 1600, 2000, 2400, 3200, 4000 }, battleSizeOverride.BattleSizes);
            Assert.Equal(new[] { 600, 920, 1280, 1700, 2160, 2500, 4000 }, battleSizeOverride.SiegeBattleSizes);
            Assert.Equal(new[] { 600, 800, 960, 1120, 1280, 1440, 1600 }, battleSizeOverride.SallyOutBattleSizes);
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