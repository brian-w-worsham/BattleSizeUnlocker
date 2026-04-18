using System;
using System.IO;
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
        public void InitializeSettings_AppliesDefaultBattleSize_WhenSettingsUnavailable()
        {
            var testMain = new TestMain();

            testMain.InitializeSettings();

            Assert.NotNull(testMain.CurrentSettings);
            Assert.Equal(500, testMain.CurrentSettings.CustomBattleSize);
            Assert.Collection(testMain.AppliedBattleSizes, battleSize => Assert.Equal(500, battleSize));
        }

        [Fact]
        public void InitializeSettings_PrefersLocalSettingsFile_WhenPresent()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = 1500 }
            };

            try
            {
                BattleSizeSettingsStore.Save(testMain.SettingsFilePath, new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize });

                testMain.InitializeSettings();

                Assert.Equal(BattleSizeRuntime.MaximumBattleSize, testMain.CurrentSettings.CustomBattleSize);
                Assert.Collection(testMain.AppliedBattleSizes, battleSize => Assert.Equal(BattleSizeRuntime.MaximumBattleSize, battleSize));
            }
            finally
            {
                DeleteSettingsFile(testMain.SettingsFilePath);
            }
        }

        [Fact]
        public void ApplyBattleSizeForMission_AppliesConfiguredBattleSize_WhenMissionExists()
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
        public void ApplyBattleSizeForMission_DoesNothing_WhenMissionIsMissing()
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
        public void ApplyBattleSizeForMission_AppliesConfiguredBattleSize_ForSiegeMissionInitialization()
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
        public void ApplyBattleSizeForApplicationTick_ReappliesBattleSize_WhenRuntimeConfigDrifts()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize },
                IsBattleSizeAlreadyApplied = false
            };

            testMain.InitializeSettings();
            testMain.AppliedBattleSizes.Clear();

            testMain.ApplyBattleSizeForApplicationTick();

            Assert.Collection(testMain.AppliedBattleSizes, battleSize => Assert.Equal(BattleSizeRuntime.MaximumBattleSize, battleSize));
        }

        [Fact]
        public void ApplyBattleSizeForApplicationTick_DoesNothing_WhenRuntimeConfigAlreadyMatches()
        {
            var testMain = new TestMain
            {
                ResolvedSettings = new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize },
                IsBattleSizeAlreadyApplied = true
            };

            testMain.InitializeSettings();
            testMain.AppliedBattleSizes.Clear();

            testMain.ApplyBattleSizeForApplicationTick();

            Assert.Empty(testMain.AppliedBattleSizes);
        }

        [Fact]
        public void ModuleId_MatchesOriginalModuleIdentifier()
        {
            Assert.Equal("BattleSizeUnlocker", Main.ModuleId);
        }

        private sealed class TestMain : Main
        {
            internal string SettingsFilePath { get; set; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), BattleSizeConfig.SettingsFileName);

            internal ModSettings ResolvedSettings { get; set; }

            internal bool IsBattleSizeAlreadyApplied { get; set; }

            internal List<int> AppliedBattleSizes { get; } = new List<int>();

            protected override ModSettings ResolveSettings()
            {
                return ResolvedSettings;
            }

            internal override void ApplyBattleSize(int battleSize)
            {
                AppliedBattleSizes.Add(battleSize);
            }

            protected override string GetSettingsFilePath()
            {
                return SettingsFilePath;
            }

            protected override bool IsBattleSizeCurrentlyApplied(int battleSize)
            {
                return IsBattleSizeAlreadyApplied;
            }
        }

        private static void DeleteSettingsFile(string settingsFilePath)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath))
            {
                return;
            }

            if (File.Exists(settingsFilePath))
            {
                File.Delete(settingsFilePath);
            }

            string directory = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}