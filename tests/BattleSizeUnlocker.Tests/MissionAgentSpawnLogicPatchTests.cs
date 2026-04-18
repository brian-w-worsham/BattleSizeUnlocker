using System.Reflection;
using BattleSizeUnlocker.Patches;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace BattleSizeUnlocker.Tests
{
    /// <summary>
    /// Tests for the siege-specific MissionAgentSpawnLogic Harmony patch.
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
            // Verify the constructor AccessTools would find using the same signature used in ApplyPatch.
            var ctor = AccessTools.Constructor(
                typeof(MissionAgentSpawnLogic),
                new[] { typeof(IMissionTroopSupplier[]), typeof(BattleSideEnum), typeof(Mission.BattleSizeType) });

            Assert.NotNull(ctor);
        }
    }
}