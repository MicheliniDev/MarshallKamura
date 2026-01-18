using HarmonyLib;

namespace KamuraPrime.Patches.BossPatches
{
    [HarmonyPatch]
    public class AttackCancelPatches
    {
        private static bool isSuperArmor;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BossBlackboard), "OnDamaged")]
        private static void CheckCanCancel(ref BossBlackboard __instance, ref float __state)
        {
            isSuperArmor = !AttackManager.CanCancelCurrentState("IsHit");

            if (isSuperArmor)
                __state = __instance.DamageStacking;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BossBlackboard), "OnDamaged")]
        private static void RestoreDamageStack(ref BossBlackboard __instance, ref float __state)
        {
            if (isSuperArmor)
            {
                __instance.DamageStacking = __state;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BossBlackboard), "OnStaminaChanged")]
        private static bool CheckPreventStaminaDecrease(ref BossBlackboard __instance, ref DamageInfo damageInfo)
        {
            if (damageInfo.Amount < 0)
            {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Blackboard), nameof(Blackboard.SetBranchTrigger))]
        private static bool CheckIsHitAllow(ref Blackboard __instance, ref string trigger)
        {
            return AttackManager.CanCancelCurrentState(trigger);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Blackboard), nameof(Blackboard.SkipAndNextNode))]
        private static bool PreventSkipIfSuperArmor(ref Blackboard __instance)
        {
            return AttackManager.CanCancelCurrentState("IsHit");
        }
    }
}