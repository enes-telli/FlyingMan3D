namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;

    public class PointNormalModule : PointModule
    {
        public enum NormalMode { Auto, Free }
        public NormalMode normalMode = NormalMode.Auto;
        SplineSample evalResult = new SplineSample();
        public PointNormalModule(SplineEditor editor) : base(editor)
        {

        }

        public override GUIContent GetIconOff()
        {
            return IconContent("N", "normal", "Set Point Normals");
        }

        public override GUIContent GetIconOn()
        {
            return IconContent("N", "normal_on", "Set Point Normals");
        }

        void SetNormals(int mode)
        {
            mode--;
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < selectedPoints.Count; i++) avg += points[selectedPoints[i]].position;
            if (selectedPoints.Count > 1) avg /= selectedPoints.Count;
            Camera editorCamera = SceneView.lastActiveSceneView.camera;

            for (int i = 0; i < selectedPoints.Count; i++)
            {
                switch (mode)
                {
                    case 0: points[selectedPoints[i]].normal *= -1; break;
                    case 1: points[selectedPoints[i]].normal = Vector3.Normalize(editorCamera.transform.position - points[selectedPoints[i]].position); break;
                    case 2: points[selectedPoints[i]].normal = editorCamera.transform.forward; break;
                    case 3: points[selectedPoints[i]].normal = CalculatePointNormal(points, selectedPoints[i], isClosed); break;
                    case 4: points[selectedPoints[i]].normal = Vector3.left; break;
                    case 5: points[selectedPoints[i]].normal = Vector3.right; break;
                    case 6: points[selectedPoints[i]].normal = Vector3.up; break;
                    case 7: points[selectedPoints[i]].normal = Vector3.down; break;
                    case 8: points[selectedPoints[i]].normal = Vector3.forward; break;
                    case 9: points[selectedPoints[i]].normal = Vector3.back; break;
                    case 10: points[selectedPoints[i]].normal = Vector3.Normalize(avg - points[selectedPoints[i]].position); break;
                    case 11:
                        SplineSample result = new SplineSample();
                        editor.evaluateAtPoint(selectedPoints[i], result);
                        points[selectedPoints[i]].normal = Vector3.Cross(result.forward, result.right).normalized;
                        break;
                }
            }
        }

        public static Vector3 CalculatePointNormal(SplinePoint[] points, int index, bool isClosed)
        {
            if (points.Length < 3)
            {
                Debug.Log("Spline needs to have at least 3 control points in order to calculate normals");
                return Vector3.zero;
            }
            Vector3 side1 = Vector3.zero;
            Vector3 side2 = Vector3.zero;
            if (index == 0)
            {
                if (isClosed)
                {
                    side1 = points[index].position - points[index + 1].position;
                    side2 = points[index].position - points[points.Length - 2].position;
                }
                else
                {
                    side1 = points[0].position - points[1].position;
                    side2 = points[0].position - points[2].position;
                }
            }
            else if (index == points.Length - 1)
            {
                side1 = points[points.Length - 1].position - points[points.Length - 3].position;
                side2 = points[points.Length - 1].position - points[points.Length - 2].position;
            }
            else
            {
                side1 = points[index].position - points[index + 1].position;
                side2 = points[index].position - points[index - 1].position;
            }
            return Vector3.Cross(side1.normalized, side2.normalized).normalized;
        }

        public override void DrawInspector()
        {
            if (editor.is2D)
            {
                EditorGUILayout.LabelField("Normal editing unavailable in 2D Mode", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            normalMode = (NormalMode)EditorGUILayout.EnumPopup("Normal Mode", normalMode);

            int setNormals = EditorGUILayout.Popup(0, new string[] {"Normal Operations", "Flip",  "Look At Camera", "Align with Camera", "Calculate", "Left", "Right", "Up", "Down", "Forward", "Back", "Look At Avg. Center", "Perpendicular to Spline" });
            if (setNormals > 0) SetNormals(setNormals);
        }

        public override void DrawScene()
        {
            if (editor.is2D) return;
            for (int i = 0; i < selectedPoints.Count; i++)
            {
                if (isClosed && selectedPoints[i] == points.Length - 1) continue;
                if (normalMode == NormalMode.Free) FreeNormal(selectedPoints[i]);
                else AutoNormal(selectedPoints[i]);
            }
        }

        void AutoNormal(int index)
        {
            editor.evaluateAtPoint(index, evalResult);
            Handles.color = highlightColor;
            Handles.DrawWireDisc(evalResult.position, evalResult.forward, HandleUtility.GetHandleSize(points[index].position) * 0.5f);
            Handles.color = color;
            Matrix4x4 matrix = Matrix4x4.TRS(points[index].position, evalResult.rotation, Vector3.one);
            Vector3 pos = points[index].position + points[index].normal * HandleUtility.GetHandleSize(points[index].position) * 0.5f;
            Handles.DrawLine(evalResult.position, pos);
            Vector3 lastPos = pos;
            Vector3 lastLocalPos = matrix.inverse.MultiplyPoint(pos);
            pos = Handles.FreeMoveHandle(pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * 0.1f, Vector3.zero, Handles.CircleHandleCap);
            if (pos != lastPos)
            {
                RecordUndo("Edit Point Normals");
                pos = matrix.inverse.MultiplyPoint(pos);
                Vector3 delta = pos - lastLocalPos;
                for (int n = 0; n < selectedPoints.Count; n++)
                {
                    if (selectedPoints[n] == index) continue;
                    editor.evaluateAtPoint(selectedPoints[n], evalResult);
                    Matrix4x4 localMatrix = Matrix4x4.TRS(points[selectedPoints[n]].position, evalResult.rotation, Vector3.one);
                    Vector3 localPos = localMatrix.inverse.MultiplyPoint(points[selectedPoints[n]].position + points[selectedPoints[n]].normal * HandleUtility.GetHandleSize(points[selectedPoints[n]].position) * 0.5f);
                    localPos += delta;
                    localPos.z = 0f;
                    points[selectedPoints[n]].normal = (localMatrix.MultiplyPoint(localPos) - points[selectedPoints[n]].position).normalized;
                }
                pos.z = 0f;
                pos = matrix.MultiplyPoint(pos);
                points[index].normal = (pos - points[index].position).normalized;
            }
        }

        void FreeNormal(int index)
        {
            Handles.color = highlightColor;
            Handles.DrawWireDisc(points[index].position, points[index].normal, HandleUtility.GetHandleSize(points[index].position) * 0.25f);
            Handles.DrawWireDisc(points[index].position, points[index].normal, HandleUtility.GetHandleSize(points[index].position) * 0.5f);
            Handles.color = color;
            Handles.DrawLine(points[index].position, points[index].position + HandleUtility.GetHandleSize(points[index].position) * points[index].normal);
            Vector3 normalPos = points[index].position + points[index].normal * HandleUtility.GetHandleSize(points[index].position);
            Vector3 lastNormal = points[index].normal;
            normalPos = SplineEditorHandles.FreeMoveCircle(normalPos, HandleUtility.GetHandleSize(normalPos) * 0.1f);
            normalPos -= points[index].position;
            normalPos.Normalize();
            if (normalPos == Vector3.zero) normalPos = Vector3.up;
            if (lastNormal != normalPos)
            {
                RecordUndo("Edit Point Normals");
                points[index].normal = normalPos;
                Quaternion delta = Quaternion.FromToRotation(lastNormal, normalPos);
                for (int n = 0; n < selectedPoints.Count; n++)
                {
                    if (selectedPoints[n] == index) continue;
                    points[selectedPoints[n]].normal = delta * points[selectedPoints[n]].normal;
                }
            }
        }
    }
}
