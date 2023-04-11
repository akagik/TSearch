using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Room6.TSearch.Editor
{
    public static class MenuHelper
    {
        public static void AddMenuItem(string name, string shortcut, bool isChecked, int priority, Action execute, Func<bool> validate)
        {
            var addMenuItemMethod = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
            addMenuItemMethod?.Invoke(null, new object[] { name, shortcut, isChecked, priority, execute, validate });
        }

        public static void GetMenuItemDefaultShortcuts(
            List<string> outItemNames,
            List<string> outItemDefaultShortcuts)
        {
            var addMenuItemMethod = typeof(Menu).GetMethod("GetMenuItemDefaultShortcuts", BindingFlags.Static | BindingFlags.NonPublic);
            addMenuItemMethod?.Invoke(null, new object[] { outItemNames, outItemDefaultShortcuts });
        }

        public static void AddSeparator(string name, int priority)
        {
            var addSeparatorMethod = typeof(Menu).GetMethod("AddSeparator", BindingFlags.Static | BindingFlags.NonPublic);
            addSeparatorMethod?.Invoke(null, new object[] { name, priority });
        }

        public static void RemoveMenuItem(string name)
        {
            var removeMenuItemMethod = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
            removeMenuItemMethod?.Invoke(null, new object[] { name });
        }

        public static void Update()
        {
            var internalUpdateAllMenus = typeof(EditorUtility).GetMethod("Internal_UpdateAllMenus", BindingFlags.Static | BindingFlags.NonPublic);
            internalUpdateAllMenus?.Invoke(null, null);

            var shortcutIntegrationType = Type.GetType("UnityEditor.ShortcutManagement.ShortcutIntegration, UnityEditor.CoreModule");
            var instanceProp = shortcutIntegrationType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
            var instance = instanceProp?.GetValue(null);
            var rebuildShortcutsMethod = instance?.GetType().GetMethod("RebuildShortcuts", BindingFlags.Instance | BindingFlags.NonPublic);
            rebuildShortcutsMethod?.Invoke(instance, null);
        }
    }
}