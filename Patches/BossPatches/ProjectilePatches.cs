using DHUtil.Elite.Animation;
using HarmonyLib;
using System;
using System.Collections;

namespace KamuraPrime.Patches.BossPatches
{
    [HarmonyPatch]
    public class ProjectilePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Bullet), "Shoot", [typeof(int), typeof(float), typeof(InGameEntity)])]
        private static void MakeBulletsHome(Bullet __instance, float moveSpeed, InGameEntity shooter)
        {
            if (!Plugin.Instance.IsP2) return;

            var target = Singleton<GameManager>.Instance.PlayerStateMachine.Entity.transform;

            var homing = __instance.gameObject.GetOrAddComponent<BulletHomeController>();
            homing.Initialize(__instance.rigid, moveSpeed, Constants.BULLET_HOME_STRENGTH, target);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EliteCounterShootState), "Attack")]
        public static bool TryDoDoubleShot(EliteCounterShootState __instance, ref IEnumerator __result)
        {
            if (!Plugin.Instance.IsP2) return true;

            __result = DoubleShotSequence(__instance);
            return false;
        }

        private static IEnumerator DoubleShotSequence(EliteCounterShootState state)
        {
            yield return OriginalAttack(state); 
            state.boss.ChangeAnimation(GetAnimationParameter.AimShot);
            yield return OriginalAttack(state);
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(EliteCounterShootState), "Attack")]
        public static IEnumerator OriginalAttack(object instance)
        {
            throw new NotImplementedException("Six Seven Sigma Skibidi Aura Farm");
        }
    }
}