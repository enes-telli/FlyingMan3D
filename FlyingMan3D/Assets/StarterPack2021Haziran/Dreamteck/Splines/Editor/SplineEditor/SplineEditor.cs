namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using Dreamteck.Editor;
    using UnityEngine;
    using UnityEditor;
    using Dreamteck.Splines;

    public class SplineEditor : SplineEditorBase
    {
        public enum Space { World, Local };
        public bool editMode = false;
        protected Matrix4x4 _matrix;
        protected string editorName = "SplineEditor";
        public bool isClosed = false;
        public Spline.Type splineType;
        public int sampleRate;
        public Color color = Color.white;
        public bool is2D = false;
        public Space space = Space.World;

        private int module = -1, selectModule = -1, loadedModuleIndex = -1;
        public MainPointModule mainModule;
        private PointModule[] modules = new PointModule[0];

        protected List<PointOperation> pointOperations = new List<PointOperation>();
        private string[] pointOperationStrings = new string[0];

        public SplinePoint[] points = new SplinePoint[0];
        public List<int> selectedPoints = new List<int>();
        protected Vector2 lastClickPoint = Vector2.zero;

        public Tool lastEditorTool = Tool.None;

        float editLabelAlpha = 0f;
        Vector2 editLabelPosition = Vector2.zero;

        public Matrix4x4 matrix
        {
            get { return _matrix; }
        }

        float lastEmptyClickTime = 0f;

        protected GUIContent[] toolContents = new GUIContent[0], toolContentsSelected = new GUIContent[0];

        protected bool pointToolsToggle = false;

        protected Toolbar toolbar;
        protected SplineSample evalResult = new SplineSample();
        bool emptyClick = false;

        public delegate void SplineEvaluation(double percent, SplineSample result);
        public delegate void SplinePointEvaluation(int pointIndex, SplineSample result);
        public delegate Vector3 SplineEvaluatePosition(double percent);
        public delegate float SplineCalculateLength(double from, double to);
        public delegate double SplineTravel(double start, float distance, Spline.Direction direction);

        public SplineEvaluation evaluate;
        public SplinePointEvaluation evaluateAtPoint;
        public SplineEvaluatePosition evaluatePosition;
        public SplineCalculateLength calculateLength;
        public SplineTravel travel;
        public EmptyHandler selectionChangeHandler;

        public int moduleCount
        {
            get { return modules.Length; }
        }

        public PointModule currentModule
        {
            get
            {
                if (module < 0 || module >= modules.Length) return null;
                else return modules[module];
            }
        }

        public SplineEditor(Matrix4x4 transformMatrix, string editorName) : base()
        {
            _matrix = transformMatrix;
            this.editorName = editorName;
            mainModule = new MainPointModule(this);
            mainModule.onSelectionChanged += OnSelectionChanged;
            List<PointModule> moduleList = new List<PointModule>();
            OnModuleList(moduleList);
            modules = moduleList.ToArray();
            toolContents = new GUIContent[modules.Length];
            toolContentsSelected = new GUIContent[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].onSelectionChanged += OnSelectionChanged;
                toolContents[i] = modules[i].GetIconOff();
                toolContentsSelected[i] = modules[i].GetIconOn();
            }
            toolbar = new Toolbar(toolContents, toolContentsSelected, 35f);

            pointOperations.Add(new PointOperation { name = "Flat X", action = delegate { FlatSelection(0); } });
            pointOperations.Add(new PointOperation { name = "Flat Y", action = delegate { FlatSelection(1); } });
            pointOperations.Add(new PointOperation { name = "Flat Z", action = delegate { FlatSelection(2); } });
            pointOperations.Add(new PointOperation { name = "Mirror X", action = delegate { MirrorSelection(0); } });
            pointOperations.Add(new PointOperation { name = "Mirror Y", action = delegate { MirrorSelection(1); } });
            pointOperations.Add(new PointOperation { name = "Mirror Z", action = delegate { MirrorSelection(2); } });
            pointOperations.Add(new PointOperation { name = "Distribute Evenly", action = delegate { DistributeEvenly(); } });
            pointOperations.Add(new PointOperation { name = "Auto Bezier Tangents", action = delegate { AutoTangents(); } });

            pointOperationStrings = new string[pointOperations.Count + 1];
            pointOperationStrings[0] = "Point Operations";
            for (int i = 0; i < pointOperations.Count; i++)
            {
                pointOperationStrings[i + 1] = pointOperations[i].name;
            }
        }

        public PointModule GetModule(int index)
        {
            return modules[index];
        }

        public override void UndoRedoPerformed()
        {
            
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if(selectedPoints[i] >= points.Length)
                {
                    selectedPoints.RemoveAt(i);
                    i--;
                }
            }
            ResetCurrentModule();
        }

        protected virtual void OnModuleList(List<PointModule> list)
        {
            list.Add(new CreatePointModule(this));
            list.Add(new DeletePointModule(this));
            list.Add(new PointMoveModule(this));
            list.Add(new PointRotateModule(this));
            list.Add(new PointScaleModule(this));
            list.Add(new PointNormalModule(this));
            list.Add(new PointMirrorModule(this));
        }

        public override void Destroy()
        {
            base.Destroy();
            mainModule.Deselect();
            if (currentModule != null) currentModule.Deselect();
            if(lastEditorTool != Tool.None && Tools.current == Tool.None) Tools.current = lastEditorTool;
        }

        void OnSelectionChanged()
        {
            ResetCurrentModule();
            Repaint();
            if (selectionChangeHandler != null) selectionChangeHandler();
        }

        protected override void Save()
        {
            base.Save();
            EditorPrefs.SetBool(GetSaveName("editMode"), editMode);
            EditorPrefs.SetBool(GetSaveName("pointToolsToggle"), pointToolsToggle);
        }

        protected override void Load()
        {
            base.Load();
            editMode = EditorPrefs.GetBool(GetSaveName("editMode"), false);
            pointToolsToggle = EditorPrefs.GetBool(GetSaveName("pointToolsToggle"), false);
        }

        private void HandleEditModeToggle()
        {
            if(Event.current.type == EventType.KeyDown)
            {
                if (editMode && Event.current.keyCode == KeyCode.Escape)
                {
                    if(module >= 0)
                    {
                        UntoggleCurrentModule();
                        Repaint();
                    } else
                    {
                        editMode = false;
                        Repaint();
                    }
                }
                if (Event.current.control && Event.current.keyCode == KeyCode.E) {
                    editMode = !editMode;
                    Repaint();
                }
            }
        }

        public override void DrawInspector()
        {
            HandleEditModeToggle();
            base.DrawInspector();
            if (editMode)
            {
                if (!gizmosEnabled)
                {
                    EditorGUILayout.HelpBox("Gizmos are disabled in the scene view. Enable Gizmos in the scene view for the spline editor to work.", MessageType.Error);
                }
                EditorGUILayout.Space();
                DrawToolMenu();
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                if (currentModule != null) currentModule.DrawInspector();
                DreamteckEditorGUI.DrawSeparator();
                PointPanel();
                if (EditorGUI.EndChangeCheck()) ResetCurrentModule();
            } else
            {
                if (GUILayout.Button("Edit"))
                {
                    editMode = true;
                }
            }
        } 

        void DrawToolMenu()
        {
            EditorGUILayout.BeginHorizontal();
            if (loadedModuleIndex >= 0)
            {
                ToggleModule(loadedModuleIndex);
                loadedModuleIndex = -1;
            }
            selectModule = module;
            EditorGUI.BeginChangeCheck();
            toolbar.Draw(ref selectModule);
            if (EditorGUI.EndChangeCheck())
            {
                ToggleModule(selectModule);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        void PointPanel()
        {
            if (points.Length == 0)
            {
                EditorGUILayout.LabelField("No control points available.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            mainModule.DrawInspector();
            PointMenu();
            if(selectedPoints.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                int pointOperation = EditorGUILayout.Popup(0, pointOperationStrings);
                if (pointOperation > 0)
                {
                    pointOperations[pointOperation - 1].action.Invoke();
                    pointOperation = 0;
                }
                    EditorGUILayout.EndHorizontal();
            }
        }

        public virtual void BeforeSceneGUI(SceneView current)
        {
            mainModule.BeforeSceneDraw(current);
            if (module >= 0 && module < modules.Length)
            {
                modules[module].BeforeSceneDraw(current);
            }
        }

        public override void DrawScene(SceneView current)
        {
            HandleEditModeToggle();
            if (!editMode)
            {
                return;
            }
            base.DrawScene(current);
            Event e = Event.current;
            if (Tools.current != Tool.None)
            {
                lastEditorTool = Tools.current;
                Tools.current = Tool.None;
            }
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.GetTypeForControl(controlID) == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

            if (eventModule.mouseLeftDown) lastClickPoint = e.mousePosition;
            EditorGUI.BeginChangeCheck();
            mainModule.DrawScene();
            if (currentModule != null)
            {
                currentModule.DrawScene();
                if (currentModule is CreatePointModule)
                {
                    if (eventModule.mouseLeftDown && eventModule.mouseRight)
                    {
                        GUIUtility.hotControl = -1;
                        ToggleModule(0);
                    }
                }
            }
            if(eventModule.mouseLeftDown) emptyClick = GUIUtility.hotControl == 0;

            if (emptyClick)
            {
                if (eventModule.mouseLeft && !mainModule.isDragging && Vector2.Distance(lastClickPoint, e.mousePosition) >= mainModule.minimumRectSize && !eventModule.alt)
                {
                    mainModule.StartDrag(lastClickPoint);
                    emptyClick = false;
                }
            }

            if (eventModule.mouseLeftUp)
            {
                if (mainModule.isDragging) mainModule.FinishDrag();
                else
                {
                    if (emptyClick && !eventModule.alt)
                    {
                        if(selectedPoints.Count > 0) mainModule.ClearSelection();
                        else if(editMode)
                        {
                            if (Time.realtimeSinceStartup - lastEmptyClickTime <= 0.3f)
                            {
                                editMode = false;
                            }
                            else
                            {
                                editLabelAlpha = 1f;
                                editLabelPosition = e.mousePosition;
                                lastEmptyClickTime = Time.realtimeSinceStartup;
                            }
                        }
                    }
                }
            }


            if (!eventModule.mouseRight && !eventModule.mouseLeft && e.type == EventType.KeyDown && !e.control)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Q:
                        if (module == 0) ToggleModule(1);
                        else ToggleModule(0);
                        e.Use(); break;
                    case KeyCode.W: ToggleModule(2); e.Use(); break;
                    case KeyCode.E: ToggleModule(3); e.Use(); break;
                    case KeyCode.R: ToggleModule(4); e.Use(); break;
                    case KeyCode.T: ToggleModule(5); e.Use(); break;
                    case KeyCode.Y: ToggleModule(6); e.Use(); break;
                }
            }

            if(editLabelAlpha > 0f)
            {
                Handles.BeginGUI();
                GUI.contentColor = new Color(1f, 1f, 1f, editLabelAlpha);
                DreamteckEditorGUI.Label(new Rect(editLabelPosition, new Vector2(140, 50)), "Click Again To Exit");
                Handles.EndGUI();
                editLabelAlpha = Mathf.MoveTowards(editLabelAlpha, 0f, Time.deltaTime * 0.05f);
                Repaint();
            }
        }

        public void ToggleModule(int index)
        {
            Tools.current = Tool.None;
            if (currentModule != null) currentModule.Deselect();
            if (index == module) module = -1;
            else
            {
                module = index;
                ResetCurrentModule();
                currentModule.Select();
            }
            Repaint();
        }

        public void UntoggleCurrentModule()
        {
            if (currentModule != null) currentModule.Deselect();
            module = -1;
            Repaint();
        }


        void PointMenu()
        {
            if (selectedPoints.Count == 0 || points.Length == 0) return;
            //Otherwise show the editing menu + the point selection menu
            Vector3 avgPos = Vector3.zero;
            Vector3 avgTan = Vector3.zero;
            Vector3 avgTan2 = Vector3.zero;
            Vector3 avgNormal = Vector3.zero;
            float avgSize = 0f;
            Color avgColor = Color.clear;

            for (int i = 0; i < selectedPoints.Count; i++)
            {
                avgPos += points[selectedPoints[i]].position;
                avgNormal += points[selectedPoints[i]].normal;
                avgSize += points[selectedPoints[i]].size;
                avgTan += points[selectedPoints[i]].tangent;
                avgTan2 += points[selectedPoints[i]].tangent2;
                avgColor += points[selectedPoints[i]].color;
            }

            avgPos /= selectedPoints.Count;
            avgTan /= selectedPoints.Count;
            avgTan2 /= selectedPoints.Count;
            avgSize /= selectedPoints.Count;
            avgColor /= selectedPoints.Count;
            avgNormal.Normalize();

            SplinePoint avgPoint = new SplinePoint(avgPos, avgPos);
            avgPoint.tangent = avgTan;
            avgPoint.tangent2 = avgTan2;
            avgPoint.size = avgSize;
            avgPoint.color = avgColor;
            avgPoint.type = points[selectedPoints[0]].type;
            SplinePoint.Type lastType = avgPoint.type;

            avgPoint.normal = avgNormal;
            EditorGUI.BeginChangeCheck();
            space = (Space)EditorGUILayout.EnumPopup("Coordinate Space", space);
            if (splineType == Spline.Type.Bezier)
            {
                if (is2D)
                {
                    avgPoint.SetTangentPosition(TransformedPositionField2D("Tangent 1", avgPoint.tangent));
                    avgPoint.SetTangent2Position(TransformedPositionField2D("Tangent 2", avgPoint.tangent2));
                }
                else
                {
                    avgPoint.SetTangentPosition(TransformedPositionField("Tangent 1", avgPoint.tangent));
                    avgPoint.SetTangent2Position(TransformedPositionField("Tangent 2", avgPoint.tangent2));
                }
            }
            if (is2D) avgPoint.SetPosition(TransformedPositionField2D("Position", avgPoint.position));
            else avgPoint.SetPosition(TransformedPositionField("Position", avgPoint.position));
            if (!is2D)
            {
                if (space == Space.Local) avgPoint.normal = _matrix.inverse.MultiplyVector(avgPoint.normal);
                avgPoint.normal = TransformedPositionField("Normal", avgPoint.normal);
                if (space == Space.Local) avgPoint.normal = _matrix.MultiplyVector(avgPoint.normal);
            }
            avgPoint.size = EditorGUILayout.FloatField("Size", avgPoint.size);
            avgPoint.color = EditorGUILayout.ColorField("Color", avgPoint.color);
            if (splineType == Spline.Type.Bezier)
            {
                avgPoint.type = (SplinePoint.Type)EditorGUILayout.EnumPopup("Point Type", avgPoint.type);
            }

            if (!EditorGUI.EndChangeCheck()) return;
            RecordUndo("Edit Points");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                points[selectedPoints[i]].SetPosition(GetChangedVector(avgPos, avgPoint.position, points[selectedPoints[i]].position));
                points[selectedPoints[i]].normal = GetChangedVector(avgNormal, avgPoint.normal, points[selectedPoints[i]].normal);

                if (splineType == Spline.Type.Bezier)
                {
                    points[selectedPoints[i]].SetTangentPosition(GetChangedVector(avgTan, avgPoint.tangent, points[selectedPoints[i]].tangent));
                    points[selectedPoints[i]].SetTangent2Position(GetChangedVector(avgTan2, avgPoint.tangent2, points[selectedPoints[i]].tangent2));
                }
                if (avgPoint.size != avgSize) points[selectedPoints[i]].size = avgPoint.size;
                if (avgColor != avgPoint.color) points[selectedPoints[i]].color = avgPoint.color;
                if (lastType != avgPoint.type) points[selectedPoints[i]].type = avgPoint.type;
            }
        }

        Vector3 GetChangedVector(Vector3 oldVector, Vector3 newVector, Vector3 original)
        {
            if (!Mathf.Approximately(oldVector.x, newVector.x)) original.x = newVector.x;
            if (!Mathf.Approximately(oldVector.y, newVector.y)) original.y = newVector.y;
            if (!Mathf.Approximately(oldVector.z, newVector.z)) original.z = newVector.z;
            return original;
        }

        Vector3 TransformedPositionField(string title, Vector3 worldPoint)
        {
            Vector3 pos = worldPoint;
            if (space == Space.Local) pos = _matrix.inverse.MultiplyPoint3x4(worldPoint);
            pos = EditorGUILayout.Vector3Field(title, pos);
            if (space == Space.Local) pos = _matrix.MultiplyPoint3x4(pos);
            return pos;
        }

        Vector2 TransformedPositionField2D(string title, Vector3 worldPoint)
        {
            Vector2 pos = worldPoint;
            if (space == Space.Local) pos = _matrix.inverse.MultiplyPoint3x4(worldPoint);
            pos = EditorGUILayout.Vector2Field(title, pos);
            if (space == Space.Local) pos = _matrix.MultiplyPoint3x4(pos);
            return pos;
        }

        public void FlatSelection(int axis)
        {
            Vector3 avg = Vector3.zero;
            bool flatTangent = false;
            bool flatPosition = true;
            if (splineType == Spline.Type.Bezier)
            {
                switch (EditorUtility.DisplayDialogComplex("Flat Bezier", "How do you want to flat the selected Bezier points?", "Points Only", "Tangens Only", "Everything"))
                {
                    case 0: flatTangent = false; flatPosition = true; break;
                    case 1: flatTangent = true; flatPosition = false; break;
                    case 2: flatTangent = true; flatPosition = true; break;
                }
            }
            RecordUndo("Flat Selection");
            if (flatPosition)
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    avg += points[selectedPoints[i]].position;
                }
                avg /= selectedPoints.Count;
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: pos.x = avg.x; points[selectedPoints[i]].normal.x = 0f; break;
                        case 1: pos.y = avg.y; points[selectedPoints[i]].normal.y = 0f; break;
                        case 2: pos.z = avg.z; points[selectedPoints[i]].normal.z = 0f; break;
                    }
                    points[selectedPoints[i]].normal.Normalize();
                    if (points[selectedPoints[i]].normal == Vector3.zero) points[selectedPoints[i]].normal = Vector3.up;
                    points[selectedPoints[i]].SetPosition(pos);
                    if (flatTangent)
                    {
                        Vector3 tan = points[selectedPoints[i]].tangent;
                        Vector3 tan2 = points[selectedPoints[i]].tangent2;
                        switch (axis)
                        {
                            case 0: tan.x = avg.x; tan2.x = avg.x; break;
                            case 1: tan.y = avg.y; tan2.y = avg.y; break;
                            case 2: tan.z = avg.z; tan2.z = avg.z; break;
                        }
                        points[selectedPoints[i]].SetTangentPosition(tan);
                        points[selectedPoints[i]].SetTangent2Position(tan2);
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    Vector3 tan = points[selectedPoints[i]].tangent;
                    Vector3 tan2 = points[selectedPoints[i]].tangent2;
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: tan.x = pos.x; tan2.x = pos.x; break;
                        case 1: tan.y = pos.y; tan2.y = pos.y; break;
                        case 2: tan.z = pos.z; tan2.z = pos.z; break;
                    }
                    points[selectedPoints[i]].SetTangentPosition(tan);
                    points[selectedPoints[i]].SetTangent2Position(tan2);
                }
            }
            ResetCurrentModule();
        }

        public void MirrorSelection(int axis)
        {
            bool mirrorTangents = false;
            if (splineType == Spline.Type.Bezier)
            {
                if (EditorUtility.DisplayDialog("Mirror tangents", "Do you want to mirror the tangents too ?", "Yes", "No")) mirrorTangents = true;
            }
            float min = 0f, max = 0f;
            switch (axis)
            {
                case 0: min = max = points[selectedPoints[0]].position.x; break;
                case 1: min = max = points[selectedPoints[0]].position.y; break;
                case 2: min = max = points[selectedPoints[0]].position.z; break;
            }
            RecordUndo("Mirror Selection");
            if (mirrorTangents)
            {
                float value = 0f;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[0]].tangent.x; break;
                    case 1: value = points[selectedPoints[0]].tangent.y; break;
                    case 2: value = points[selectedPoints[0]].tangent.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[0]].tangent2.x; break;
                    case 1: value = points[selectedPoints[0]].tangent2.y; break;
                    case 2: value = points[selectedPoints[0]].tangent2.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
            }
            for (int i = 1; i < selectedPoints.Count; i++)
            {
                float value = 0f;
                switch (axis)
                {
                    case 0: value = points[selectedPoints[i]].position.x; break;
                    case 1: value = points[selectedPoints[i]].position.y; break;
                    case 2: value = points[selectedPoints[i]].position.z; break;
                }
                if (value < min) min = value;
                if (value > max) max = value;
                if (mirrorTangents)
                {
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent.x; break;
                        case 1: value = points[selectedPoints[i]].tangent.y; break;
                        case 2: value = points[selectedPoints[i]].tangent.z; break;
                    }
                    if (value < min) min = value;
                    if (value > max) max = value;
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent2.x; break;
                        case 1: value = points[selectedPoints[i]].tangent2.y; break;
                        case 2: value = points[selectedPoints[i]].tangent2.z; break;
                    }
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }

            for (int i = 0; i < selectedPoints.Count; i++)
            {
                float value = 0f;
                if (mirrorTangents)
                {
                    //Point position
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].position.x; break;
                        case 1: value = points[selectedPoints[i]].position.y; break;
                        case 2: value = points[selectedPoints[i]].position.z; break;
                    }
                    float percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: points[selectedPoints[i]].position.x = value; break;
                        case 1: points[selectedPoints[i]].position.y = value; break;
                        case 2: points[selectedPoints[i]].position.z = value; break;
                    }
                    //Tangent 1
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent.x; break;
                        case 1: value = points[selectedPoints[i]].tangent.y; break;
                        case 2: value = points[selectedPoints[i]].tangent.z; break;
                    }
                    percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: points[selectedPoints[i]].tangent.x = value; break;
                        case 1: points[selectedPoints[i]].tangent.y = value; break;
                        case 2: points[selectedPoints[i]].tangent.z = value; break;
                    }
                    //Tangent 2
                    switch (axis)
                    {
                        case 0: value = points[selectedPoints[i]].tangent2.x; break;
                        case 1: value = points[selectedPoints[i]].tangent2.y; break;
                        case 2: value = points[selectedPoints[i]].tangent2.z; break;
                    }
                    percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: points[selectedPoints[i]].tangent2.x = value; break;
                        case 1: points[selectedPoints[i]].tangent2.y = value; break;
                        case 2: points[selectedPoints[i]].tangent2.z = value; break;
                    }
                }
                else
                {
                    Vector3 pos = points[selectedPoints[i]].position;
                    switch (axis)
                    {
                        case 0: value = pos.x; break;
                        case 1: value = pos.y; break;
                        case 2: value = pos.z; break;
                    }
                    float percent = Mathf.InverseLerp(min, max, value);
                    value = Mathf.Lerp(max, min, percent);
                    switch (axis)
                    {
                        case 0: pos.x = value; break;
                        case 1: pos.y = value; break;
                        case 2: pos.z = value; break;
                    }
                    points[selectedPoints[i]].SetPosition(pos);
                }
                //Normal
                switch (axis)
                {
                    case 0: points[selectedPoints[i]].normal.x *= -1f; break;
                    case 1: points[selectedPoints[i]].normal.y *= -1f; break;
                    case 2: points[selectedPoints[i]].normal.z *= -1f; break;
                }
                points[selectedPoints[i]].normal.Normalize();
            }
            ResetCurrentModule();
        }

        public void DistributeEvenly()
        {
            if (selectedPoints.Count < 3) return;
            RecordUndo("Distribute Evenly");
            int min = points.Length-1, max = 0;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (selectedPoints[i] < min) min = selectedPoints[i];
                if (selectedPoints[i] > max) max = selectedPoints[i];
            }
            double minPercent = (double)min / (points.Length - 1);
            double maxPercent = (double)max / (points.Length - 1);
            float length = calculateLength(minPercent, maxPercent);
            float step = length / (max - min);
            SplineSample evalResult = new SplineSample();
            evaluate(minPercent, evalResult);
            for (int i = min + 1; i < max; i++)
            {
                double percent = travel(evalResult.percent, step, Spline.Direction.Forward);
                evaluate(percent, evalResult);
                points[i].SetPosition(evalResult.position);
            }
            ResetCurrentModule();
        }

        public void AutoTangents()
        {
            RecordUndo("Auto Tangents");
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                int index = selectedPoints[i];
                Vector3 prevPos = points[index].position, forwardPos = points[index].position;
                if(index == 0 && points.Length > 1)
                {
                    prevPos = points[0].position + (points[0].position - points[1].position);
                } else prevPos = points[index - 1].position;
                if (index == points.Length-1 && points.Length > 1)
                {
                    forwardPos = points[points.Length-1].position + (points[points.Length - 1].position - points[points.Length - 2].position);
                }
                else forwardPos = points[index + 1].position;
                Vector3 delta = (forwardPos - prevPos) / 2f;
                points[index].tangent = points[index].position - delta / 3f;
                points[index].tangent2 = points[index].position + delta / 3f;
            }
            ResetCurrentModule();
        }

        protected void ResetCurrentModule()
        {
            if (module < 0 || module >= modules.Length) return;
            modules[module].Reset();
        }

        public class PointOperation
        {
            public string name = "";
            public System.Action action;
        }
    }
}
