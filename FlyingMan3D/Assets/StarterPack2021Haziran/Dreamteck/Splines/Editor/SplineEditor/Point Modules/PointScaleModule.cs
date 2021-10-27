namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PointScaleModule : PointTransformModule
    {
        public bool scaleSize = true;
        public bool scaleTangents = true;


        public PointScaleModule(SplineEditor editor) : base(editor)
        {
        }

        public override GUIContent GetIconOff()
        {
            return EditorGUIUtility.IconContent("ScaleTool");
        }

        public override GUIContent GetIconOn()
        {
            return EditorGUIUtility.IconContent("ScaleTool On");
        }

        public override void LoadState()
        {
            base.LoadState();
            scaleSize = LoadBool("scaleSize");
            scaleTangents = LoadBool("scaleTangents");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("scaleSize", scaleSize);
            SaveBool("scaleTangents", scaleTangents);
        }

        public override void DrawInspector()
        {
            editSpace = (EditSpace)EditorGUILayout.EnumPopup("Edit Space", editSpace);
            scaleSize = EditorGUILayout.Toggle("Scale Sizes", scaleSize);
            scaleTangents = EditorGUILayout.Toggle("Scale Tangents", scaleTangents);
        }

        public override void DrawScene()
        {
            if (selectedPoints.Count == 0) return;
            if (eventModule.mouseLeftUp)
            {
                Reset();
            }
            Vector3 lastScale = scale;
            Vector3 c = selectionCenter;
            scale = Handles.ScaleHandle(scale, c, rotation, HandleUtility.GetHandleSize(c));
            if (lastScale != scale)
            {
                RecordUndo("Scale Points");
                PrepareTransform();
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    points[selectedPoints[i]] = localPoints[selectedPoints[i]];
                    TransformPoint(ref points[selectedPoints[i]], false, scaleTangents, scaleSize);
                }
                SetDirty();
            }
        }
    }
}
