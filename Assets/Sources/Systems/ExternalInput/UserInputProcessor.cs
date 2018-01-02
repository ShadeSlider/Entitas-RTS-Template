using System.Linq;
using System.Reflection;
using Entitas;
using UnityEngine;

namespace Sources.Systems.ExternalInput
{
    public class UserInputProcessor : IInitializeSystem, IExecuteSystem, ICleanupSystem
    {
        private Contexts _contexts;
        private InputContext _context;
        private IGroup<InputEntity> _mouseEventsGroup;
        private IGroup<InputEntity> _keyEventsGroup;
        private InputEntity _mouseEventEntity;
        private UiEntity _mainUiEntity;
    
        private Hotkey[] _hotkeys;
    
        private bool _keyWasDown;
    
        public UserInputProcessor(Contexts contexts)
        {
            _contexts = contexts;
            _context = contexts.input;
            _mouseEventsGroup = _context.GetGroup(InputMatcher.AllOf(InputMatcher.MouseEvent, InputMatcher.ScreenPoint));
            _keyEventsGroup = _context.GetGroup(InputMatcher.AllOf(InputMatcher.KeyEvent));
            _mouseEventEntity = _context.GetGroup(InputMatcher.MouseEvent).GetSingleEntity();
        }

        public void Initialize()
        {
            CreateMouseEventEntity();

            //Controls
            HotkeysConfig hotkeysConfig = _contexts.meta.gameConfig.value.hotkeys;
            FieldInfo[] hotkeyFields = hotkeysConfig.GetType().GetFields().Where(f => f.FieldType == typeof(Hotkey)).ToArray();
            _hotkeys = new Hotkey[hotkeyFields.Length];

            for (int i = 0; i < hotkeyFields.Length; i++)
            {
                _hotkeys[i] = (Hotkey)hotkeyFields[i].GetValue(hotkeysConfig);
            }
            
            //Main Ui
            _mainUiEntity = _contexts.ui.mainUiRootEntity;
        }

        private void CreateMouseEventEntity()
        {
            _mouseEventEntity = _context.CreateEntity();
            _mouseEventEntity.isMouseEvent = true;
            _mouseEventEntity.AddScreenPoint(Input.mousePosition);
        }

        public void Execute()
        {
            CreateMouseEventEntity();

            if (_mainUiEntity.view.gameObject.GetComponent<UiInteractionBehaviour>().IsMouseOverUi)
            {
                _mouseEventEntity.isMouseOverUi = true;
            }
        
            if (!Input.GetAxis("Mouse X").Equals(0) || !Input.GetAxis("Mouse Y").Equals(0))
            {
                _mouseEventEntity.ReplaceScreenPoint(Input.mousePosition);
            }

            if (!Input.GetAxis("Mouse ScrollWheel").Equals(0))
            {
                if (Input.GetAxis("Mouse ScrollWheel") < 0)
                {
                    _mouseEventEntity.isMouseScrollDown = true;
                }
                else
                {
                    _mouseEventEntity.isMouseScrollUp = true;
                }
            }            

            _mouseEventEntity.isLeftMouseButtonDown = Input.GetMouseButtonDown(0); 
            _mouseEventEntity.isLeftMouseButtonHeld = Input.GetMouseButton(0); 
            _mouseEventEntity.isLeftMouseButtonUp = Input.GetMouseButtonUp(0); 

            _mouseEventEntity.isRightMouseButtonDown = Input.GetMouseButtonDown(1); 
            _mouseEventEntity.isRightMouseButtonHeld = Input.GetMouseButton(1); 
            _mouseEventEntity.isRightMouseButtonUp = Input.GetMouseButtonUp(1); 
            
            if (!Input.anyKey && !Input.anyKeyDown && !_keyWasDown)
            {
                return;
            }
        
        
            _keyWasDown = Input.anyKey || Input.anyKeyDown;
            
            for (int i = 0; i < _hotkeys.Length; i++)
            {
                bool isKeyDown = Input.GetKeyDown(_hotkeys[i].keyCode);
                bool isKeyHeld = Input.GetKey(_hotkeys[i].keyCode);
                bool isKeyUp = Input.GetKeyUp(_hotkeys[i].keyCode);
            
                if (isKeyDown || isKeyHeld || isKeyUp)
                {
                    bool isShiftModifier = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    bool isControlModifier = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                    bool isAltModifier = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            
                    InputEntity keyEntity = _context.CreateEntity();
                    keyEntity.AddKeyEvent(new Hotkey(_hotkeys[i].keyCode, isShiftModifier, isControlModifier, isAltModifier));
                    keyEntity.isKeyDown = isKeyDown;
                    keyEntity.isKeyHeld = isKeyHeld;
                    keyEntity.isKeyUp = isKeyUp;
                }
            }            
        }

        public void Cleanup() 
        {
            foreach (InputEntity inputEntity in _mouseEventsGroup)
            {
                inputEntity.isDestroyed = true;
            }
            foreach (InputEntity inputEntity in _keyEventsGroup)
            {
                inputEntity.isDestroyed = true;
            }    
        }
    }
}