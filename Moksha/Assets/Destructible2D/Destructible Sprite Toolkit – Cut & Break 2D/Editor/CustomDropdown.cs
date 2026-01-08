using System;
using UnityEditor;
using UnityEngine;
namespace CoreBit.DestructibleSprite
{
    public class CustomDropdown : EditorWindow
    {
        private string[] options;
        private int selectedIndex;
        private Action<int> onSelect;
        private Vector2 scrollPosition;

        public static void Show(Rect buttonRect, string[] options, int selectedIndex, Action<int> onSelect)
        {
            CustomDropdown window = CreateInstance<CustomDropdown>();
            window.options = options;
            window.selectedIndex = selectedIndex;
            window.onSelect = onSelect;

            Vector2 windowSize = new Vector2(buttonRect.width, Mathf.Min(options.Length * 25 + 10, 200));
            Vector2 position = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y + buttonRect.height));

            window.position = new Rect(position.x, position.y, windowSize.x, windowSize.y);
            window.ShowPopup();
        }
        private void OnGUI()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.22f, 0.22f, 0.22f));

            GUIStyle itemStyle = new GUIStyle("Button")
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(10, 10, 5, 5)
            };

            GUIStyle selectedStyle = new GUIStyle(itemStyle)
            {
                fontStyle = FontStyle.Bold
            };

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < options.Length; i++)
            {
                GUIStyle style = (i == selectedIndex) ? selectedStyle : itemStyle;

                if (GUILayout.Button(options[i], style, GUILayout.Height(23)))
                {
                    onSelect?.Invoke(i);
                    Close();
                }
            }

            GUILayout.EndScrollView();
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}