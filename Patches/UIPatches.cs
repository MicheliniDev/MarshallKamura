using HarmonyLib;

namespace KamuraPrime.Patches
{
    [HarmonyPatch]
    public class UIPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIManager), "Initialize")]
        private static void AddResetButton(ref UIManager __instance)
        {
            CustomUIManager.AddPauseResetBossButton();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenuUI), "ExecuteButtonAction")]
        private static void AddBossChallengeButtonCheck(ref MainMenuUI __instance, ref OutGameButtonUI button)
        {
            if (button.Key == Constants.BOSS_KEY)
            {
                GameManager.Instance.MoveScene("Scene_GameDesign_Boss_ART");
                Plugin.Instance.UnlockAbilities();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PauseUI), "ExecuteButtonAction")]
        private static void AddResetBossChallengeButtonCheck(ref PauseUI __instance, ref OutGameButtonUI button)
        {
            if (button.Key == "Reset")
            {
                GameManager.Instance.PlayerStateMachine.ChangeState(PlayerStateType.Dead);
                GameManager.Instance.UIManager.PauseInputManager.Close();
            }
        }
    }
}
