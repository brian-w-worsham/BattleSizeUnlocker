using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace BattleSizeUnlocker.Patches
{
    /// <summary>
    /// Ensures Bannerlord's battle-size getters always return the configured value
    /// before mission spawn logic captures the initial deployment size.
    /// </summary>
    internal static class BattleSizeGetterPatches
    {
        private static int ResolveConfiguredBattleSize()
        {
            return BattleSizeRuntime.GetEffectiveBattleSize(BattleSizeConfig.Current);
        }

        [HarmonyPatch(typeof(BannerlordConfig), nameof(BannerlordConfig.GetRealBattleSize))]
        internal static class GetRealBattleSizePatch
        {
            internal static bool Prefix(ref int __result)
            {
                __result = ResolveConfiguredBattleSize();
                return false;
            }
        }

        [HarmonyPatch(typeof(BannerlordConfig), nameof(BannerlordConfig.GetRealBattleSizeForSiege))]
        internal static class GetRealBattleSizeForSiegePatch
        {
            internal static bool Prefix(ref int __result)
            {
                __result = ResolveConfiguredBattleSize();
                return false;
            }
        }

        [HarmonyPatch(typeof(BannerlordConfig), nameof(BannerlordConfig.GetRealBattleSizeForSallyOut))]
        internal static class GetRealBattleSizeForSallyOutPatch
        {
            internal static bool Prefix(ref int __result)
            {
                __result = ResolveConfiguredBattleSize();
                return false;
            }
        }
    }
}