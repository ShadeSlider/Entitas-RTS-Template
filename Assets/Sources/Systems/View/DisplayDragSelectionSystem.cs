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
            return entity.dragSelectionData.mouseHeldScreenPointComponent.value != Vector2.zero;
        }

        protected override void Execute(List<InputEntity> entities)
        {
            DragSelectionDataComponent dragSelectionDataComponent = _context.dragSelectionDataEntity.dragSelectionData;
            UiEntity dragSelectionImageEntity = _contexts.ui.dragSelectionImageUiEntity;

            RectTransform dragSelectionImageTransform = (RectTransform)dragSelectionImageEntity.view.gameObject.transform;
        
            Vector2 selectionCenter = (dragSelectionDataComponent.mouseDownScreenPointComponent.value + dragSelectionDataComponent.mouseHeldScreenPointComponent.value) / 2f;
            float selectionWidth =  Mathf.Abs (dragSelectionDataComponent.mouseHeldScreenPointComponent.value.x - dragSelectionDataComponent.mouseDownScreenPointComponent.value.x);
            float selectionHeight =  Mathf.Abs (dragSelectionDataComponent.mouseHeldScreenPointComponent.value.y - dragSelectionDataComponent.mouseDownScreenPointComponent.value.y);

            dragSelectionImageTransform.gameObject.SetActive(true);
            dragSelectionImageTransform.position = selectionCenter;
            dragSelectionImageTransform.sizeDelta = new Vector2(selectionWidth, selectionHeight);
        
            if (dragSelectionDataComponent.mouseUpScreenPointComponent.value != Vector2.zero)
            {
                dragSelectionImageTransform.gameObject.SetActive(false);
            }
        }

        public void Cleanup()
        {
            DragSelectionDataComponent dragSelectionDataComponent = _context.dragSelectionDataEntity.dragSelectionData;
            if (dragSelectionDataComponent.mouseUpScreenPointComponent.value != Vector2.zero)
            {
                _context.ReplaceDragSelectionData(new ScreenPointComponent(), new ScreenPointComponent(), new ScreenPointComponent());
            }
        }
    }
}
