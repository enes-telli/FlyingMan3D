namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SplineSampleModifierEditor : SplineUserSubEditor
    {
        protected SplineSampleModifier module;
        protected int selected = -1;
        protected bool drawAllKeys = false;

        public SplineSampleModifierEditor(SplineUser user, SplineUserEditor editor, SplineSampleModifier input) : base(user, editor)
        {
            module = input;
            title = "Tracer Module";
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (!isOpen) return;
            List<SplineSampleModifier.Key> keys = module.GetKeys();
            if (keys.Count > 0) drawAllKeys = EditorGUILayout.Toggle("Draw all Modules", drawAllKeys);
            for (int i = 0; i < keys.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (selected == i)
                {
                    EditorGUI.BeginChangeCheck();
                    KeyGUI(keys[i]);
                    if (EditorGUI.EndChangeCheck()) user.Rebuild();
                }
                else EditorGUILayout.LabelField(i + " [" + (Mathf.Round((float)keys[i].start * 10) / 10f) + " - " + (Mathf.Round((float)keys[i].end * 10) / 10f) + "]");
                EditorGUILayout.EndVertical();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (lastRect.Contains(Event.current.mousePosition))
                {
                    if(Event.current.type == EventType.MouseDown)
                    {
                        if(Event.current.button == 0)
                        {
                            selected = i;
                            editor.Repaint();
                        } else if (Event.current.button == 1)
                        {
                            int index = i;
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Delete"), false, delegate
                            {
                                keys.RemoveAt(index);
                                module.SetKeys(keys);
                                editor.Repaint();
                                user.Rebuild();
                            });
                            menu.ShowAsContext();
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            if (keys.Count > 0) module.blend = EditorGUILayout.Slider("Blend", module.blend, 0f, 1f);
        }

        public override void DrawScene()
        {
            base.DrawScene();
            List<SplineSampleModifier.Key> keys = module.GetKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                if(selected == i || drawAllKeys) KeyHandles(keys[i], selected == i);
            }
        }

        protected virtual void KeyGUI(SplineSampleModifier.Key key)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 50f;
            key.start = EditorGUILayout.Slider("Start", (float)key.start, 0f, 1f);
            key.end = EditorGUILayout.Slider("End", (float)key.end, 0f, 1f);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0f;
            float centerStart = (float)key.centerStart, centerEnd = (float)key.centerEnd;
            EditorGUILayout.MinMaxSlider("Center", ref centerStart, ref centerEnd, 0f, 1f);
            key.centerStart = centerStart;
            key.centerEnd = centerEnd;
            if (key.interpolation == null) key.interpolation = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            key.interpolation = EditorGUILayout.CurveField("Interpolation", key.interpolation);
            key.blend = EditorGUILayout.Slider("Blend", key.blend, 0f, 1f);
        }

        protected virtual void KeyHandles(SplineSampleModifier.Key key, bool edit)
        {
            if (!isOpen) return;
            bool changed = false;
            double value = 0f;
            value = key.start;
            SplineComputerEditorHandles.Slider(user.spline, ref value, user.spline.editorPathColor, "Start", SplineComputerEditorHandles.SplineSliderGizmo.ForwardTriangle, 0.8f);
            if (key.start != value)
            {
                key.start = value;
                changed = true;
            }

            value = key.globalCenterStart;
            SplineComputerEditorHandles.Slider(user.spline, ref value, user.spline.editorPathColor, "", SplineComputerEditorHandles.SplineSliderGizmo.Rectangle, 0.6f);
            if (key.globalCenterStart != value)
            {
                key.globalCenterStart = value;
                changed = true;
            }

            value = key.globalCenterEnd;
            SplineComputerEditorHandles.Slider(user.spline, ref value, user.spline.editorPathColor, "", SplineComputerEditorHandles.SplineSliderGizmo.Rectangle, 0.6f);
            if (key.globalCenterEnd != value)
            {
                key.globalCenterEnd = value;
                changed = true;
            }

            value = key.end;
            SplineComputerEditorHandles.Slider(user.spline, ref value, user.spline.editorPathColor, "End", SplineComputerEditorHandles.SplineSliderGizmo.BackwardTriangle, 0.8f);
            if (key.end != value)
            {
                key.end = value;
                changed = true;
            }


            if (Event.current.control)
            {
                value = key.position;
                double lastValue = value;
                SplineComputerEditorHandles.Slider(user.spline, ref value, user.spline.editorPathColor, "", SplineComputerEditorHandles.SplineSliderGizmo.Circle, 0.4f);
                if (value != lastValue)
                {
                    key.position = value;
                    changed = true;
                }
            }

            if (changed) user.Rebuild();
        }

    }
}