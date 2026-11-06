// Stand-in for UnityEditor, which has no downloadable reference assembly.
// Only the members the game's Editor scripts use. Add members here as needed.
namespace UnityEditor
{
    public sealed class MenuItem : System.Attribute { public MenuItem(string itemName) { } public MenuItem(string itemName, bool isValidateFunction) { } public MenuItem(string itemName, bool isValidateFunction, int priority) { } }
    public static class EditorApplication { public static bool isPlaying { get; set; } }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string assetPath) where T : UnityEngine.Object { return null; }
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static bool IsValidFolder(string path) { return false; }
        public static string CreateFolder(string parentFolder, string newFolderName) { return null; }
        public static string[] FindAssets(string filter, string[] searchInFolders) { return null; }
        public static string GUIDToAssetPath(string guid) { return null; }
    }
    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object target) { }
        public static bool DisplayDialog(string title, string message, string ok) { return true; }
        public static bool DisplayDialog(string title, string message, string ok, string cancel) { return true; }
    }
    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string propertyPath) { return null; }
        public bool ApplyModifiedPropertiesWithoutUndo() { return true; }
    }
    public class SerializedProperty
    {
        public UnityEngine.Object objectReferenceValue { get; set; }
        public int arraySize { get; set; }
        public int intValue { get; set; }
        public float floatValue { get; set; }
        public UnityEngine.Vector2 vector2Value { get; set; }
        public SerializedProperty GetArrayElementAtIndex(int index) { return null; }
    }
    public static class PrefabUtility
    {
        public static UnityEngine.GameObject SaveAsPrefabAsset(UnityEngine.GameObject instanceRoot, string assetPath) { return null; }
    }
}
namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup, NewSceneMode mode) { return default(UnityEngine.SceneManagement.Scene); }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string dstScenePath) { return true; }
        public static UnityEngine.SceneManagement.Scene OpenScene(string scenePath) { return default(UnityEngine.SceneManagement.Scene); }
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; }
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene scene) { return true; }
    }
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
}
