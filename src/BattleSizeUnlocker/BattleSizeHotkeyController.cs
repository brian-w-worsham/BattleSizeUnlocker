using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace BattleSizeUnlocker
{
    /// <summary>
    /// Handles the non-ModLib hotkey flow for opening and saving battle size settings.
    /// </summary>
    internal static class BattleSizeHotkeyController
    {
        internal const string HotkeyDisplayText = "Ctrl + Shift + F8";

        private static readonly int[] SuggestedBattleSizes =
        {
            2,
            200,
            300,
            400,
            500,
            600,
            800,
            1000,
            1200,
            1400,
            1600,
            1800,
            2000,
            BattleSizeRuntime.MaximumBattleSize
        };

        internal static bool IsInquiryOpen { get; private set; }

        internal static void Tick(Game game, Action openSettingsScreen)
        {
            if (openSettingsScreen == null)
            {
                throw new ArgumentNullException(nameof(openSettingsScreen));
            }

            if (!IsOpenHotkeyPressed(
                    Input.IsKeyDown(InputKey.LeftControl),
                    Input.IsKeyDown(InputKey.RightControl),
                    Input.IsKeyDown(InputKey.LeftShift),
                    Input.IsKeyDown(InputKey.RightShift),
                    Input.IsKeyPressed(InputKey.F8)))
            {
                return;
            }

            var activeState = game?.GameStateManager?.ActiveState;
            if (!CanOpenSettings(activeState?.GetType().Name, activeState?.IsMenuState ?? true, IsInquiryOpen))
            {
                return;
            }

            openSettingsScreen();
        }

        internal static void Show(Action<ModSettings> saveSettings, Action<int> applyBattleSize)
        {
            if (IsInquiryOpen || saveSettings == null || applyBattleSize == null)
            {
                return;
            }

            ModSettings currentSettings = BattleSizeConfig.Current ?? new ModSettings();
            int currentBattleSize = BattleSizeRuntime.NormalizeBattleSize(currentSettings.CustomBattleSize);
            var inquiry = new MultiSelectionInquiryData(
                "Battle Size Unlocker",
                BuildInquiryDescription(currentBattleSize),
                BuildInquiryOptions(currentBattleSize),
                true,
                1,
                1,
                "Apply",
                "Cancel",
                selectedOptions =>
                {
                    IsInquiryOpen = false;
                    int selectedBattleSize = selectedOptions != null && selectedOptions.Count > 0
                        ? BattleSizeRuntime.NormalizeBattleSize((int)selectedOptions[0].Identifier)
                        : currentBattleSize;

                    saveSettings(new ModSettings { CustomBattleSize = selectedBattleSize });
                    applyBattleSize(selectedBattleSize);
                    InformationManager.DisplayMessage(new InformationMessage($"Battle size set to {selectedBattleSize}.", Colors.Green));
                },
                _ => { IsInquiryOpen = false; },
                string.Empty,
                false);

            IsInquiryOpen = true;
            MBInformationManager.ShowMultiSelectionInquiry(inquiry, true, false);
        }

        internal static bool CanOpenSettings(string activeStateName, bool isMenuState, bool isInquiryOpen)
        {
            return !isInquiryOpen && !isMenuState && string.Equals(activeStateName, "MapState", StringComparison.Ordinal);
        }

        internal static bool IsOpenHotkeyPressed(bool isLeftControlDown, bool isRightControlDown, bool isLeftShiftDown, bool isRightShiftDown, bool isF8Pressed)
        {
            return (isLeftControlDown || isRightControlDown) && (isLeftShiftDown || isRightShiftDown) && isF8Pressed;
        }

        internal static IReadOnlyList<int> BuildSelectableBattleSizes(int currentBattleSize)
        {
            var values = new SortedSet<int>(SuggestedBattleSizes)
            {
                BattleSizeRuntime.NormalizeBattleSize(currentBattleSize)
            };

            return values.ToList();
        }

        private static string BuildInquiryDescription(int currentBattleSize)
        {
            return $"Current battle size: {currentBattleSize}\nChoose the maximum battle size to apply and save for future sessions. The current Bannerlord build supports up to {BattleSizeRuntime.MaximumBattleSize}.";
        }

        private static List<InquiryElement> BuildInquiryOptions(int currentBattleSize)
        {
            return BuildSelectableBattleSizes(currentBattleSize)
                .Select(value => new InquiryElement(
                    value,
                    value == currentBattleSize ? $"{value} (current)" : value.ToString(),
                    null,
                    true,
                    value == BattleSizeRuntime.MaximumBattleSize ? "Highest supported battle size on this build." : string.Empty))
                .ToList();
        }
    }
}