namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class RotationModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;

        public RotationModifierEditor(SplineUser user, SplineUserEditor parent, RotationModifier input) : base(user, parent, input)
        {
            title = "Rotation Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Rotation"))
            {
                ((RotationModifier)module).AddKey(Vector3.zero, addTime - 0.1, addTime + 0.1);
                user.Rebuild();
            }
        }

        protected override void KeyGUI(SplineSampleModifier.Key key)
        {
            RotationModifier.RotationKey rotationKey = (RotationModifier.RotationKey)key;
            base.KeyGUI(key);
            if (!rotationKey.useLookTarget) rotationKey.rotation = EditorGUILayout.Vector3Field("Rotation", rotationKey.rotation);
            rotationKey.useLookTarget = EditorGUILayout.Toggle("Use Look Target", rotationKey.useLookTarget);
            if (rotationKey.useLookTarget) rotationKey.target = (Transform)EditorGUILayout.ObjectField("Target", rotationKey.target, typeof(Transform), true);
        }

        protected override void KeyHandles(SplineSampleModifier.Key key, bool edit)
        {
            RotationModifier.RotationKey rotationKey = (RotationModifier.RotationKey)key;
            SplineSample result = new SplineSample();
            user.spline.Evaluate(rotationKey.position, result);
            if (rotationKey.useLookTarget)
            {
                if (rotationKey.target != null)
                {
                    Handles.DrawDottedLine(result.position, rotationKey.target.position, 5f);
                    if (edit)
                    {
                        Vector3 lastPos = rotationKey.target.position;
                        rotationKey.target.position = Handles.PositionHandle(rotationKey.target.position, Quaternion.identity);
                        if (lastPos != rotationKey.target.position) user.Rebuild();
                    }
                }
            }
            else
            {
                Quaternion directionRot = Quaternion.LookRotation(result.forward, result.up);
                Quaternion rot = directionRot * Quaternion.Euler(rotationKey.rotation);
                SplineEditorHandles.DrawArrowCap(result.position, rot, HandleUtility.GetHandleSize(result.position));

                if (edit)
                {
                    Vector3 lastEuler = rot.eulerAngles;
                    rot = Handles.RotationHandle(rot, result.position);
                    rot = Quaternion.Inverse(directionRot) * rot;
                    rotationKey.rotation = rot.eulerAngles;
                    if (rot.eulerAngles != lastEuler) user.Rebuild();
                }
            }
            base.KeyHandles(key, edit);
        }
    }
}
