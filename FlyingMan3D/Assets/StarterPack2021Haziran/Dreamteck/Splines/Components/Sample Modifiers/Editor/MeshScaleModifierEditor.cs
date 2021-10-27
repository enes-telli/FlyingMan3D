namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class MeshScaleModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;

        public MeshScaleModifierEditor(MeshGenerator user, SplineUserEditor editor, MeshScaleModifier input) : base(user, editor, input)
        {
            module = input;
            title = "Scale Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Scale"))
            {
                ((MeshScaleModifier)module).AddKey(addTime - 0.1, addTime + 0.1);
                user.Rebuild();
            }
        }

        protected override void KeyGUI(SplineSampleModifier.Key key)
        {
            MeshScaleModifier.ScaleKey scaleKey = (MeshScaleModifier.ScaleKey)key;
            base.KeyGUI(key);
            scaleKey.scale = EditorGUILayout.Vector2Field("Scale", scaleKey.scale);
        }
    }
}
