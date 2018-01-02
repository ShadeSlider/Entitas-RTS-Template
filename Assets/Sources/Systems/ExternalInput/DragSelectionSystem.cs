using System.Collections.Generic;
using System.Linq;
using Configs.SO;
using Entitas;
using UnityEngine;

namespace Sources.Systems.ExternalInput
{
    public sealed class DragSelectionSystem : ReactiveSystem<InputEntity>, IInitializeSystem {

        readonly Contexts _contexts;
        readonly InputContext _context;
        private IGroup<GameEntity> _selectedEntitiesGroup;
        private IGroup<InputEntity> _keyEventGroup;
        private IGroup<InputEntity> _dragSelectionDataGroup;
        private GameConfig _gameConfig;

        public DragSelectionSystem(Contexts contexts) : base(contexts.input)
        {
            _contexts = contexts;
            _context = contexts.input;
            _selectedEntitiesGroup = contexts.game.GetGroup(GameMatcher.AllOf(GameMatcher.Selected));
            _keyEventGroup = contexts.input.GetGroup(InputMatcher.AllOf(InputMatcher.KeyEvent, InputMatcher.KeyHeld));
            _dragSelectionDataGroup = contexts.input.GetGroup(InputMatcher.DragSelectionData);
        }
    
    
        public void Initialize()
        {
            _gameConfig = _contexts.meta.gameConfig.value;
            _context.SetDragSelectionData(new ScreenPointComponent(), new ScreenPointComponent(), new ScreenPointComponent());
        }

        protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context) {
            return context.CreateCollector(
                InputMatcher.AllOf(
                    InputMatcher.ScreenPoint, 
                    InputMatcher.MouseEvent 
                ).AnyOf(InputMatcher.LeftMouseButtonDown, InputMatcher.LeftMouseButtonHeld, InputMatcher.LeftMouseButtonUp)
            );
        }

        protected override bool Filter(InputEntity entity)
        {
            return entity.hasScreenPoint;
        }

        protected override void Execute(List<InputEntity> entities)
        {
            InputEntity dragSelectionDataEntity = _context.dragSelectionDataEntity;
        
            foreach (InputEntity entity in entities)
            {
                if (entity.isLeftMouseButtonDown)
                {
                    dragSelectionDataEntity.dragSelectionData.mouseDownScreenPointComponent.value = entity.screenPoint.value;
                }
                else if (entity.isLeftMouseButtonHeld) {
                    dragSelectionDataEntity.dragSelectionData.mouseHeldScreenPointComponent.value = entity.screenPoint.value;
                }
                else if (entity.isLeftMouseButtonUp) {
                    dragSelectionDataEntity.dragSelectionData.mouseUpScreenPointComponent.value = entity.screenPoint.value;
                }
            }


            dragSelectionDataEntity.ReplaceComponent(InputComponentsLookup.DragSelectionData, dragSelectionDataEntity.dragSelectionData);
            
            DragSelectionDataComponent dragSelectionDataComponent = dragSelectionDataEntity.dragSelectionData;
            if (
                dragSelectionDataComponent.mouseUpScreenPointComponent.value == Vector2.zero 
                || ((dragSelectionDataComponent.mouseHeldScreenPointComponent.value - dragSelectionDataComponent.mouseDownScreenPointComponent.value).magnitude < _gameConfig.inputConfig.dragSelectionDeadZone) 
            )
            {
                return;
            }
        
            InputEntity addToSelectionKeyEvent = _keyEventGroup.GetEntities().SingleOrDefault(e => e.keyEvent.value.keyCode == KeyCode.LeftShift);
            bool isAddToSelectionKeyHeld = addToSelectionKeyEvent != null && addToSelectionKeyEvent.isKeyHeld;
        
            if (!isAddToSelectionKeyHeld)
            {
                foreach (var gameEntity in _selectedEntitiesGroup.GetEntities())
                {
                    gameEntity.isSelected = false;
                }            
            }

            Vector2 mouseDownViewport = Camera.main.ScreenToViewportPoint (dragSelectionDataComponent.mouseDownScreenPointComponent.value);
            Vector2 mouseUpViewport = Camera.main.ScreenToViewportPoint (dragSelectionDataComponent.mouseUpScreenPointComponent.value);
            Rect selectionRect = new Rect (mouseDownViewport.x, mouseDownViewport.y, mouseUpViewport.x - mouseDownViewport.x, mouseUpViewport.y - mouseDownViewport.y);

            GameEntity[] selectableEntities = _contexts.game.GetGroup(GameMatcher.Selectable).GetEntities();
            Camera mainCamera = Camera.main;
            foreach (var selectableEntity in selectableEntities)
            {
                GameObject selectableGo = selectableEntity.view.gameObject;
                if (selectionRect.Contains (mainCamera.WorldToViewportPoint (selectableGo.transform.position), true)) {
                    selectableEntity.isSelected = true;

                }
            }
        }
    }
}
