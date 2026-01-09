using System;
using UnityEditor;
using UnityEngine;

namespace CoreBit.DestructibleSprite
{
    public class AutoSlicePopup : EditorWindow
    {
        private int horizontalPieces = 3;
        private int verticalPieces = 3;
        private Action<int, int> onSlice;

        public static void ShowWindow(Action<int, int> sliceCallback)
        {
            var window = EditorWindow.CreateInstance<AutoSlicePopup>();
            window.titleContent = new GUIContent("Auto Slice");
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 220, 120);
            window.onSlice = sliceCallback;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label("Auto Slice Settings", EditorStyles.boldLabel);

            horizontalPieces = EditorGUILayout.IntField(
                new GUIContent("Columns (Horizontal)", "Number of pieces horizontally"),
                horizontalPieces);

            verticalPieces = EditorGUILayout.IntField(
                new GUIContent("Rows (Vertical)", "Number of pieces vertically"),
                verticalPieces);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel", GUILayout.Height(28)))
            {
                Close();
            }

            if (GUILayout.Button("Slice", GUILayout.Height(28)))
            {
                onSlice?.Invoke(horizontalPieces, verticalPieces);
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}