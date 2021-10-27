namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class DSCreatePointModule : CreatePointModule
    {
        DreamteckSplinesEditor dsEditor;
        private bool createNode = false;


        public DSCreatePointModule(SplineEditor editor) : base(editor)
        {
            dsEditor = (DreamteckSplinesEditor)editor;
        }

        public override void LoadState()
        {
            base.LoadState();
            createNode = LoadBool("createNode");
        }

        public override void SaveState()
        {
            base.SaveState();
            SaveBool("createNode", createNode);
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            createNode = EditorGUILayout.Toggle("Create Node", createNode);
        }

        protected override void CreateSplinePoint(Vector3 position, Vector3 normal)
        {
            RecordUndo("Create Point");
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            List<int> indices = new List<int>();
            List<Node> nodes = new List<Node>();
            SplineComputer spline = dsEditor.spline;
            AddPoint();
            bool closeSpline = false;
            if (!isClosed && points.Length >= 3)
            {
                Vector2 first = HandleUtility.WorldToGUIPoint(points[0].position);
                Vector2 last = HandleUtility.WorldToGUIPoint(points[points.Length - 1].position);
                if (Vector2.Distance(first, last) <= 20f)
                {
                    if (EditorUtility.DisplayDialog("Close spline?", "Do you want to make the spline path closed ?", "Yes", "No"))
                    {
                        closeSpline = true;
                        SceneView.currentDrawingSceneView.Focus();
                        SceneView.RepaintAll();
                    }
                }
            }

            if (appendMode == AppendMode.End)
            {
                for (int i = 0; i < indices.Count; i++) nodes[i].AddConnection(spline, indices[i] + 1);
            }

            if (createNode)
            {
                if (appendMode == 0)
                {
                    CreateNodeForPoint(0);
                }
                else
                {
                    CreateNodeForPoint(points.Length - 1);
                }
            }
            if (closeSpline)
            {
                editor.isClosed = true;
            }
            dsEditor.UpdateSpline();
            if (appendMode == AppendMode.Beginning) spline.ShiftNodes(0, spline.pointCount-1, 1);
        }

        protected override void InsertMode(Vector3 screenCoordinates)
        {
            base.InsertMode(screenCoordinates);
            double percent = ProjectScreenSpace(screenCoordinates);
            editor.evaluate(percent, evalResult);
            if (editor.eventModule.mouseRight)
            {
                SplineEditorHandles.DrawCircle(evalResult.position, Quaternion.LookRotation(editorCamera.transform.position - evalResult.position), HandleUtility.GetHandleSize(evalResult.position) * 0.2f);
                return;
            }
            if (SplineEditorHandles.CircleButton(evalResult.position, Quaternion.LookRotation(editorCamera.transform.position - evalResult.position), HandleUtility.GetHandleSize(evalResult.position) * 0.2f, 1.5f, color))
            {
                RecordUndo("Create Point");
                SplinePoint newPoint = new SplinePoint(evalResult.position, evalResult.position);
                newPoint.size = evalResult.size;
                newPoint.color = evalResult.color;
                newPoint.normal = evalResult.up;
                SplinePoint[] newPoints = new SplinePoint[points.Length + 1];
                
                int pointIndex = dsEditor.spline.PercentToPointIndex(percent);
                for (int i = 0; i < newPoints.Length; i++)
                {
                    if (i <= pointIndex) newPoints[i] = points[i];
                    else if (i == pointIndex + 1) newPoints[i] = newPoint;
                    else newPoints[i] = points[i - 1];
                }
                SplineComputer spline = dsEditor.spline;
                points = newPoints;
                lastCreated = points.Length - 1;
                dsEditor.UpdateSpline();
                spline.ShiftNodes(pointIndex + 1, spline.pointCount - 1, 1);
                if (createNode) CreateNodeForPoint(pointIndex + 1);
            }
        }

        void CreateNodeForPoint(int index)
        {
            dsEditor.UpdateSpline();
            GameObject obj = new GameObject("Node_" + (points.Length - 1));
            obj.transform.parent = dsEditor.spline.transform;
            Node node = obj.AddComponent<Node>();
            node.transform.localRotation = Quaternion.identity;
            node.transform.position = points[index].position;
            dsEditor.spline.ConnectNode(node, index);
        }
    }
}
