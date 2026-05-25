using System.Reflection;
using BattleSizeUnlocker.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the MissionAgentSpawnLogic Harmony patch.
    /// </summary>
    public class MissionAgentSpawnLogicPatchTests
    {
        [Fact]
        public void ConstructorPatch_PostfixMethod_Exists()
        {
            MethodInfo postfix = typeof(MissionAgentSpawnLogicPatches.ConstructorPatch)
                .GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.NotNull(postfix);
        }

        [Fact]
        public void ApplyPatch_TargetsExpectedConstructorArgumentTypes()
        {
            var spawnLogicType =
                AccessTools.TypeByName("TaleWorlds.MountAndBlade.DefaultBattleMissionAgentSpawnLogic")
                ?? AccessTools.TypeByName("TaleWorlds.MountAndBlade.MissionAgentSpawnLogic");

            Assert.NotNull(spawnLogicType);

            // Verify the constructor AccessTools would find using the same signature used in ApplyPatch.
            var ctor = AccessTools.Constructor(
                spawnLogicType,
                new[] { typeof(IMissionTroopSupplier[]), typeof(BattleSideEnum), typeof(Mission.BattleSizeType) });

            Assert.NotNull(ctor);
        }
    }
}