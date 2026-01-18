using System.Reflection;
using System;
using UnityEngine;

namespace KamuraPrime
{
    public static class Extensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent<T>(out var comp))
                return comp;

            return gameObject.AddComponent<T>();
        }

        public static void SetField(this object target, string fieldName, object value, System.Type baseType = null)
        {
            var type = baseType ?? target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Plugin.Instance.Log($"Could not find field '{fieldName}' on type '{type.Name}'", BepInEx.Logging.LogLevel.Error);
            }
        }
    }
}
