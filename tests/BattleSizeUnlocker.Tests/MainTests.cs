using System.Collections.Generic;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the submodule lifecycle behavior.
    /// </summary>
    public class MainTests
    {
        [Fact]
        public void InitializeSettings_CachesAndAppliesConfiguredBattleSize()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = 1500 }
            };

            testMain.InitializeSettings();

            Assert.Equal(1500, testMain.CurrentSettings.CustomBattleSize);
            Assert.Collection(testMain.AppliedBattleSizes, battleSize => Assert.Equal(1500, battleSize));
        }

        [Fact]
        public void InitializeSettings_DoesNotApply_WhenSettingsUnavailable()
        {
            var testMain = new TestMain();

            testMain.InitializeSettings();

            Assert.Null(testMain.CurrentSettings);
            Assert.Empty(testMain.AppliedBattleSizes);
        }

        [Fact]
        public void ApplyBattleSizeForMission_AppliesConfiguredBattleSize_ForFieldBattle()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = 1300 }
            };

            testMain.InitializeSettings();
            testMain.AppliedBattleSizes.Clear();

            testMain.ApplyBattleSizeForMission(true);

            Assert.Collection(testMain.AppliedBattleSizes, battleSize => Assert.Equal(1300, battleSize));
        }

        [Fact]
        public void ApplyBattleSizeForMission_DoesNothing_ForNonFieldBattle()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = 1300 }
            };

            testMain.InitializeSettings();
            testMain.AppliedBattleSizes.Clear();

            testMain.ApplyBattleSizeForMission(false);

            Assert.Empty(testMain.AppliedBattleSizes);
        }

        [Fact]
        public void ApplyBattleSizeForMission_DoesNothing_WhenSettingsNotInitialized()
        {
            var testMain = new TestMain();

            testMain.ApplyBattleSizeForMission(true);

            Assert.Empty(testMain.AppliedBattleSizes);
        }

        [Fact]
        public void ApplyBattleSizeForGameStart_ReappliesCachedBattleSize()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = 1100 }
            };

            testMain.InitializeSettings();
            testMain.AppliedBattleSizes.Clear();

            testMain.ApplyBattleSizeForGameStart();

            Assert.Collection(testMain.AppliedBattleSizes, battleSize => Assert.Equal(1100, battleSize));
        }

        [Fact]
        public void ModuleId_MatchesOriginalModuleIdentifier()
        {
            Assert.Equal("BattleSizeUnlocker", Main.ModuleId);
        }

        private sealed class TestMain : Main
        {
            internal ModSettings ResolvedSettings { get; set; }

            internal List<int> AppliedBattleSizes { get; } = new List<int>();

            protected override ModSettings ResolveSettings()
            {
                return ResolvedSettings;
            }

            internal override void ApplyBattleSize(int battleSize)
            {
                AppliedBattleSizes.Add(battleSize);
            }
        }
    }
}