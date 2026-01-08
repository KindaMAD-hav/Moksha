using System;
using UnityEditor;
using UnityEngine;
namespace CoreBit.DestructibleSprite
{
    public class BreakableItemExportWindow : EditorWindow
    {
        // Settings
        private float explosionForce = 20f;
        private BreakDirection breakDirection = BreakDirection.Up;
        private bool canFadeOut = false;
        private float fadeOutDelay = 2f;
        private float fadeOutDuration = 1f;
        private bool enableReset = false;
        private float resetDelay = 5f;
        private bool ignoreInternalCollisions = false;
        private int hitsToBreak = 1;
        // New setting: export policy
        private bool exportOnlyVisibleLayers = true;

        // Updated callback signature to include layer export choice
        private Action<float, BreakDirection, bool, float, float, bool, float, bool, bool, int> onExportCallback;

        // Show window
        public static void ShowWindow(Action<float, BreakDirection, bool, float, float, bool, float, bool, bool, int> onExport)
        {
            var window = EditorWindow.CreateInstance<BreakableItemExportWindow>();
            window.titleContent = new GUIContent("Export Breakable Item");
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 360, 260);

            window.onExportCallback = onExport;

            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            GUILayout.Label("Configure Breakable Item Settings", labelStyle);

            GUILayout.Space(6);

            explosionForce = EditorGUILayout.FloatField(
                new GUIContent("Explosion Power", "How strongly pieces are pushed apart when broken."),
                explosionForce);

            ignoreInternalCollisions = EditorGUILayout.Toggle(
                new GUIContent("Ignore Internal Collisions", "If enabled, pieces won't collide with each other."),
                ignoreInternalCollisions);

            hitsToBreak = EditorGUILayout.IntField(
                new GUIContent("Hits To Break", "How many hits are required to break the item."),
                hitsToBreak);

            breakDirection = (BreakDirection)EditorGUILayout.EnumPopup(
                new GUIContent("Break Direction", "The main direction in which pieces will move after breaking."),
                breakDirection);

            canFadeOut = EditorGUILayout.Toggle(
                new GUIContent("Enable Fade Out", "If enabled, broken pieces will gradually fade away."),
                canFadeOut);

            if (canFadeOut)
            {
                fadeOutDelay = EditorGUILayout.FloatField(
                    new GUIContent("Fade Out Delay", "Time (in seconds) before fading starts."),
                    fadeOutDelay);

                fadeOutDuration = EditorGUILayout.FloatField(
                    new GUIContent("Fade Out Duration", "How long (in seconds) the fading effect lasts."),
                    fadeOutDuration);
            }

            enableReset = EditorGUILayout.Toggle(
                new GUIContent("Enable Reset", "If enabled, the item will reassemble after a delay."),
                enableReset);

            if (enableReset)
            {
                resetDelay = EditorGUILayout.FloatField(
                    new GUIContent("Reset Delay", "Time (in seconds) before the object resets to its original state."),
                    resetDelay);
            }

            GUILayout.Space(8);

            // ✅ New option
            exportOnlyVisibleLayers = EditorGUILayout.Toggle(
                new GUIContent("Export Only Visible Layers", "If enabled, only layers that are currently visible will be included in the export."),
                exportOnlyVisibleLayers);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Cancel", "Close without exporting prefab"), GUILayout.Height(30)))
            {
                Close();
            }

            if (GUILayout.Button(new GUIContent("Export Prefab", "Save this breakable item as a prefab with these settings"), GUILayout.Height(30)))
            {
                onExportCallback?.Invoke(explosionForce, breakDirection, canFadeOut, fadeOutDelay, fadeOutDuration, enableReset, resetDelay, exportOnlyVisibleLayers, ignoreInternalCollisions, hitsToBreak);
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }
    }
}
