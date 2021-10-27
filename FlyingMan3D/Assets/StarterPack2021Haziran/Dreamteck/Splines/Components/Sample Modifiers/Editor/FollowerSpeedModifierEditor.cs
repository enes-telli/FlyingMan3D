namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;

    public class FollowerSpeedModifierEditor : SplineSampleModifierEditor
    {
        public bool allowSelection = true;
        private float addTime = 0f;

        public FollowerSpeedModifierEditor(SplineUser user, SplineUserEditor editor, FollowerSpeedModifier input) : base(user, editor, input)
        {
            module = input;
            title = "Speed Modifiers";
        }

        public void ClearSelection()
        {
            selected = -1;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            if (GUILayout.Button("Add Speed Region"))
            {
                ((FollowerSpeedModifier)module).AddKey(addTime - 0.1, addTime + 0.1);
                user.Rebuild();
            }
        }

        protected override void KeyGUI(SplineSampleModifier.Key key)
        {
            FollowerSpeedModifier.SpeedKey offsetKey = (FollowerSpeedModifier.SpeedKey)key;
            base.KeyGUI(key);
            offsetKey.speed = EditorGUILayout.FloatField("Add Speed", offsetKey.speed);
        }
    }
}
