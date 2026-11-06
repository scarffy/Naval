using Naval.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Naval.Slice
{
    /// <summary>Loads the slice's CSV files from Resources/SliceData and starts a game.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SliceBootstrap : MonoBehaviour
    {
        /// <summary>The running game, or null if the data failed to load (see <see cref="LoadError"/>).</summary>
        public GameState State { get; private set; }
        /// <summary>The full message of the data error, or null when loading worked.</summary>
        public string LoadError { get; private set; }

        void Awake()
        {
            try
            {
                State = new GameState(LoadData());
            }
            catch (DataException e)
            {
                LoadError = e.Message;
                Debug.LogError("Slice data failed to load: " + e.Message);
            }
        }

        /// <summary>False when the scene is not in Build Settings, so <see cref="Restart"/> cannot reload it.</summary>
        public static bool CanRestart { get { return SceneManager.GetActiveScene().buildIndex >= 0; } }

        /// <summary>Starts a new game by reloading the active scene. The scene must be in Build Settings.</summary>
        public static void Restart()
        {
            if (!CanRestart)
            {
                Debug.LogError("Cannot restart: the scene is not in Build Settings. Run Naval > Build Slice Scene, which adds it.");
                return;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static GameData LoadData()
        {
            return DataLoader.Load(new SliceCsv
            {
                Goods = Read("goods"),
                Market = Read("market"),
                Ports = Read("ports"),
                Routes = Read("routes"),
                Ships = Read("ships"),
                Upgrades = Read("upgrades"),
                Rules = Read("rules")
            });
        }

        static string Read(string name)
        {
            var asset = Resources.Load<TextAsset>("SliceData/" + name);
            if (asset == null) throw new DataException("Resources/SliceData/" + name + ".csv is missing");
            return asset.text;
        }
    }
}
