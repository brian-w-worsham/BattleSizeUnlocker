using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Reads and writes the module's local settings file.
    /// </summary>
    internal static class BattleSizeSettingsStore
    {
        internal static ModSettings Load(string settingsFilePath)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath) || !File.Exists(settingsFilePath))
            {
                return null;
            }

            try
            {
                XDocument document = XDocument.Load(settingsFilePath);
                XElement root = document.Root;

                if (root == null)
                {
                    return null;
                }

                return new ModSettings
                {
                    CustomBattleSize = BattleSizeRuntime.NormalizeBattleSize(ReadInt(root, nameof(ModSettings.CustomBattleSize), new ModSettings().CustomBattleSize))
                };
            }
            catch
            {
                return null;
            }
        }

        internal static void Save(string settingsFilePath, ModSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath) || settings == null)
            {
                return;
            }

            string directory = Path.GetDirectoryName(settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            int normalizedBattleSize = BattleSizeRuntime.NormalizeBattleSize(settings.CustomBattleSize);
            XDocument document = new XDocument(
                new XElement(
                    "BattleSizeUnlockerSettings",
                    new XElement(nameof(ModSettings.CustomBattleSize), normalizedBattleSize.ToString(CultureInfo.InvariantCulture))));

            document.Save(settingsFilePath);
        }

        private static int ReadInt(XElement root, string name, int defaultValue)
        {
            return int.TryParse(root.Element(name)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : defaultValue;
        }
    }
}