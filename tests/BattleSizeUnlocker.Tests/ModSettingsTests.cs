using System;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the ModLib settings metadata that drives the in-game options UI.
    /// </summary>
    public class ModSettingsTests
    {
        [Fact]
        public void Defaults_MatchOriginalModBehavior()
        {
            var settings = new ModSettings();

            Assert.Equal("BattleSizeUnlocker", settings.ID);
            Assert.Equal("BattleSizeUnlocker", settings.ModuleFolderName);
            Assert.Equal("BattleSizeUnlocker", settings.ModName);
            Assert.Equal(500, settings.CustomBattleSize);
        }

        [Fact]
        public void IdProperty_IsMarkedWithXmlElement()
        {
            PropertyInfo property = typeof(ModSettings).GetProperty(nameof(ModSettings.ID));

            Assert.NotNull(property.GetCustomAttribute<XmlElementAttribute>());
        }

        [Fact]
        public void CustomBattleSize_IsMarkedWithXmlElement()
        {
            PropertyInfo property = typeof(ModSettings).GetProperty(nameof(ModSettings.CustomBattleSize));

            Assert.NotNull(property.GetCustomAttribute<XmlElementAttribute>());
        }

        [Fact]
        public void CustomBattleSize_HasExpandedSettingPropertyMetadata()
        {
            PropertyInfo property = typeof(ModSettings).GetProperty(nameof(ModSettings.CustomBattleSize));
            CustomAttributeData attribute = Assert.Single(property.CustomAttributes.Where(data => data.AttributeType.Name.IndexOf("SettingProperty", StringComparison.Ordinal) >= 0));

            Assert.Collection(
                attribute.ConstructorArguments,
                argument => Assert.Equal("Battle size", argument.Value),
                argument => Assert.Equal(2, Convert.ToInt32(argument.Value)),
            argument => Assert.Equal(4000, Convert.ToInt32(argument.Value)),
                argument => Assert.Equal(2, Convert.ToInt32(argument.Value)),
            argument => Assert.Equal(4000, Convert.ToInt32(argument.Value)),
                argument => Assert.Equal("This setting will override the actual battle size setting for the game.", argument.Value));
        }
    }
}