using UnityEditor;
using UnityEngine;
namespace CoreBit.DestructibleSprite
{
    [CustomEditor(typeof(BreakableItem))]
    public class BreakableItemEditor : Editor
    {
        private SerializedProperty layers;
        private SerializedProperty hitsToBreak;
        private SerializedProperty explosionForce;
        private SerializedProperty ignoreInternalCollisions;
        private SerializedProperty breakDirection;

        private SerializedProperty enableFadeOut;
        private SerializedProperty fadeOutDelay;
        private SerializedProperty fadeOutDuration;

        private SerializedProperty enableReset;
        private SerializedProperty resetDelay;
        private SerializedProperty onBreak;
        private void OnEnable()
        {
            // General
            layers = serializedObject.FindProperty("layers");
            hitsToBreak = serializedObject.FindProperty("hitsToBreak");
            explosionForce = serializedObject.FindProperty("explosionForce");
            ignoreInternalCollisions = serializedObject.FindProperty("ignoreInternalCollisions");
            breakDirection = serializedObject.FindProperty("breakDirection");

            // Fade out
            enableFadeOut = serializedObject.FindProperty("enableFadeOut");
            fadeOutDelay = serializedObject.FindProperty("fadeOutDelay");
            fadeOutDuration = serializedObject.FindProperty("fadeOutDuration");

            // Reset
            enableReset = serializedObject.FindProperty("enableReset");
            resetDelay = serializedObject.FindProperty("resetDelay");

            // Events
            onBreak = serializedObject.FindProperty("onBreak");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(layers, new GUIContent("Layers"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Break Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hitsToBreak);
            EditorGUILayout.PropertyField(explosionForce);
            EditorGUILayout.PropertyField(ignoreInternalCollisions);
            EditorGUILayout.PropertyField(breakDirection);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fade Out Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableFadeOut);
            if (enableFadeOut.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fadeOutDelay);
                EditorGUILayout.PropertyField(fadeOutDuration);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reset Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableReset);
            if (enableReset.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(resetDelay);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(onBreak);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
