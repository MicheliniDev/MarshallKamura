using HarmonyLib;

namespace KamuraPrime.Patches.BossPatches
{
    [HarmonyPatch]
    public class SkipReloadAnimPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteGroundShootState), "OnStart")]
        private static void SkipReloadAnim(ref EliteGroundShootState __instance)
        {
            __instance.ReloadAnimation = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteAirShootState), "OnStart")]
        private static void SkipAirReloadAnim(ref EliteAirShootState __instance)
        {
            __instance.ReloadAnimation = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteCounterShootState), "OnStart")]
        private static void SkipCounterReloadAnim(ref EliteAirShootState __instance)
        {
            __instance.ReloadAnimation = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteJumpShootState), "OnStart")]
        private static void SkipJumpReloadAnim(ref EliteAirShootState __instance)
        {
            __instance.ReloadAnimation = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteTeleportShootState), "OnStart")]
        private static void SkipTelepeloadAnim(ref EliteAirShootState __instance)
        {
            __instance.ReloadAnimation = false;
        }
    }
}
