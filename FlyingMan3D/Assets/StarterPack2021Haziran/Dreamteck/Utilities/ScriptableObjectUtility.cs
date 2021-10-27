#if UNITY_EDITOR
namespace Dreamteck {
    using UnityEngine;
    using UnityEditor;
    using System.IO;

    public static class ScriptableObjectUtility
    {
        public static T CreateAsset<T>(string name = "", bool selectAfterCreation = true) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            SaveAsset(asset, name, selectAfterCreation);
            return asset;
        }

        public static ScriptableObject CreateAsset(string type, string name = "", bool selectAfterCreation = true)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            SaveAsset<ScriptableObject>(asset, name, selectAfterCreation);
            return asset;
        }

        static void SaveAsset<T>(T asset, string name = "", bool selectAfterCreation = true) where T : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            string assetName = "New " + typeof(T).ToString();
            if (name != "") assetName = name;
            if(!path.EndsWith("/"))
            {
                path += "/";
            }
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + assetName + ".asset");
            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            if (selectAfterCreation)
            {
                Selection.activeObject = asset;
            }
        }
    }
}
#endif
