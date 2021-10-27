namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SplineDebugEditor : SplineEditorBase
    {
        SplineComputer spline;
        float length = 0f;
       

        public SplineDebugEditor(SplineComputer spline) : base()
        {
            this.spline = spline;
            GetSplineLength();
        }

        void GetSplineLength()
        {
            length = Mathf.RoundToInt(spline.CalculateLength() * 100f) / 100f;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (Event.current.type == EventType.MouseUp) GetSplineLength();

            spline.editorPathColor = EditorGUILayout.ColorField("Color in Scene", spline.editorPathColor);
            bool lastAlwaysDraw = spline.alwaysDraw;
            spline.alwaysDraw = EditorGUILayout.Toggle("Always Draw Spline", spline.alwaysDraw);
            if (lastAlwaysDraw != spline.alwaysDraw)
            {
                if (spline.alwaysDraw)
                {
                    DSSplineDrawer.RegisterComputer(spline);
                }
                else
                {
                    DSSplineDrawer.UnregisterComputer(spline);
                }
            }
            spline.drawThinckness = EditorGUILayout.Toggle("Draw thickness", spline.drawThinckness);
            if (spline.drawThinckness)
            {
                EditorGUI.indentLevel++;
                spline.billboardThickness = EditorGUILayout.Toggle("Always face camera", spline.billboardThickness);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Samples: " + spline.samples.Length + "\n\r" + "Length: " + length, MessageType.Info);
        }

        public override void DrawScene(SceneView current)
        {
            base.DrawScene(current);
            if (Event.current.type == EventType.MouseUp && open)
            {
                GetSplineLength();
            }
        }
    }
}
