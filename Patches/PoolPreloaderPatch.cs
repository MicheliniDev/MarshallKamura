using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace KamuraPrime.Patches
{
    [HarmonyPatch]
    public class ProjectilePoolPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProjectilePoolingManager), "OnProfileLoaded")]
        public static void PreloadExtraBullets(ProjectilePoolingManager __instance)
        {
            var pools = Traverse.Create(__instance)
                                    .Field<Dictionary<string, ObjectPool<Projectile>>>("objectPools")
                                    .Value;

            if (pools != null && pools.ContainsKey(Constants.BULLET_PROJECTILE_NAME))
            {
                Transform poolParent = GameManager.Instance.transform;

                Plugin.Instance.Log($"Pre-warming pool for: {Constants.BULLET_PROJECTILE_NAME} with {Constants.BULLET_PRELOAD_COUNT} items");
                PrewarmPool(pools[Constants.BULLET_PROJECTILE_NAME], poolParent, Constants.BULLET_PRELOAD_COUNT);
            }
        }

        private static void PrewarmPool(ObjectPool<Projectile> pool, Transform parent, int count)
        {
            var buffer = new List<Projectile>(count);

            for (int i = 0; i < count; i++)
            {
                var projectile = pool.Get();
                if (projectile != null)
                {
                    projectile.transform.SetParent(parent);
                    projectile.gameObject.SetActive(false);
                    buffer.Add(projectile);
                }
            }

            foreach (var p in buffer)
            {
                pool.Release(p);
            }

            Plugin.Instance.Log($"Pre-warmed {buffer.Count} bullets.");
        }
    }
}