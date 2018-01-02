using System.IO;
using UnityEngine;

namespace Configs.SO
{
    [CreateAssetMenu(menuName="Configs/Game")]
    public class GameConfig : ScriptableObject
    {
        public UiConfig uiConfig;
        public InputConfig inputConfig;
        public PlayerConfig[] playerConfig;
        public ControlsConfig controls;
        public HotkeysConfig hotkeys;

        public string appDataPath;

        public string configsDirName;
        public string controlsConfigFilename;
        public string hotkeysConfigFilename;

        private void OnEnable()
        {
            appDataPath = Application.persistentDataPath + Path.DirectorySeparatorChar;
        }
    }
}
