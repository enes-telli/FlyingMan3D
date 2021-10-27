namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(SplinePositioner), true)]
    [CanEditMultipleObjects]
    public class SplinePositionerEditor : SplineTracerEditor
    {
        protected override void BodyGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Positioning", EditorStyles.boldLabel);

            serializedObject.Update();
            SerializedProperty mode = serializedObject.FindProperty("_mode");
            EditorGUI.BeginChangeCheck();
            SplinePositioner positioner = (SplinePositioner)target;
            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
            if (positioner.mode == SplinePositioner.Mode.Distance) positioner.position = EditorGUILayout.FloatField("Distance", (float)positioner.position);
            else
            {
                SerializedProperty percent = serializedObject.FindProperty("_result").FindPropertyRelative("percent");

                EditorGUILayout.BeginHorizontal();
                SerializedProperty position = serializedObject.FindProperty("_position");
                double pos = positioner.ClipPercent(percent.floatValue);
                EditorGUI.BeginChangeCheck();
                pos = EditorGUILayout.Slider("Percent", (float)pos, 0f, 1f);
                if (EditorGUI.EndChangeCheck()) position.floatValue = (float)pos;
                if (GUILayout.Button("Set Distance", GUILayout.Width(85)))
                {
                    DistanceWindow w = EditorWindow.GetWindow<DistanceWindow>(true);
                    w.Init(OnSetDistance, positioner.CalculateLength());
                }
                EditorGUILayout.EndHorizontal();


            }
            SerializedProperty targetObject = serializedObject.FindProperty("_targetObject");
            EditorGUILayout.PropertyField(targetObject, new GUIContent("Target Object"));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
            base.BodyGUI();
        }

        void OnSetDistance(float distance)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                SplinePositioner positioner = (SplinePositioner)targets[i];
                double travel = positioner.Travel(0.0, distance, Spline.Direction.Forward);
                positioner.position = travel;
            }
        }
    }
}
