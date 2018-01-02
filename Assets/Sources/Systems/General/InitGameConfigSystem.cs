using System.IO;
using Configs.SO;
using Entitas;
using Newtonsoft.Json;
using UnityEngine;

namespace Sources.Systems.General
{
    public class InitGameConfigSystem : IInitializeSystem
    {
        private readonly MetaContext _context;
        private readonly GameConfig _config;

        private string _controlsConfigPath;
        private string _hotkeysConfigPath;

        public InitGameConfigSystem(Contexts contexts, GameConfig config)
        {
            _context = contexts.meta;
            _config = config;
        }

        public void Initialize()
        {
            GameConfig runtimeConfig = Object.Instantiate(_config);

            InitializeDirectories();

            OverwriteConfigs(); //Comment to disable writing to config files on every startup

            StreamReader controlsStreamReader = File.OpenText(_controlsConfigPath);
            runtimeConfig.controls = JsonConvert.DeserializeObject<ControlsConfig>(controlsStreamReader.ReadToEnd());
            
            StreamReader hotkeysStreamReader = File.OpenText(_hotkeysConfigPath);
            runtimeConfig.hotkeys = JsonConvert.DeserializeObject<HotkeysConfig>(hotkeysStreamReader.ReadToEnd());
            
//            _context.SetGameConfig(runtimeConfig); //Uncomment to enable config files hookup
            _context.SetGameConfig(_config); 
        }

        private void InitializeDirectories()
        {
            string appDataPath = _config.appDataPath;
            string configsPath = appDataPath + Path.DirectorySeparatorChar + _config.configsDirName + Path.DirectorySeparatorChar;
            
            _controlsConfigPath = configsPath + _config.controlsConfigFilename;
            _hotkeysConfigPath = configsPath + _config.hotkeysConfigFilename;

            if (!Directory.Exists(configsPath))
            {
                Directory.CreateDirectory(configsPath);
            }
        }

        private void OverwriteConfigs()
        {
            StreamWriter swc = File.CreateText(_controlsConfigPath);
            swc.Write(JsonConvert.SerializeObject(_config.controls, Formatting.Indented));
            swc.Close();
            
            StreamWriter swh = File.CreateText(_hotkeysConfigPath);
            swh.Write(JsonConvert.SerializeObject(_config.hotkeys, Formatting.Indented));
            swh.Close();
        }
    }
}