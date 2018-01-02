using System.Collections.Generic;
using System.Linq;
using Entitas;
using Entitas.Unity;
using UnityEngine;

namespace Sources.Systems.ExternalInput
{
    public sealed class ClickSelectionSystem : ReactiveSystem<InputEntity> {

        readonly InputContext _context;
        private IGroup<GameEntity> _selectedEntitiesGroup;
        private IGroup<InputEntity> _keyEventGroup;

        public ClickSelectionSystem(Contexts contexts) : base(contexts.input) {
            _context = contexts.input;
            _selectedEntitiesGroup = contexts.game.GetGroup(GameMatcher.AllOf(GameMatcher.Selected));
            _keyEventGroup = contexts.input.GetGroup(InputMatcher.AllOf(InputMatcher.KeyEvent, InputMatcher.KeyHeld));
        }

        protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context) {
            return context.CreateCollector(InputMatcher.AllOf(InputMatcher.ScreenPoint, InputMatcher.MouseEvent, InputMatcher.LeftMouseButtonUp).NoneOf(InputMatcher.MouseOverUi));
        }

        protected override bool Filter(InputEntity entity)
        {
            return entity.hasScreenPoint;
        }

        protected override void Execute(List<InputEntity> entities)
        {

            InputEntity addToSelectionKeyEvent = _keyEventGroup.GetEntities().SingleOrDefault(e => e.keyEvent.value.keyCode == KeyCode.LeftShift);
            bool isAddToSelectionKeyHeld = addToSelectionKeyEvent != null && addToSelectionKeyEvent.isKeyHeld;
            InputEntity entity = entities.Single();

            RaycastHit hit;
            if(Physics.Raycast (Camera.main.ScreenPointToRay (entity.screenPoint.value), out hit, Mathf.Infinity)) {

                if (!isAddToSelectionKeyHeld)
                {
                    foreach (var gameEntity in _selectedEntitiesGroup.GetEntities())
                    {
                        gameEntity.isSelected = false;
                    }            
                }
                
                GameObject clickTargetGo = null;
            
                if (hit.collider != null) {
                    clickTargetGo = hit.collider.gameObject;
                } else if(hit.rigidbody !=null) {
                    clickTargetGo = hit.rigidbody.gameObject;	
                }

                if (clickTargetGo == null)
                {
                    return;
                }
            
                EntityLink clickedEntityLink = clickTargetGo.GetComponentInParent<EntityLink>();
                
                if (clickedEntityLink == null)
                {
                    return;
                }
            
                GameEntity clickedEntity = (GameEntity)clickedEntityLink.entity;
                if (clickedEntity.isSelectable)
                {
                    if (isAddToSelectionKeyHeld)
                    {
                        clickedEntity.isSelected = !clickedEntity.isSelected;
                    }
                    else
                    {
                        clickedEntity.isSelected = true;
                    }
                }
            }            
        }
    }
}
