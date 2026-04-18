using System;
using System.IO;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the local XML-backed settings source.
    /// </summary>
    public class BattleSizeConfigTests
    {
        [Fact]
        public void Initialize_NormalizesLegacyLocalSettings_WhenSettingsFileExceedsSupportedMaximum()
        {
            string settingsFilePath = CreateSettingsFilePath();

            try
            {
                BattleSizeSettingsStore.Save(settingsFilePath, new ModSettings { CustomBattleSize = 3600 });

                BattleSizeConfig.Initialize(
                    () => new ModSettings { CustomBattleSize = 1200 },
                    () => new ModSettings(),
                    settingsFilePath);

                Assert.Equal(BattleSizeRuntime.MaximumBattleSize, BattleSizeConfig.Current.CustomBattleSize);
            }
            finally
            {
                DeleteSettingsFile(settingsFilePath);
            }
        }

        [Fact]
        public void Initialize_FallsBackToDefaultSettings_WhenNoProvidersResolveAValue()
        {
            string settingsFilePath = CreateSettingsFilePath();

            try
            {
                BattleSizeConfig.Initialize(
                    () => null,
                    () => new ModSettings { CustomBattleSize = 500 },
                    settingsFilePath);

                Assert.Equal(500, BattleSizeConfig.Current.CustomBattleSize);
            }
            finally
            {
                DeleteSettingsFile(settingsFilePath);
            }
        }

        [Fact]
        public void Save_PersistsNormalizedBattleSize()
        {
            string settingsFilePath = CreateSettingsFilePath();

            try
            {
                BattleSizeConfig.ReplaceCurrent(new ModSettings { CustomBattleSize = 9000 });

                BattleSizeConfig.Save(settingsFilePath);

                ModSettings savedSettings = BattleSizeSettingsStore.Load(settingsFilePath);

                Assert.NotNull(savedSettings);
                Assert.Equal(BattleSizeRuntime.MaximumBattleSize, savedSettings.CustomBattleSize);
            }
            finally
            {
                DeleteSettingsFile(settingsFilePath);
            }
        }

        private static string CreateSettingsFilePath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), BattleSizeConfig.SettingsFileName);
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