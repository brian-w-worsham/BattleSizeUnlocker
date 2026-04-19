using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleSizeUnlocker.Patches
{
    /// <summary>
    /// Adjusts mission spawn logic so the opening deployment can exceed Bannerlord's conservative
    /// half-agent troop clamp. Siege and sally-out missions use the engine's full agent ceiling;
    /// field battles use a reduced ceiling that reserves agent slots for cavalry mounts.
    /// </summary>
    internal static class MissionAgentSpawnLogicPatches
    {
        private static readonly AccessTools.FieldRef<MissionAgentSpawnLogic, int> BattleSizeField =
            AccessTools.FieldRefAccess<MissionAgentSpawnLogic, int>("_battleSize");

        /// <summary>
        /// Explicitly patches the MissionAgentSpawnLogic constructor. Called from Main.OnSubModuleLoad
        /// instead of relying on PatchAll attribute discovery, which can silently fail for constructors.
        /// </summary>
        internal static void ApplyPatch(Harmony harmony)
        {
            var ctor = AccessTools.Constructor(
                typeof(MissionAgentSpawnLogic),
                new[] { typeof(IMissionTroopSupplier[]), typeof(BattleSideEnum), typeof(Mission.BattleSizeType) });

            if (ctor == null)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        "[BattleSizeUnlocker] ERROR: MissionAgentSpawnLogic constructor not found — opening troop cap patch not applied.",
                        Colors.Red));
                return;
            }

            harmony.Patch(ctor, postfix: new HarmonyMethod(typeof(ConstructorPatch), nameof(ConstructorPatch.Postfix)));
        }

        internal static class ConstructorPatch
        {
            internal static void Postfix(MissionAgentSpawnLogic __instance, Mission.BattleSizeType battleSizeType)
            {
                int engineAgentCeiling = MissionAgentSpawnLogic.MaxNumberOfAgentsForMission;
                int battleSizeBeforeAdjust = __instance.BattleSize;

                int adjustedBattleSize;
                if (battleSizeType == Mission.BattleSizeType.Battle)
                {
                    adjustedBattleSize = BattleSizeRuntime.GetEffectiveFieldBattleSize(
                        BattleSizeConfig.Current,
                        engineAgentCeiling);
                }
                else
                {
                    adjustedBattleSize = BattleSizeRuntime.GetEffectiveOpeningBattleSize(
                        BattleSizeConfig.Current,
                        engineAgentCeiling);
                }

                if (adjustedBattleSize > battleSizeBeforeAdjust)
                {
                    BattleSizeField(__instance) = adjustedBattleSize;
                }
            }
        }
    }
}