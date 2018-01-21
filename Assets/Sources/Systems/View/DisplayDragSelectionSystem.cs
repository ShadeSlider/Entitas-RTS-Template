using System.Collections.Generic;
using CodeUtils;
using Entitas;
using UnityEngine;

namespace Sources.Systems.View
{
    public sealed class DisplayDragSelectionSystem : ReactiveSystem<InputEntity>, ICleanupSystem {

        readonly Contexts _contexts;
        readonly InputContext _context;

        public DisplayDragSelectionSystem(Contexts contexts) : base(contexts.input) {
            _contexts = contexts;
            _context = contexts.input;
        }

        protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context) {
            return context.CreateCollector(InputMatcher.DragSelectionData);
        }

        protected override bool Filter(InputEntity entity)
        {
            return entity.dragSelectionData.mouseHeldScreenPoint != Vector2.zero;
        }

        protected override void Execute(List<InputEntity> entities)
        {
            DragSelectionDataComponent dragSelectionDataComponent = _context.dragSelectionDataEntity.dragSelectionData;
            UiEntity dragSelectionImageEntity = _contexts.ui.dragSelectionImageUiEntity;

            RectTransform dragSelectionImageTransform = (RectTransform)dragSelectionImageEntity.view.gameObject.transform;
        
            Vector2 selectionCenter = (dragSelectionDataComponent.mouseDownScreenPoint + dragSelectionDataComponent.mouseHeldScreenPoint) / 2f;
            float selectionWidth =  Mathf.Abs (dragSelectionDataComponent.mouseHeldScreenPoint.x - dragSelectionDataComponent.mouseDownScreenPoint.x);
            float selectionHeight =  Mathf.Abs (dragSelectionDataComponent.mouseHeldScreenPoint.y - dragSelectionDataComponent.mouseDownScreenPoint.y);

            dragSelectionImageTransform.gameObject.SetActive(true);
            dragSelectionImageTransform.position = selectionCenter;
            dragSelectionImageTransform.sizeDelta = new Vector2(selectionWidth, selectionHeight);
        
            if (dragSelectionDataComponent.mouseUpScreenPoint != Vector2.zero)
            {
                dragSelectionImageTransform.gameObject.SetActive(false);
            }
        }

        public void Cleanup()
        {
            DragSelectionDataComponent dragSelectionDataComponent = _context.dragSelectionDataEntity.dragSelectionData;
            if (dragSelectionDataComponent.mouseUpScreenPoint != Vector2.zero)
            {
                _context.ReplaceDragSelectionData(Vector2.zero, Vector2.zero, Vector2.zero);
            }
        }
    }
}
