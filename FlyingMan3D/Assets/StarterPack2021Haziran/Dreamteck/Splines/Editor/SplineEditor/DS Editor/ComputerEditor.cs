namespace Dreamteck.Splines.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class ComputerEditor : SplineEditorBase
    {
        private SplineComputer spline = null;
        private SplineComputer[] splines = new SplineComputer[0];
        private SerializedObject serializedObject;
        private bool pathToolsFoldout = false, interpolationFoldout = false;
        public bool drawComputer = true;
        public bool drawConnectedComputers = true;
        private DreamteckSplinesEditor pathEditor;
        private int operation = -1, module = -1, transformTool = 1;
        private ComputerEditorModule[] modules = new ComputerEditorModule[0];
        private Dreamteck.Editor.Toolbar utilityToolbar;
        private Dreamteck.Editor.Toolbar operationsToolbar;
        private Dreamteck.Editor.Toolbar transformToolbar;

        private SerializedProperty splineProperty;
        private SerializedProperty sampleRate;
        private SerializedProperty type;
        private SerializedProperty linearAverageDirection;
        private SerializedProperty space;
        private SerializedProperty sampleMode;
        private SerializedProperty optimizeAngleThreshold;
        private SerializedProperty updateMode;
        private SerializedProperty rebuildOnAwake;
        private SerializedProperty multithreaded;
        private SerializedProperty customNormalInterpolation;
        private SerializedProperty customValueInterpolation;


        public ComputerEditor(SplineComputer[] splines, SerializedObject serializedObject, DreamteckSplinesEditor pathEditor) : base()
        {
            spline = splines[0];
            this.splines = splines;
            this.pathEditor = pathEditor;
            this.serializedObject = serializedObject;

            splineProperty = serializedObject.FindProperty("spline");
            sampleRate = serializedObject.FindProperty("spline").FindPropertyRelative("sampleRate");
            type = serializedObject.FindProperty("spline").FindPropertyRelative("type");
            linearAverageDirection = splineProperty.FindPropertyRelative("linearAverageDirection");
            space = serializedObject.FindProperty("_space");
            sampleMode = serializedObject.FindProperty("_sampleMode");
            optimizeAngleThreshold = serializedObject.FindProperty("_optimizeAngleThreshold");
            updateMode = serializedObject.FindProperty("updateMode");
            rebuildOnAwake = serializedObject.FindProperty("rebuildOnAwake");
            multithreaded = serializedObject.FindProperty("multithreaded");
            customNormalInterpolation = splineProperty.FindPropertyRelative("customNormalInterpolation");
            customValueInterpolation = splineProperty.FindPropertyRelative("customValueInterpolation");


            modules = new ComputerEditorModule[2];
            modules[0] = new ComputerMergeModule(spline);
            modules[1] = new ComputerSplitModule(spline);
            GUIContent[] utilityContents = new GUIContent[modules.Length], utilityContentsSelected = new GUIContent[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                utilityContents[i] = modules[i].GetIconOff();
                utilityContentsSelected[i] = modules[i].GetIconOn();
                modules[i].undoHandler += OnRecordUndo;
                modules[i].repaintHandler += OnRepaint;
            }
            utilityToolbar = new Dreamteck.Editor.Toolbar(utilityContents, utilityContentsSelected, 35f);
            utilityToolbar.newLine = false;


            int index = 0;
            GUIContent[] transformContents = new GUIContent[4], transformContentsSelected = new GUIContent[4];
            transformContents[index] = new GUIContent("OFF");
            transformContentsSelected[index++] = new GUIContent("OFF");

            transformContents[index] = EditorGUIUtility.IconContent("MoveTool");
            transformContentsSelected[index++] = EditorGUIUtility.IconContent("MoveTool On");

            transformContents[index] = EditorGUIUtility.IconContent("RotateTool");
            transformContentsSelected[index++] = EditorGUIUtility.IconContent("RotateTool On");

            transformContents[index] = EditorGUIUtility.IconContent("ScaleTool");
            transformContentsSelected[index] = EditorGUIUtility.IconContent("ScaleTool On");

            transformToolbar = new Dreamteck.Editor.Toolbar(transformContents, transformContentsSelected, 35f);
            transformToolbar.newLine = false;

            index = 0;
            GUIContent[] operationContents = new GUIContent[3], operationContentsSelected = new GUIContent[3];
            for (int i = 0; i < operationContents.Length; i++)
            {
                operationContents[i] = new GUIContent("");
                operationContentsSelected[i] = new GUIContent("");
            }
            operationsToolbar = new Dreamteck.Editor.Toolbar(operationContents, operationContentsSelected, 64f);
            operationsToolbar.newLine = false;
        }

        void OnRecordUndo(string title)
        {
            if (undoHandler != null) undoHandler(title);
        }

        void OnRepaint()
        {
            if (repaintHandler != null) repaintHandler();
        }

        protected override void Load()
        {
            base.Load();
            pathToolsFoldout = LoadBool("DreamteckSplinesEditor.pathToolsFoldout", false);
            interpolationFoldout = LoadBool("DreamteckSplinesEditor.interpolationFoldout", false);
            transformTool = LoadInt("DreamteckSplinesEditor.transformTool", 0);
        }

        protected override void Save()
        {
            base.Save();
            SaveBool("DreamteckSplinesEditor.pathToolsFoldout", pathToolsFoldout);
            SaveBool("DreamteckSplinesEditor.interpolationFoldout", interpolationFoldout);
            SaveInt("DreamteckSplinesEditor.transformTool", transformTool);
        }

        public override void Destroy()
        {
            base.Destroy();
            for (int i = 0; i < modules.Length; i++) modules[i].Deselect();
        }

        public override void DrawInspector()
        {
            base.DrawInspector();
            if (spline == null) return;
            SplineEditorGUI.SetHighlightColors(SplinePrefs.highlightColor, SplinePrefs.highlightContentColor);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            operationsToolbar.SetContent(0, new GUIContent(spline.isClosed ? "Break" : "Close"));
            operationsToolbar.SetContent(1, new GUIContent("Reverse"));
            operationsToolbar.SetContent(2, new GUIContent(spline.is2D ? "3D Mode" : "2D Mode"));
            operationsToolbar.Draw(ref operation);
            if (EditorGUI.EndChangeCheck()) PerformOperation();
            EditorGUI.BeginChangeCheck();
            if (splines.Length == 1)
            {
                int mod = module;
                utilityToolbar.Draw(ref mod);
                if (EditorGUI.EndChangeCheck()) ToggleModule(mod);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (module >= 0 && module < modules.Length) modules[module].DrawInspector();
            EditorGUILayout.Space();
            DreamteckEditorGUI.DrawSeparator();

            EditorGUILayout.Space();
            
            serializedObject.Update();



            EditorGUI.BeginChangeCheck();
            Spline.Type lastType = (Spline.Type)type.intValue;
            EditorGUILayout.PropertyField(type);
            if(lastType == Spline.Type.CatmullRom && type.intValue == (int)Spline.Type.Bezier)
            {
                if(EditorUtility.DisplayDialog("Hermite to Bezier", "Would you like to retain the Catmull Rom shape in Bezier mode?", "Yes", "No"))
                {
                    for (int i = 0; i < splines.Length; i++) splines[i].CatToBezierTangents();
                    
                    serializedObject.Update();
                    pathEditor.Refresh();
                }
            }
            if(spline.type == Spline.Type.Linear) EditorGUILayout.PropertyField(linearAverageDirection);
            int lastSpace = space.intValue;
            EditorGUILayout.PropertyField(space, new GUIContent("Space"));
            EditorGUILayout.PropertyField(sampleMode, new GUIContent("Sample Mode"));
            if (sampleMode.intValue == (int)SplineComputer.SampleMode.Optimized) EditorGUILayout.PropertyField(optimizeAngleThreshold);
            EditorGUILayout.PropertyField(updateMode);
            if (updateMode.intValue == (int)SplineComputer.UpdateMode.None && Application.isPlaying)
            {
                if (GUILayout.Button("Rebuild"))
                {
                    for (int i = 0; i < splines.Length; i++) splines[i].RebuildImmediate(true, true);
                }
            }
            if (spline.type != Spline.Type.Linear) EditorGUILayout.PropertyField(sampleRate, new GUIContent("Sample Rate"));
            EditorGUILayout.PropertyField(rebuildOnAwake);
            EditorGUILayout.PropertyField(multithreaded);

            EditorGUI.indentLevel++;
            bool curveUpdate = false;
            interpolationFoldout = EditorGUILayout.Foldout(interpolationFoldout, "Point Value Interpolation");
            if (interpolationFoldout)
            {
                if (customValueInterpolation.animationCurveValue == null || customValueInterpolation.animationCurveValue.keys.Length == 0)
                {
                    if (GUILayout.Button("Size & Color Interpolation"))
                    {
                        AnimationCurve curve = new AnimationCurve();
                        curve.AddKey(new Keyframe(0, 0, 0, 0));
                        curve.AddKey(new Keyframe(1, 1, 0, 0));
                        for (int i = 0; i < splines.Length; i++) splines[i].customValueInterpolation = curve;
                        serializedObject.Update();
                        curveUpdate = true;
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(customValueInterpolation, new GUIContent("Size & Color Interpolation"));
                    if (GUILayout.Button("x", GUILayout.MaxWidth(25)))
                    {
                        customValueInterpolation.animationCurveValue = null;
                        for (int i = 0; i < splines.Length; i++) splines[i].customValueInterpolation = null;
                        serializedObject.Update();
                        curveUpdate = true;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (customNormalInterpolation.animationCurveValue == null || customNormalInterpolation.animationCurveValue.keys.Length == 0)
                {
                    if (GUILayout.Button("Normal Interpolation"))
                    {
                        AnimationCurve curve = new AnimationCurve();
                        curve.AddKey(new Keyframe(0, 0));
                        curve.AddKey(new Keyframe(1, 1));
                        for (int i = 0; i < splines.Length; i++) splines[i].customNormalInterpolation = curve;
                        serializedObject.Update();
                        curveUpdate = true;
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(customNormalInterpolation, new GUIContent("Normal Interpolation"));
                    if (GUILayout.Button("x", GUILayout.MaxWidth(25)))
                    {
                        customNormalInterpolation.animationCurveValue = null;
                        for (int i = 0; i < splines.Length; i++) splines[i].customNormalInterpolation = null;
                        serializedObject.Update();
                        curveUpdate = true;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck() || curveUpdate)
            {
                if (sampleRate.intValue < 2) sampleRate.intValue = 2;
                if (lastSpace != space.intValue)
                {
                    for (int i = 0; i < splines.Length; i++) splines[i].space = (SplineComputer.Space)space.intValue;
                    serializedObject.Update();
                    if (splines.Length == 1) pathEditor.Refresh();
                }

                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < splines.Length; i++)
                {
                    splines[i].Rebuild(true);
                }
            }

            
            if (pathEditor.currentModule != null) transformTool = 0;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Edit Transform");
            GUILayout.FlexibleSpace();
            int lastTool = transformTool;
            transformToolbar.Draw(ref transformTool);
            if (lastTool != transformTool && transformTool > 0) pathEditor.UntoggleCurrentModule();
            EditorGUILayout.EndHorizontal();
        }

        void PerformOperation()
        {
            switch (operation)
            {
                case 0:
                    if (spline.isClosed) BreakSpline();
                    else CloseSpline();
                    operation = -1;
                    break;
                case 1:
                    ReversePointOrder();
                    operation = -1;
                    break;
                case 2:
                    spline.is2D = !spline.is2D;
                    operation = -1;
                    break;
            }
        }

        void ToggleModule(int index)
        {
            if (module >= 0 && module < modules.Length) modules[module].Deselect();
            if (module == index) index = -1;
            module = index;
            if (module >= 0 && module < modules.Length) modules[module].Select();
        }

        public void BreakSpline()
        {
            RecordUndo("Break path");
            if (splines.Length == 1 && pathEditor.selectedPoints.Count == 1) spline.Break(pathEditor.selectedPoints[0]);
            else
            {
                for (int i = 0; i < splines.Length; i++) splines[i].Break();
            }
        }

        public void CloseSpline()
        {
            RecordUndo("Close path");
            for (int i = 0; i < splines.Length; i++)
            {
                splines[i].Close();
            }
        }

        void ReversePointOrder()
        {
            for (int i = 0; i < splines.Length; i++)
            {
                ReversePointOrder(splines[i]);
            }
        }

        void ReversePointOrder(SplineComputer spline)
        {
            SplinePoint[] points = spline.GetPoints();
            for (int i = 0; i < Mathf.FloorToInt(points.Length / 2); i++)
            {
                SplinePoint temp = points[i];
                points[i] = points[(points.Length - 1) - i];
                Vector3 tempTan = points[i].tangent;
                points[i].tangent = points[i].tangent2;
                points[i].tangent2 = tempTan;
                int opposideIndex = (points.Length - 1) - i;
                points[opposideIndex] = temp;
                tempTan = points[opposideIndex].tangent;
                points[opposideIndex].tangent = points[opposideIndex].tangent2;
                points[opposideIndex].tangent2 = tempTan;
            }
            if (points.Length % 2 != 0)
            {
                Vector3 tempTan = points[Mathf.CeilToInt(points.Length / 2)].tangent;
                points[Mathf.CeilToInt(points.Length / 2)].tangent = points[Mathf.CeilToInt(points.Length / 2)].tangent2;
                points[Mathf.CeilToInt(points.Length / 2)].tangent2 = tempTan;
            }
            spline.SetPoints(points);
        }

        public override void DrawScene(SceneView current)
        {
            base.DrawScene(current);
            if (drawComputer)
            {
                for (int i = 0; i < splines.Length; i++)
                {
                    DSSplineDrawer.DrawSplineComputer(splines[i]);
                }

            }
            if (drawConnectedComputers)
            {
                for (int i = 0; i < splines.Length; i++)
                {
                    List<SplineComputer> computers = splines[i].GetConnectedComputers();
                    for (int j = 1; j < computers.Count; j++)
                    {
                        DSSplineDrawer.DrawSplineComputer(computers[j], 0.0, 1.0, 0.5f);
                    }
                }
            }



            if (pathEditor.currentModule == null)
            {
                switch (transformTool)
                {
                    case 1:
                        for (int i = 0; i < splines.Length; i++)
                        {
                            Vector3 position = splines[i].transform.position;
                            position = Handles.PositionHandle(position, splines[i].transform.rotation);
                            if (position != splines[i].transform.position)
                            {
                                RecordUndo("Move spline computer");
                                Undo.RecordObject(splines[i].transform, "Move spline computer");
                                splines[i].transform.position = position;
                                splines[i].SetPoints(pathEditor.points);
                                pathEditor.Refresh();
                            }
                        }
                        break;
                    case 2:
                        for (int i = 0; i < splines.Length; i++)
                        {
                            Quaternion rotation = splines[i].transform.rotation;
                            rotation = Handles.RotationHandle(rotation, splines[i].transform.position);
                            if (rotation != splines[i].transform.rotation)
                            {
                                RecordUndo("Rotate spline computer");
                                Undo.RecordObject(splines[i].transform, "Rotate spline computer");
                                splines[i].transform.rotation = rotation;
                                splines[i].SetPoints(pathEditor.points);
                                pathEditor.Refresh();
                            }
                        }
                        break;
                    case 3:
                        for (int i = 0; i < splines.Length; i++)
                        {
                            Vector3 scale = splines[i].transform.localScale;
                            scale = Handles.ScaleHandle(scale, splines[i].transform.position, splines[i].transform.rotation,
                                HandleUtility.GetHandleSize(splines[i].transform.position));
                            if (scale != splines[i].transform.localScale)
                            {
                                RecordUndo("Scale spline computer");
                                Undo.RecordObject(splines[i].transform, "Scale spline computer");
                                splines[i].transform.localScale = scale;
                                splines[i].SetPoints(pathEditor.points);
                                pathEditor.Refresh();
                            }
                        }
                        break;
                }
                if (transformTool > 0)
                {
                    for (int i = 0; i < splines.Length; i++)
                    {
                        Vector2 screenPosition = HandleUtility.WorldToGUIPoint(splines[i].transform.position);
                        screenPosition.y += 20f;
                        Handles.BeginGUI();
                        DreamteckEditorGUI.Label(new Rect(screenPosition.x - 120 + splines[i].name.Length * 4, screenPosition.y, 120, 25), splines[i].name);
                        Handles.EndGUI();
                    }

                }
            }
            if (module >= 0 && module < modules.Length) modules[module].DrawScene();
        }
    }
}
