namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class ColorModifierEditor : SplineSampleModifierEditor
    {
        private float addTime = 0f;

        public ColorModifierEditor(SplineUser user, SplineUserEditor editor, ColorModifier input) : base(user, editor, input)
        {
            module = input;
            title = "Color Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add New Color"))
            {
                ((ColorModifier)module).AddKey(addTime - 0.1, addTime + 0.1);
                user.Rebuild();
            }
        }

        protected override void KeyGUI(SplineSampleModifier.Key key)
        {
            ColorModifier.ColorKey offsetKey = (ColorModifier.ColorKey)key;
            base.KeyGUI(key);
            offsetKey.color = EditorGUILayout.ColorField("Color", offsetKey.color);
            offsetKey.blendMode = (ColorModifier.ColorKey.BlendMode)EditorGUILayout.EnumPopup("Blend Mode", offsetKey.blendMode);
        }

        protected override void KeyHandles(SplineSampleModifier.Key key, bool edit)
        {
            if (!isOpen) return;
            base.KeyHandles(key, edit);
        }
    }
}
