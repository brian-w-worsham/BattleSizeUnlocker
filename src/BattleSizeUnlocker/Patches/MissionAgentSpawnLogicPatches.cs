using HarmonyLib;
using System;
using System.Reflection;
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
        private const string LegacySpawnLogicTypeName = "TaleWorlds.MountAndBlade.MissionAgentSpawnLogic";
        private const string CurrentSpawnLogicTypeName = "TaleWorlds.MountAndBlade.DefaultBattleMissionAgentSpawnLogic";

        /// <summary>
        /// Explicitly patches the MissionAgentSpawnLogic constructor. Called from Main.OnSubModuleLoad
        /// instead of relying on PatchAll attribute discovery, which can silently fail for constructors.
        /// </summary>
        internal static void ApplyPatch(Harmony harmony)
        {
            Type spawnLogicType = ResolveSpawnLogicType();
            if (spawnLogicType == null)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        "[BattleSizeUnlocker] ERROR: Mission spawn logic type not found - opening troop cap patch not applied.",
                        Colors.Red));
                return;
            }

            var ctor = AccessTools.Constructor(
                spawnLogicType,
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
            internal static void Postfix(object __instance, Mission.BattleSizeType battleSizeType)
            {
                if (__instance == null)
                {
                    return;
                }

                Type instanceType = __instance.GetType();
                PropertyInfo maxAgentsProperty = AccessTools.Property(instanceType, "MaxNumberOfAgentsForMission");
                PropertyInfo battleSizeProperty = AccessTools.Property(instanceType, "BattleSize");
                FieldInfo battleSizeField = AccessTools.Field(instanceType, "_battleSize");
                if (maxAgentsProperty == null || battleSizeProperty == null || battleSizeField == null)
                {
                    return;
                }

                MethodInfo maxAgentsGetter = maxAgentsProperty.GetGetMethod(true);
                if (maxAgentsGetter == null)
                {
                    return;
                }

                object maxAgentsTarget = maxAgentsGetter.IsStatic ? null : __instance;
                int engineAgentCeiling = (int)maxAgentsGetter.Invoke(maxAgentsTarget, null);
                int battleSizeBeforeAdjust = (int)battleSizeProperty.GetValue(__instance, null);

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
                    battleSizeField.SetValue(__instance, adjustedBattleSize);
                }
            }
        }

        private static Type ResolveSpawnLogicType()
        {
            return AccessTools.TypeByName(CurrentSpawnLogicTypeName)
                   ?? AccessTools.TypeByName(LegacySpawnLogicTypeName);
        }
    }
}