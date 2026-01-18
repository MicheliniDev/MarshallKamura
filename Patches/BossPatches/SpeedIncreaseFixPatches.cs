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
                elapsedTime += Time.deltaTime * 1.5f;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BossStateNode), "CheckAttackFrame")]
        public static bool UseCustomYield(BossStateNode __instance, int frame, ref IEnumerator __result)
        {
            var boss = __instance.boss;
            if (!boss || !boss.Animator)
            {
                __result = CreateWaitRoutine(null, frame);
                return false;
            }

            __result = CreateWaitRoutine(boss.Animator, frame);
            return false;
        }

        private static IEnumerator CreateWaitRoutine(Animator anim, int frame)
        {
            if (anim)
                yield return new WaitForAnimationFrame(anim, frame);
        }
    }
}