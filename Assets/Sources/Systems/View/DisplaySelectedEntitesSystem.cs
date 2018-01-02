using System.Collections.Generic;
using Entitas;
using UnityEngine;

public sealed class DisplaySelectedEntitesSystem : ReactiveSystem<GameEntity> {

    readonly GameContext _context;

    public DisplaySelectedEntitesSystem(Contexts contexts) : base(contexts.game) {
        _context = contexts.game;
    }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
        return context.CreateCollector(GameMatcher.Selected.AddedOrRemoved());
    }

    protected override bool Filter(GameEntity entity) {
        return entity.hasView;
    }

    protected override void Execute(List<GameEntity> entities) {
        
        foreach (GameEntity entity in entities)
        {
            GameObject entityGo = entity.view.gameObject;

            Transform selectionMarkTransform = entityGo.transform.Find("SelectionMark");

            if (selectionMarkTransform != null)
            {
                selectionMarkTransform.gameObject.SetActive(entity.isSelected);    
            }
        }
    }
}
