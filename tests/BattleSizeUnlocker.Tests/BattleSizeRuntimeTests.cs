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

            var settings = BattleSizeRuntime.LoadSettings(() => expected);

            Assert.Same(expected, settings);
        }

        [Fact]
        public void LoadSettings_Throws_WhenProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => BattleSizeRuntime.LoadSettings(null));
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
    }
}