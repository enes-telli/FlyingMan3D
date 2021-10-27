namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class TransformModuleEditor : SplineUserSubEditor
    {
        private TransformModule motionApplier;

        public TransformModuleEditor(SplineUser user, SplineUserEditor parent, TransformModule input) : base(user, parent)
        {
            title = "Motion";
            motionApplier = input;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            EditorGUI.indentLevel = 1;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position", GUILayout.Width(EditorGUIUtility.labelWidth));
            motionApplier.applyPositionX = EditorGUILayout.Toggle(motionApplier.applyPositionX, GUILayout.Width(30));
            GUILayout.Label("X", GUILayout.Width(20));
            motionApplier.applyPositionY = EditorGUILayout.Toggle(motionApplier.applyPositionY, GUILayout.Width(30));
            GUILayout.Label("Y", GUILayout.Width(20));
            motionApplier.applyPositionZ = EditorGUILayout.Toggle(motionApplier.applyPositionZ, GUILayout.Width(30));
            GUILayout.Label("Z", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
            if (motionApplier.applyPosition)
            {
                EditorGUI.indentLevel = 2;
                motionApplier.offset = EditorGUILayout.Vector2Field("Offset", motionApplier.offset);
            }
            EditorGUI.indentLevel = 1;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rotation", GUILayout.Width(EditorGUIUtility.labelWidth));
            motionApplier.applyRotationX = EditorGUILayout.Toggle(motionApplier.applyRotationX, GUILayout.Width(30));
            GUILayout.Label("X", GUILayout.Width(20));
            motionApplier.applyRotationY = EditorGUILayout.Toggle(motionApplier.applyRotationY, GUILayout.Width(30));
            GUILayout.Label("Y", GUILayout.Width(20));
            motionApplier.applyRotationZ = EditorGUILayout.Toggle(motionApplier.applyRotationZ, GUILayout.Width(30));
            GUILayout.Label("Z", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            if (motionApplier.applyRotation)
            {
                EditorGUI.indentLevel = 2;
                motionApplier.rotationOffset = EditorGUILayout.Vector3Field("Offset", motionApplier.rotationOffset);
            }
            EditorGUI.indentLevel = 1;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale", GUILayout.Width(EditorGUIUtility.labelWidth));
            motionApplier.applyScaleX = EditorGUILayout.Toggle(motionApplier.applyScaleX, GUILayout.Width(30));
            GUILayout.Label("X", GUILayout.Width(20));
            motionApplier.applyScaleY = EditorGUILayout.Toggle(motionApplier.applyScaleY, GUILayout.Width(30));
            GUILayout.Label("Y", GUILayout.Width(20));
            motionApplier.applyScaleZ = EditorGUILayout.Toggle(motionApplier.applyScaleZ, GUILayout.Width(30));
            GUILayout.Label("Z", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            if (motionApplier.applyScale)
            {
                EditorGUI.indentLevel = 2;
                motionApplier.baseScale = EditorGUILayout.Vector3Field("Base scale", motionApplier.baseScale);
            }

            motionApplier.velocityHandleMode = (TransformModule.VelocityHandleMode)EditorGUILayout.EnumPopup("Velocity Mode", motionApplier.velocityHandleMode);

            EditorGUI.indentLevel = 0;
        }
    }
}
