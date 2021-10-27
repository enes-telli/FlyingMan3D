namespace Dreamteck
{
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;
#if !UNITY_2018_3_OR_NEWER
    using System.Reflection;
    using Type = System.Type;
#endif

    public static class DreamteckEditorGUI
    {
        public static Texture2D blankImage
        {
            get
            {
                if (_blankImage == null)
                {
                    _blankImage = new Texture2D(1, 1);
                    _blankImage.SetPixel(0, 0, Color.white);
                    _blankImage.Apply();
                }
                return _blankImage;
            }
        }
        private static Texture2D _blankImage = null;

        public static readonly Color backgroundColor = new Color(0.95f, 0.95f, 0.95f);
        public static Color iconColor = Color.black;

        public static readonly Color highlightColor = new Color(0f, 0.564f, 1f, 1f);
        public static readonly Color highlightContentColor = new Color(1f, 1f, 1f, 0.95f);


        public static readonly Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        public static readonly Color activeColor = new Color(1f, 1f, 1f, 1f);

        public static readonly Color baseColor = Color.white;
        public static readonly Color lightColor = Color.white;
        public static readonly Color lightDarkColor = Color.white;
        public static readonly Color darkColor = Color.white;
        public static readonly Color borderColor = Color.white;

        private static List<int> layerNumbers = new List<int>();

        public static readonly GUIStyle labelText = null;
        private static float scale = -1f;

#if !UNITY_2018_3_OR_NEWER
        private static MethodInfo gradientFieldMethod;
#endif

        static DreamteckEditorGUI()
        {
            baseColor = EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);
            lightColor = EditorGUIUtility.isProSkin ? new Color32(84, 84, 84, 255) : new Color32(222, 222, 222, 255);
            lightDarkColor = EditorGUIUtility.isProSkin ? new Color32(30, 30, 30, 255) : new Color32(180, 180, 180, 255);
            darkColor = EditorGUIUtility.isProSkin ? new Color32(15, 15, 15, 255) : new Color32(152, 152, 152, 255);
            borderColor = EditorGUIUtility.isProSkin ? new Color32(5, 5, 5, 255) : new Color32(100, 100, 100, 255);
            backgroundColor = baseColor;
            backgroundColor -= new Color(0.1f, 0.1f, 0.1f, 0f);
            iconColor = GUI.skin.label.normal.textColor;

            labelText = new GUIStyle(GUI.skin.GetStyle("label"));
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleRight;
            labelText.normal.textColor = Color.white;
            SetScale(1f);

#if !UNITY_2018_3_OR_NEWER
            Type tyEditorGUILayout = typeof(EditorGUILayout);
            gradientFieldMethod = tyEditorGUILayout.GetMethod("GradientField", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string), typeof(Gradient), typeof(GUILayoutOption[]) }, null);
#endif
            }

        public static void SetScale(float newScale)
        {
            if (scale == newScale) return;
            scale = newScale;
            labelText.fontSize = Mathf.RoundToInt(12f * scale);
        }

        public static void Label(Rect position, string text, bool active = true, GUIStyle style = null)
        {
            if (style == null) style = labelText;
            if (!active) GUI.color = inactiveColor;
            else GUI.color = activeColor;
            GUI.color = new Color(0f, 0f, 0f, GUI.color.a * 0.5f);
            GUI.Label(new Rect(position.x - 1, position.y + 1, position.width, position.height), text, style);
            if (!active) GUI.color = inactiveColor;
            else GUI.color = activeColor;
            GUI.Label(position, text, style);
        }

        public static LayerMask LayermaskField(string label, LayerMask layerMask)
        {
            string[] layers = UnityEditorInternal.InternalEditorUtility.layers;

            layerNumbers.Clear();

            for (int i = 0; i < layers.Length; i++)
            {
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));
            }

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                {
                    maskWithoutEmpty |= (1 << i);
                }
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                {
                    mask |= (1 << layerNumbers[i]);
                }
            }

            layerMask.value = mask;

            return layerMask;
        }

        public static bool DropArea<T>(Rect rect, out T[] content, bool acceptProjectAssets = false)
        {
            content = new T[0];
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(Event.current.mousePosition)) return false;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        List<T> contentList = new List<T>();
                        foreach (object dragged_object in DragAndDrop.objectReferences)
                        {
                            if (dragged_object is GameObject)
                            {
                                GameObject gameObject = (GameObject)dragged_object;
                                if (acceptProjectAssets || !AssetDatabase.Contains(gameObject))
                                {
                                    if (gameObject.GetComponent<T>() != null) contentList.Add(gameObject.GetComponent<T>());
                                }
                            }
                        }
                        content = contentList.ToArray();
                        return true; 
                    }
                    else return false;
            }
            return false;
        }


        public static Gradient GradientField(string label, Gradient gradient, params GUILayoutOption[] options)
        {
#if UNITY_2018_3_OR_NEWER
            return EditorGUILayout.GradientField(label, gradient, options);
#else
             gradient = (Gradient)gradientFieldMethod.Invoke(null, new object[] { label, gradient, options });
            return gradient;
#endif
        }

        public static void DrawSeparator()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(Screen.width / 2f, 2f);
            EditorGUI.DrawRect(rect, darkColor);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }
}
