using System;
using System.IO;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Manages the persisted battle size settings stored with the module.
    /// </summary>
    internal static class BattleSizeConfig
    {
        internal const string SettingsFileName = "BattleSizeUnlocker.settings.xml";

        internal static ModSettings Current { get; private set; } = new ModSettings();

        internal static void Initialize(Func<ModSettings> modLibSettingsProvider, Func<ModSettings> defaultSettingsFactory, string settingsFilePath = null)
        {
            if (modLibSettingsProvider == null)
            {
                throw new ArgumentNullException(nameof(modLibSettingsProvider));
            }

            if (defaultSettingsFactory == null)
            {
                throw new ArgumentNullException(nameof(defaultSettingsFactory));
            }

            string resolvedSettingsFilePath = ResolveSettingsFilePath(settingsFilePath);
            ModSettings fileSettings = BattleSizeSettingsStore.Load(resolvedSettingsFilePath);
            if (fileSettings != null)
            {
                Current = fileSettings;
                return;
            }

            ReplaceCurrent(BattleSizeRuntime.LoadSettings(modLibSettingsProvider, defaultSettingsFactory) ?? defaultSettingsFactory());
        }

        internal static void Save(string settingsFilePath = null)
        {
            ReplaceCurrent(Current);
            BattleSizeSettingsStore.Save(ResolveSettingsFilePath(settingsFilePath), Current);
        }

        internal static void ReplaceCurrent(ModSettings settings)
        {
            Current = new ModSettings
            {
                CustomBattleSize = BattleSizeRuntime.NormalizeBattleSize(settings?.CustomBattleSize ?? new ModSettings().CustomBattleSize)
            };
        }

        internal static string GetDefaultSettingsFilePath()
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(Main).Assembly.Location);

            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                assemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            string moduleDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", ".."));
            return Path.Combine(moduleDirectory, SettingsFileName);
        }

        private static string ResolveSettingsFilePath(string settingsFilePath)
        {
            return string.IsNullOrWhiteSpace(settingsFilePath)
                ? GetDefaultSettingsFilePath()
                : settingsFilePath;
        }
    }
}