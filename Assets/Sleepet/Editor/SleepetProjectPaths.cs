using UnityEditor;
using UnityEditor.SceneManagement;

namespace Sleepet.Editor
{
    public static class SleepetProjectPaths
    {
        public const string PetPrefabPath = "Assets/Sleepet/Prefabs/MochaUI.prefab";
        public const string AppPrefabPath = "Assets/Sleepet/Prefabs/SleepetApp.prefab";
        public const string HomeScenePath = "Assets/Sleepet/Scenes/Sleepet_Home.unity";

        [MenuItem("Sleepet/Open Home scene")]
        public static void OpenHome() => EditorSceneManager.OpenScene(HomeScenePath);
    }
}
