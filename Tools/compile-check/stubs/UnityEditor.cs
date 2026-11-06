// Stand-in for UnityEditor, which has no downloadable reference assembly.
// Only the members the game's Editor scripts use. Add members here as needed.
namespace UnityEditor
{
    public sealed class MenuItem : System.Attribute { public MenuItem(string itemName) { } public MenuItem(string itemName, bool isValidateFunction, int priority) { } }
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
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene scene) { return true; }
    }
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
}
