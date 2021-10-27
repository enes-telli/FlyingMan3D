namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class DreamteckSplinesEditor : SplineEditor
    {
        public SplineComputer spline = null;
        private Transform _transform;
        private DSCreatePointModule _createPointModule = null;

        public DreamteckSplinesEditor(SplineComputer spline, string editorName) : base (spline.transform.localToWorldMatrix, editorName)
        {
            this.spline = spline;
            _transform = spline.transform;
            evaluate = spline.Evaluate;
            evaluateAtPoint = spline.Evaluate;
            evaluatePosition = spline.EvaluatePosition;
            calculateLength = spline.CalculateLength;
            travel = spline.Travel;
            undoHandler = HandleUndo;
            mainModule.onBeforeDeleteSelectedPoints += OnBeforeDeleteSelectedPoints;
            mainModule.onDuplicatePoint += OnDuplicatePoint;
            if (spline.isNewlyCreated)
            {
                if (SplinePrefs.startInCreationMode)
                {
                    open = true;
                    editMode = true;
                    ToggleModule(0);
                }
                spline.isNewlyCreated = false;
            }
            Refresh();
        }

        protected override void Load()
        {
            pointOperations.Add(new PointOperation { name = "Center To Transform", action = delegate { CenterSelection(); } });
            pointOperations.Add(new PointOperation { name = "Move Transform To", action = delegate { MoveTransformToSelection(); } });
            base.Load();
        }

        private void OnDuplicatePoint(int[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                spline.ShiftNodes(points[i], spline.pointCount - 1, 1);
            }
        }

        private void OnBeforeDeleteSelectedPoints()
        {
            string nodeString = "";
            List <Node> deleteNodes = new List<Node>();
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                Node node = spline.GetNode(selectedPoints[i]);
                if (node)
                {
                    spline.DisconnectNode(selectedPoints[i]);
                    if (node.GetConnections().Length == 0)
                    {
                        deleteNodes.Add(node);
                        if (nodeString != "") nodeString += ", ";
                        string trimmed = node.name.Trim();
                        if (nodeString.Length + trimmed.Length > 80) nodeString += "...";
                        else nodeString += node.name.Trim();
                    }
                }
            }

            if (deleteNodes.Count > 0)
            {
                string message = "The following nodes:\r\n" + nodeString + "\r\n were only connected to the currently selected points. Would you like to remove them from the scene?";
                if (EditorUtility.DisplayDialog("Remove nodes?", message, "Yes", "No"))
                {
                    for (int i = 0; i < deleteNodes.Count; i++)
                    {
                        Undo.DestroyObjectImmediate(deleteNodes[i].gameObject);
                    }
                }
            }

            int min = spline.pointCount - 1;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (selectedPoints[i] < min)
                {
                    min = selectedPoints[i];
                }
            }

            int pointsDeletedBefore = 0;
            for (int i = 0; i < spline.pointCount; i++)
            {
                if (selectedPoints.Contains(i))
                {
                    pointsDeletedBefore++;
                    continue;
                }
                Node node = spline.GetNode(i);
                if (pointsDeletedBefore > 0 && node)
                {
                    spline.TransferNode(i, i - pointsDeletedBefore);
                }
            }
        }


        protected override void OnModuleList(List<PointModule> list)
        {
            _createPointModule = new DSCreatePointModule(this);
            list.Add(_createPointModule);
            list.Add(new DeletePointModule(this));
            list.Add(new PointMoveModule(this));
            list.Add(new PointRotateModule(this));
            list.Add(new PointScaleModule(this));
            list.Add(new PointNormalModule(this));
            list.Add(new PointMirrorModule(this));
            list.Add(new PrimitivesModule(this));
        }

        public override void Destroy()
        {
            base.Destroy();
            UpdateSpline();
        }

        public override void DrawInspector()
        {
            Refresh();
            base.DrawInspector();
            UpdateSpline();
        }

        public override void DrawScene(SceneView current)
        {
            Refresh();
            base.DrawScene(current);
            UpdateSpline();
        }

        public override void BeforeSceneGUI(SceneView current)
        {
            Refresh();
            for (int i = 0; i < moduleCount; i++)
            {
                SetupModule(GetModule(i));
            }
            SetupModule(mainModule);
            _createPointModule.createPointColor = SplinePrefs.createPointColor;
            _createPointModule.createPointSize = SplinePrefs.createPointSize;
            base.BeforeSceneGUI(current);
            UpdateSpline();
        }

        public void Refresh()
        {
            _matrix = _transform.localToWorldMatrix;
            points = spline.GetPoints();
            isClosed = spline.isClosed;
            splineType = spline.type;
            sampleRate = spline.sampleRate;
            is2D = spline.is2D;
            color = spline.editorPathColor;
        }

        public void UpdateSpline()
        {
            if (spline == null) return;
            if (!isClosed && spline.isClosed) spline.Break();
            else if(spline.isClosed && points.Length < 4)
            {
                spline.Break();
                isClosed = false;
            }
            spline.SetPoints(points);
            if (isClosed && !spline.isClosed) spline.Close();
            spline.type = splineType;
            spline.sampleRate = sampleRate;
            spline.is2D = is2D;
            spline.EditorUpdateConnectedNodes();
        }

        private void HandleUndo(string title)
        {
            Undo.RecordObject(spline, title);
        }

        public void MoveTransformToSelection() //Move to Dreamteck Splines editor
        {
            RecordUndo("Move Transform To Selection");
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avg += points[selectedPoints[i]].position;
            }
            avg /= selectedPoints.Count;
            _transform.position = avg;
            ResetCurrentModule();
        }

        public void CenterSelection()
        {
            RecordUndo("Center Selection");
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avg += points[selectedPoints[i]].position;
            }
            avg /= selectedPoints.Count;
            Vector3 delta = _transform.position - avg;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                points[selectedPoints[i]].SetPosition(points[selectedPoints[i]].position + delta);
            }
            ResetCurrentModule();
        }

        private void SetupModule(PointModule module)
        {
            module.duplicationDirection = SplinePrefs.duplicationDirection;
            module.highlightColor = SplinePrefs.highlightColor;
            module.showPointNumbers = SplinePrefs.showPointNumbers;
        }
    }
}
