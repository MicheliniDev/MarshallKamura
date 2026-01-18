using HarmonyLib;

namespace KamuraPrime.Patches.BossPatches
{
    [HarmonyPatch]
    public class StickyBombPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteAirShootState), "OnSuccess")]
        private static void AddBombsToAirShoot(ref EliteAirShootState __instance)
        {
            Plugin.Instance.SpawnStickyBomb();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteCounterShootState), "OnSuccess")]
        private static void AddBombsToCounter(ref EliteAirShootState __instance)
        {
            Plugin.Instance.SpawnStickyBomb();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteTeleportShootState), "OnSuccess")]
        private static void AddBombsToTeleportShoot(ref EliteAirShootState __instance)
        {
            Plugin.Instance.SpawnStickyBomb();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteJumpShootState), "OnSuccess")]
        private static void AddBombsToJumpShoot(ref EliteAirShootState __instance)
        {
            Plugin.Instance.SpawnStickyBomb();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteGroundShootState), "OnSuccess")]
        private static void AddBombsToGroundShoot(ref EliteAirShootState __instance)
        {
            Plugin.Instance.SpawnStickyBomb();
        }
    }
}
