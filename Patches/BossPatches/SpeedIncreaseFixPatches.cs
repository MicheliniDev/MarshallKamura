using HarmonyLib;
using KamuraPrime.YieldInstructions;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KamuraPrime.Patches.BossPatches
{
    [HarmonyPatch]
    public class SpeedIncreaseFixPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Blackboard), "Update")]
        private static bool IncreaseDeltaTimeFromSpeed(ref Blackboard __instance)
        {
            if (SceneManager.GetActiveScene().name == "Scene_GameDesign_Boss_ART")
            {
                var elapsedTime = Traverse.Create(__instance).Field<float>("elapsedTime").Value;
                elapsedTime += Time.deltaTime * __instance.Animator.speed;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BossStateNode), "CheckAttackFrame")]
        public static bool UseCustomYield(BossStateNode __instance, int frame, ref IEnumerator __result)
        {
            __result = CreateWaitRoutine(__instance.boss.Animator, frame);
            return false;
        }

        private static IEnumerator CreateWaitRoutine(Animator anim, int frame)
        {
            yield return new WaitForAnimationFrame(anim, frame);
        }
    }
}