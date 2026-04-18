using System.Linq;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the hotkey-driven settings menu logic.
    /// </summary>
    public class BattleSizeHotkeyControllerTests
    {
        [Fact]
        public void IsOpenHotkeyPressed_ReturnsTrue_WhenCtrlShiftAndF8Pressed()
        {
            bool isPressed = BattleSizeHotkeyController.IsOpenHotkeyPressed(true, false, true, false, true);

            Assert.True(isPressed);
        }

        [Fact]
        public void IsOpenHotkeyPressed_ReturnsFalse_WhenShiftIsMissing()
        {
            bool isPressed = BattleSizeHotkeyController.IsOpenHotkeyPressed(true, false, false, false, true);

            Assert.False(isPressed);
        }

        [Fact]
        public void IsOpenHotkeyPressed_ReturnsFalse_WhenControlIsMissing()
        {
            bool isPressed = BattleSizeHotkeyController.IsOpenHotkeyPressed(false, false, true, false, true);

            Assert.False(isPressed);
        }

        [Fact]
        public void CanOpenSettings_ReturnsTrue_ForMapStateWithoutInquiry()
        {
            bool canOpen = BattleSizeHotkeyController.CanOpenSettings("MapState", false, false);

            Assert.True(canOpen);
        }

        [Fact]
        public void BuildSelectableBattleSizes_IncludesCurrentValueAndMaximum()
        {
            var selectableBattleSizes = BattleSizeHotkeyController.BuildSelectableBattleSizes(1375);

            Assert.Contains(1375, selectableBattleSizes);
            Assert.Equal(BattleSizeRuntime.MaximumBattleSize, selectableBattleSizes.Last());
            Assert.Equal(selectableBattleSizes.Distinct().Count(), selectableBattleSizes.Count);
        }

        [Fact]
        public void BuildSelectableBattleSizes_ClampsCurrentValueIntoSupportedRange()
        {
            var selectableBattleSizes = BattleSizeHotkeyController.BuildSelectableBattleSizes(9000);

            Assert.Equal(BattleSizeRuntime.MaximumBattleSize, selectableBattleSizes.Last());
        }
    }
}