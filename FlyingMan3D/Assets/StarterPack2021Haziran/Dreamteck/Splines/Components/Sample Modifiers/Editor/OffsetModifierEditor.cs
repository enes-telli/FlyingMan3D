namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class OffsetModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;
        Matrix4x4 matrix = new Matrix4x4();

        public OffsetModifierEditor(SplineUser user, SplineUserEditor editor, OffsetModifier input) : base(user, editor, input)
        {
            module = input;
            title = "Offset Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Offset"))
            {
                ((OffsetModifier)module).AddKey(Vector2.zero, addTime - 0.1, addTime + 0.1);
                user.Rebuild();
            }
        }

        protected override void KeyGUI(SplineSampleModifier.Key key)
        {
            OffsetModifier.OffsetKey offsetKey = (OffsetModifier.OffsetKey)key;
            base.KeyGUI(key);
            offsetKey.offset = EditorGUILayout.Vector2Field("Offset", offsetKey.offset);
        }

        protected override void KeyHandles(SplineSampleModifier.Key key, bool edit)
        {
            if (!isOpen) return;
            bool is2D = user.spline != null && user.spline.is2D;
            SplineSample result = new SplineSample();
            OffsetModifier.OffsetKey offsetKey = (OffsetModifier.OffsetKey)key;
            user.spline.Evaluate(offsetKey.position, result);
            matrix.SetTRS(result.position, Quaternion.LookRotation(result.forward, result.up), Vector3.one * result.size);
            Vector3 pos = matrix.MultiplyPoint(offsetKey.offset);
            if (is2D)
            {
                Handles.DrawLine(result.position, result.position + result.right * offsetKey.offset.x * result.size);
                Handles.DrawLine(result.position, result.position - result.right * offsetKey.offset.x * result.size);
            }
            else Handles.DrawWireDisc(result.position, result.forward, offsetKey.offset.magnitude * result.size);
            Handles.DrawLine(result.position, pos);

            if (edit)
            {
                Vector3 lastPos = pos;
                pos = SplineEditorHandles.FreeMoveRectangle(pos, HandleUtility.GetHandleSize(pos) * 0.1f);
                if (pos != lastPos)
                {
                    pos = matrix.inverse.MultiplyPoint(pos);
                    pos.z = 0f;
                    if (is2D) offsetKey.offset = Vector2.right * pos.x;
                    else offsetKey.offset = pos;
                    user.Rebuild();
                }
            }

            base.KeyHandles(key, edit);
        }
    }
}
