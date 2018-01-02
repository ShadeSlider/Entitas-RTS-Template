using System.Collections.Generic;
using System.Linq;
using Entitas;
using UnityEngine;

public sealed class RightClickNavigationSystem : ReactiveSystem<InputEntity>, IInitializeSystem {

    readonly Contexts _contexts;
    readonly InputContext _context;
    private IGroup<GameEntity> _selectedNavAgentsGroup;

    public RightClickNavigationSystem(Contexts contexts) : base(contexts.input) {
        _contexts = contexts;
        _context = contexts.input;
    }

    protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context) {
        return context.CreateCollector(InputMatcher.RightMouseButtonUp);
    }

    protected override bool Filter(InputEntity entity)
    {
        return entity.hasScreenPoint;
    }

    public void Initialize()
    {
        _selectedNavAgentsGroup = _contexts.game.GetGroup(GameMatcher.AllOf(GameMatcher.Selected, GameMatcher.NavAgent));
        
        GameObject targetGo = new GameObject("RightClickNavigationTarget");

        GameEntity targetEntity = _contexts.game.CreateEntity();
        targetEntity.isRightClickNavigationTarget = true;
        targetEntity.AddView(targetGo);
    }

    protected override void Execute(List<InputEntity> entities)
    {
        GameEntity[] navAgentEntities = _selectedNavAgentsGroup.GetEntities();

        if (navAgentEntities.Length == 0)
        {
            return;
        }
        
        InputEntity entity = entities.Single();
        RaycastHit hit;
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(entity.screenPoint.value), out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
        {
            return;
        }

        GameObject targetGo = _contexts.game.rightClickNavigationTargetEntity.view.gameObject;
        targetGo.transform.position = hit.point;
        _contexts.game.rightClickNavigationTargetEntity.ReplaceView(targetGo);

        
        foreach (GameEntity navAgentEntity in navAgentEntities)
        {
            navAgentEntity.navAgent.value.QueuePath(_contexts.game.rightClickNavigationTargetEntity.view.gameObject.transform.position);
            navAgentEntity.isMoving = true;
        }
    }
}
