using System.Reflection;
using HarmonyLib;
using BattleSizeUnlocker.Patches;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for Harmony-based battle-size getter patches.
    /// </summary>
    public class BattleSizeGetterPatchTests : System.IDisposable
    {
        private readonly int _originalBattleSize;

        public BattleSizeGetterPatchTests()
        {
            _originalBattleSize = BattleSizeRuntime.GetEffectiveBattleSize(BattleSizeConfig.Current);
        }

        public void Dispose()
        {
            BattleSizeConfig.ReplaceCurrent(new ModSettings { CustomBattleSize = _originalBattleSize });
        }

        [Fact]
        public void GetRealBattleSizePatch_Prefix_UsesConfiguredBattleSize()
        {
            BattleSizeConfig.ReplaceCurrent(new ModSettings { CustomBattleSize = BattleSizeRuntime.MaximumBattleSize });
            int result = 0;

            bool executeOriginal = BattleSizeGetterPatches.GetRealBattleSizePatch.Prefix(ref result);

            Assert.Equal(BattleSizeRuntime.MaximumBattleSize, result);
            Assert.False(executeOriginal);
        }

        [Fact]
        public void GetRealBattleSizeForSiegePatch_Prefix_UsesConfiguredBattleSize()
        {
            BattleSizeConfig.ReplaceCurrent(new ModSettings { CustomBattleSize = 1800 });
            int result = 0;

            bool executeOriginal = BattleSizeGetterPatches.GetRealBattleSizeForSiegePatch.Prefix(ref result);

            Assert.Equal(1800, result);
            Assert.False(executeOriginal);
        }

        [Fact]
        public void GetRealBattleSizeForSallyOutPatch_Prefix_UsesConfiguredBattleSize()
        {
            BattleSizeConfig.ReplaceCurrent(new ModSettings { CustomBattleSize = 2000 });
            int result = 0;

            bool executeOriginal = BattleSizeGetterPatches.GetRealBattleSizeForSallyOutPatch.Prefix(ref result);

            Assert.Equal(2000, result);
            Assert.False(executeOriginal);
        }

        [Fact]
        public void GetRealBattleSizePatch_HasCorrectHarmonyPatchAttribute()
        {
            HarmonyPatch attr = typeof(BattleSizeGetterPatches.GetRealBattleSizePatch).GetCustomAttribute<HarmonyPatch>();

            Assert.NotNull(attr);
            Assert.Equal(typeof(BannerlordConfig), attr.info.declaringType);
            Assert.Equal(nameof(BannerlordConfig.GetRealBattleSize), attr.info.methodName);
        }

        [Fact]
        public void GetRealBattleSizeForSiegePatch_HasCorrectHarmonyPatchAttribute()
        {
            HarmonyPatch attr = typeof(BattleSizeGetterPatches.GetRealBattleSizeForSiegePatch).GetCustomAttribute<HarmonyPatch>();

            Assert.NotNull(attr);
            Assert.Equal(typeof(BannerlordConfig), attr.info.declaringType);
            Assert.Equal(nameof(BannerlordConfig.GetRealBattleSizeForSiege), attr.info.methodName);
        }

        [Fact]
        public void GetRealBattleSizeForSallyOutPatch_HasCorrectHarmonyPatchAttribute()
        {
            HarmonyPatch attr = typeof(BattleSizeGetterPatches.GetRealBattleSizeForSallyOutPatch).GetCustomAttribute<HarmonyPatch>();

            Assert.NotNull(attr);
            Assert.Equal(typeof(BannerlordConfig), attr.info.declaringType);
            Assert.Equal(nameof(BannerlordConfig.GetRealBattleSizeForSallyOut), attr.info.methodName);
        }
    }
}