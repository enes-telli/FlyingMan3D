namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections.Generic;
    using UnityEditor;
    using Dreamteck.Editor;

    [CustomEditor(typeof(SplineComputer), true)]
    [CanEditMultipleObjects]
    public partial class SplineComputerEditor : Editor 
    {
        private List<int> selectedPoints = new List<int>();

        public int[] pointSelection
        {
            get
            {
                return selectedPoints.ToArray();
            }
        }
        public bool mouseLeft = false;
        public bool mouseRight = false;
        public bool mouseLeftDown = false;
        public bool mouseRightDown = false;
        public bool mouseLeftUp = false;
        public bool mouserightUp = false;
        public bool control = false;
        public bool shift = false;
        public bool alt = false;
        public SplineComputer spline;
        public SplineComputer[] splines = new SplineComputer[0];


        protected bool closedOnMirror = false;

        
        public static bool hold = false;

        DreamteckSplinesEditor pathEditor;
        ComputerEditor computerEditor;
        SplineTriggersEditor triggersEditor;
        SplineDebugEditor debugEditor;

        public int selectedPointsCount
        {
            get { return selectedPoints.Count; }
            set { }
        }


        [MenuItem("GameObject/3D Object/Spline Computer")]
        private static void NewEmptySpline()
        {
            int count = GameObject.FindObjectsOfType<SplineComputer>().Length;
            string objName = "Spline";
            if (count > 0) objName += " " + count;
            GameObject obj = new GameObject(objName);
            obj.AddComponent<SplineComputer>();
            if (Selection.activeGameObject != null)
            {
                if (EditorUtility.DisplayDialog("Make child?", "Do you want to make the new spline a child of " + Selection.activeGameObject.name + "?", "Yes", "No"))
                {
                    obj.transform.parent = Selection.activeGameObject.transform;
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                }
            }
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/3D Object/Spline Node")]
        private static void NewSplineNode()
        {
            int count = Object.FindObjectsOfType<Node>().Length;
            string objName = "Node";
            if (count > 0) objName += " " + count;
            GameObject obj = new GameObject(objName);
            obj.AddComponent<Node>();
            if(Selection.activeGameObject != null)
            {
                if(EditorUtility.DisplayDialog("Make child?", "Do you want to make the new node a child of " + Selection.activeGameObject.name + "?", "Yes", "No"))
                {
                    obj.transform.parent = Selection.activeGameObject.transform;
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                }
            }
            Selection.activeGameObject = obj;
        }

        public void UndoRedoPerformed()
        {
            pathEditor.points = spline.GetPoints();
            pathEditor.UndoRedoPerformed();
            spline.EditorUpdateConnectedNodes();
            spline.Rebuild();
        }

        void OnEnable()
        {
            splines = new SplineComputer[targets.Length];
            for (int i = 0; i < splines.Length; i++)
            {
                splines[i] = (SplineComputer)targets[i];
                splines[i].EditorAwake();
                if (splines[i].alwaysDraw)
                {
                    DSSplineDrawer.RegisterComputer(splines[i]);
                }
            }
            spline = splines[0];
            InitializeSplineEditor();
            InitializeComputerEditor();
            debugEditor = new SplineDebugEditor(spline);
            debugEditor.undoHandler += RecordUndo;
            debugEditor.repaintHandler += OnRepaint;
            triggersEditor = new SplineTriggersEditor(spline);
            triggersEditor.undoHandler += RecordUndo;
            triggersEditor.repaintHandler += OnRepaint;
            hold = false;
#if UNITY_2019_1_OR_NEWER
            SceneView.beforeSceneGui += BeforeSceneGUI;
#else
            SceneView.onSceneGUIDelegate += BeforeSceneGUI;
#endif
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void BeforeSceneGUI(SceneView current)
        {
            pathEditor.BeforeSceneGUI(current);
        }

        void InitializeSplineEditor()
        {
            pathEditor = new DreamteckSplinesEditor(spline, "DreamteckSplines");
            pathEditor.undoHandler = RecordUndo;
            pathEditor.repaintHandler = OnRepaint;
            pathEditor.space = (SplineEditor.Space)SplinePrefs.pointEditSpace;
        }

        void InitializeComputerEditor()
        {
            computerEditor = new ComputerEditor(splines, serializedObject, pathEditor);
            computerEditor.undoHandler = RecordUndo;
            computerEditor.repaintHandler = OnRepaint;
        }

        void RecordUndo(string title)
        {
            for (int i = 0; i < splines.Length; i++)
            {
                Undo.RecordObject(splines[i], title);
            }
        }

        void OnRepaint()
        {
            SceneView.RepaintAll();
            Repaint();
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
#if UNITY_2019_1_OR_NEWER
            SceneView.beforeSceneGui -= BeforeSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= BeforeSceneGUI;
#endif
            pathEditor.Destroy();
            computerEditor.Destroy();
            debugEditor.Destroy();
            triggersEditor.Destroy();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            spline = (SplineComputer)target;
            Undo.RecordObject(spline, "Edit Points");

            if (splines.Length == 1)
            {
                SplineEditorGUI.BeginContainerBox(ref pathEditor.open, "Edit");
                if (pathEditor.open)
                {
                    SplineEditor.Space lastSpace = pathEditor.space;
                    pathEditor.DrawInspector();
                    if(lastSpace != pathEditor.space)
                    {
                        SplinePrefs.pointEditSpace = (SplineComputer.Space)pathEditor.space;
                        SplinePrefs.SavePrefs();
                    }
                }
                else if (pathEditor.lastEditorTool != Tool.None && Tools.current == Tool.None) Tools.current = pathEditor.lastEditorTool;
                SplineEditorGUI.EndContainerBox();
            }

            SplineEditorGUI.BeginContainerBox(ref computerEditor.open, "Spline Computer");
            if (computerEditor.open) computerEditor.DrawInspector();
            SplineEditorGUI.EndContainerBox();

            if (splines.Length == 1)
            {
                SplineEditorGUI.BeginContainerBox(ref triggersEditor.open, "Triggers");
                if (triggersEditor.open) triggersEditor.DrawInspector();
                SplineEditorGUI.EndContainerBox();

                SplineEditorGUI.BeginContainerBox(ref debugEditor.open, "Editor Properties");
                if (debugEditor.open) debugEditor.DrawInspector();
                SplineEditorGUI.EndContainerBox();
            }

            if (GUI.changed)
            {
               if (spline.isClosed) pathEditor.points[pathEditor.points.Length - 1] = pathEditor.points[0];
                EditorUtility.SetDirty(spline);
            }
        }

        

        public bool IsPointSelected(int index)
        {
            return selectedPoints.Contains(index);
        }

        private void OnSceneGUI()
        {
            spline = (SplineComputer)target;
            SceneView currentSceneView = SceneView.currentDrawingSceneView;
            debugEditor.DrawScene(currentSceneView);
            computerEditor.drawComputer = !(pathEditor.currentModule is CreatePointModule);
            computerEditor.DrawScene(currentSceneView);
            if (splines.Length == 1 && triggersEditor.open) triggersEditor.DrawScene(currentSceneView);
            if (splines.Length == 1 && pathEditor.open) pathEditor.DrawScene(currentSceneView);
        }
    }
}
