namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PointMoveModule : PointTransformModule
    {
        public bool snap = false;
        public float snapGridSize = 1f;
        public bool surfaceMode = false;
        public float surfaceOffset = 0f;
        public LayerMask surfaceLayerMask = ~0;

        public PointMoveModule(SplineEditor editor) : base(editor)
        {

        }

        public override GUIContent GetIconOff()
        {
            return EditorGUIUtility.IconContent("MoveTool");
        }

        public override GUIContent GetIconOn()
        {
            return EditorGUIUtility.IconContent("MoveTool On");
        }

        public override void LoadState()
        {
            base.LoadState();
            snap = LoadBool("snap");
            snapGridSize = LoadFloat("snapGridSize", 0.5f);
            surfaceOffset = LoadFloat("surfaceOffset", 0f);
            surfaceMode = LoadBool("surfaceMode");
            surfaceLayerMask = LoadInt("surfaceLayerMask", ~0);
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("snap", snap);
            SaveFloat("snapGridSize", snapGridSize);
            SaveFloat("surfaceOffset", surfaceOffset);
            SaveBool("surfaceMode", surfaceMode);
            SaveInt("surfaceLayerMask", surfaceLayerMask);
        }

        public override void BeforeSceneDraw(SceneView current)
        {
            base.BeforeSceneDraw(current);
            if (Event.current.type == EventType.MouseUp) GetRotation();
        }

        public override void DrawInspector()
        {
            editSpace = (EditSpace)EditorGUILayout.EnumPopup("Edit Space", editSpace);
            surfaceMode = EditorGUILayout.Toggle("Move On Surface", surfaceMode);
            if (surfaceMode)
            {
                surfaceLayerMask = DreamteckEditorGUI.LayermaskField("Surface Mask", surfaceLayerMask);
                surfaceOffset = EditorGUILayout.FloatField("Surface Offset", surfaceOffset);
            }
            snap = EditorGUILayout.Toggle("Snap to Grid", snap);
            if (snap)
            {
                snapGridSize = EditorGUILayout.FloatField("Grid Size", snapGridSize);
                if (snapGridSize < 0.0001f) snapGridSize = 0.0001f;
            }
        }

        public override void DrawScene()
        {
            if (selectedPoints.Count == 0) return;
            Vector3 c = selectionCenter;
            Vector3 lastPos = c;
            if (surfaceMode)
            {
                c = Handles.FreeMoveHandle(c, Quaternion.LookRotation(SceneView.currentDrawingSceneView.camera.transform.position - c), HandleUtility.GetHandleSize(c) * 0.2f, Vector3.zero, Handles.CircleHandleCap);
                if(lastPos != c)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, surfaceLayerMask))
                    {
                        c = hit.point + hit.normal * surfaceOffset;
                        Handles.DrawLine(hit.point, hit.point + hit.normal * HandleUtility.GetHandleSize(hit.point) * 0.5f);
                    }
                }
            } else c = Handles.PositionHandle(c, rotation);
            if (lastPos != c)
            {
                RecordUndo("Move Points");
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    if (isClosed && selectedPoints[i] == points.Length - 1) continue;
                    points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + (c - lastPos));
                    if (snap) points[selectedPoints[i]].SetPosition(SnapPoint(points[selectedPoints[i]].position));
                }
            }

            if (splineType == Spline.Type.Bezier && selectedPoints.Count == 1)
            {
                int index = selectedPoints[0];
                lastPos = points[index].tangent;
                Vector3 newPos = Handles.PositionHandle(points[index].tangent, rotation);
                if (snap) newPos = SnapPoint(newPos);
                if (newPos != lastPos) RecordUndo("Move Tangents");
                points[index].SetTangentPosition(newPos);

                lastPos = points[index].tangent2;
                newPos = Handles.PositionHandle(points[index].tangent2, rotation);
                if (snap) newPos = SnapPoint(newPos);
                if (newPos != lastPos) RecordUndo("Move Tangents");
                points[index].SetTangent2Position(newPos);
            }
        }

        public Vector3 SnapPoint(Vector3 point)
        {
            point.x = Mathf.RoundToInt(point.x / snapGridSize) * snapGridSize;
            point.y = Mathf.RoundToInt(point.y / snapGridSize) * snapGridSize;
            point.z = Mathf.RoundToInt(point.z / snapGridSize) * snapGridSize;
            return point;
        }
    }
}
