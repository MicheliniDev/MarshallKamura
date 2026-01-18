using Enemy.StateMachine;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XInput;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KamuraPrime.Patches
{
    [HarmonyPatch]
    public class GeneralPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerStrongAttackPattern), "CheckPattern")]
        private static void ChangeKamuraWeakExtraDamage(ref PlayerStrongAttackPattern __instance, ref PlayerStateMachine stateMachine)
        {
            if (SceneManager.GetActiveScene().name == "Scene_GameDesign_Boss_ART")
                stateMachine.combatProfile.WeakPointAttackInfo.Damages = Constants.KAMURA_DAMAGES;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerStrongAttackPattern), "CheckPattern")]
        private static void RestoreWeakExtraDamage(ref PlayerStrongAttackPattern __instance, ref PlayerStateMachine stateMachine)
        {
            stateMachine.combatProfile.WeakPointAttackInfo.Damages = Constants.ORIGINAL_DAMAGES;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerInGameEntity), "AcceptHealthChange")]
        private static bool EnableDebugInvincible(ref NodeBT __instance, ref bool __result)
        {
            if (Plugin.Instance.IsDebug)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NodeBT), "Start")]
        private static void LogStateChange(ref NodeBT __instance)
        {
            if (!Plugin.Instance.IsDebug) return;

            Plugin.Instance.Log($"Entered State: {__instance.Label}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LocalizedData), nameof(LocalizedData.LocalizedText), MethodType.Getter)]
        private static void ChangeBossName(ref LocalizedData __instance, ref string __result)
        {
            if (__instance.Key == "DLG_CUTSCENE_Captain_Simple_COMMANDER_0_NAME")
                __result = Constants.BOSS_NAME;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DeathUI), nameof(DeathUI.PlayerDeadRoutineIntro))]
        private static bool GoodTimeFrog(ref DeathUI __instance, ref IEnumerator __result)
        {
            if (Plugin.Instance.GoodTimeFrog)
            {
                __result = GoodTimeFrogDead(__instance);
                return false;
            }
            return true;
        }

        public static IEnumerator GoodTimeFrogDead(DeathUI deathUIComp)
        {
            var deathUI = Traverse.Create(deathUIComp);

            yield return new WaitForSecondsRealtime(deathUI.Field<float>("playTerm").Value);
            deathUI.Field<TextMeshProUGUI>("deathCount").Value.text = ":goodtimefrog:";
            deathUI.Method("SetTextAlpha", [1]).GetValue();
            deathUI.Field<Image>("backgroundImage").Value.material.SetFloat(Shader.PropertyToID("_FadeAmount"), -0.1f);
        }
    }
}