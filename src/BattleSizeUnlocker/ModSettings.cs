using System.Xml.Serialization;
using ModLib.Definitions;
using ModLib.Definitions.Attributes;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// ModLib-backed settings for the battle size override.
    /// </summary>
    public class ModSettings : SettingsBase
    {
        /// <inheritdoc />
        [XmlElement]
        public override string ID { get; set; } = "BattleSizeUnlocker";

        /// <inheritdoc />
        public override string ModuleFolderName => "BattleSizeUnlocker";

        /// <inheritdoc />
        public override string ModName => "BattleSizeUnlocker";

        /// <summary>
        /// Gets or sets the battle size value written into Bannerlord's runtime config.
        /// </summary>
        [XmlElement]
        [SettingProperty("Battle size", 2, 2048, 2, 2048, "This setting will override the actual battle size setting for the game.")]
        public int CustomBattleSize { get; set; } = 500;

        /// <summary>
        /// Gets the current settings instance from ModLib.
        /// </summary>
        public static ModSettings Instance => (ModSettings)SettingsDatabase.GetSettings<ModSettings>();
    }
}