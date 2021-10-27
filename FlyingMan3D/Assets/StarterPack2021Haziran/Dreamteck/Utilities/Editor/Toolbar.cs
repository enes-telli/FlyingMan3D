namespace Dreamteck.Editor
{
    using UnityEngine;
    using UnityEditor;

    public class Toolbar
    {
        GUIContent[] shownContent;
        GUIContent[] allContent;
        public bool center = true;
        public bool newLine = true;
        public float elementWidth = 0f;
        public float elementHeight = 23f;

        public Toolbar(GUIContent[] iconsNormal, GUIContent[] iconsSelected, float elementWidth = 0f)
        {
            this.elementWidth = elementWidth;
            if(iconsNormal.Length != iconsSelected.Length)
            {
                Debug.LogError("Invalid icon count for toolbar ");
                return;
            }
            allContent = new GUIContent[iconsNormal.Length * 2];
            shownContent = new GUIContent[iconsNormal.Length];
            iconsNormal.CopyTo(allContent, 0);
            iconsSelected.CopyTo(allContent, iconsNormal.Length);
        }

        public void SetContent(int index, GUIContent content)
        {
            allContent[index] = content;
            allContent[shownContent.Length + index] = content;
        }

        public void SetContent(int index, GUIContent content, GUIContent contentSelected)
        {
            allContent[index] = content;
            allContent[shownContent.Length + index] = contentSelected;
        }

        public void Draw(ref int selected)
        {
            for (int i = 0; i < shownContent.Length; i++)
            {
                shownContent[i] = selected == i ? allContent[shownContent.Length + i] : allContent[i];
            }
            if(newLine) EditorGUILayout.BeginHorizontal();
            if(center) GUILayout.FlexibleSpace();
            if(elementWidth > 0f) selected = GUILayout.Toolbar(selected, shownContent, GUILayout.Width(elementWidth * shownContent.Length), GUILayout.Height(elementHeight));
            else selected = GUILayout.Toolbar(selected, shownContent, GUILayout.Height(elementHeight));
            if (center) GUILayout.FlexibleSpace();
            if (newLine) EditorGUILayout.EndHorizontal();
        }
    }
}
