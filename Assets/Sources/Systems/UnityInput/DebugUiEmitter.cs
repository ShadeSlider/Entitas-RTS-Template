using Configs.SO;
using UnityEngine;
using UnityEngine.UI;

namespace Sources.Systems.UnityInput
{
    public class DebugUiEmitter : MonoBehaviour
    {
        public GameController gameController;
        private Contexts _contexts;
        private GameConfig _config;
        public Button pauseToggleButton;
        public Transform debugUi;
        
        private void OnEnable()
        {
            gameController = FindObjectOfType<GameController>();
            _contexts = gameController.contexts;
            
            pauseToggleButton.onClick.AddListener(() =>
            {
                _contexts.meta.isGameStatePaused = !_contexts.meta.isGameStatePaused;
            });

            _config = _contexts.meta.gameConfig.value;
            
            debugUi = transform;
        }

        private void OnDisable()
        {
            pauseToggleButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            
        }
    }
}