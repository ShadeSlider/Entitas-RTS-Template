using System.Collections.Generic;
using Entitas;

public sealed class NavAgentMovementSystem : ReactiveSystem<InputEntity>, IInitializeSystem {

    readonly Contexts _contexts;
    readonly InputContext _context;
    private IGroup<GameEntity> _movingNavAgentsGroup;

    public NavAgentMovementSystem(Contexts contexts) : base(contexts.input)
    {
        _contexts = contexts;
        _context = contexts.input;
    }

    protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context) {
        return context.CreateCollector(InputMatcher.Tick);
    }

    protected override bool Filter(InputEntity entity)
    {
        return true;
    }

    public void Initialize()
    {
        _movingNavAgentsGroup = _contexts.game.GetGroup(GameMatcher.AllOf(GameMatcher.NavAgent, GameMatcher.Moving));
    }

    protected override void Execute(List<InputEntity> entities)
    {
        foreach (GameEntity entity in _movingNavAgentsGroup.GetEntities())
        {
            NavAgentBehaviour navAgent = entity.navAgent.value; 
            navAgent.PerformMove();

            entity.ReplacePosition(navAgent.positionAfterMove);
            entity.ReplaceRotation(navAgent.rotationAfterMove);
            
            if (navAgent.TargetReached && !navAgent.IsPathQueued && !navAgent.IsSearching)
            {
                navAgent.targetPosition = null;
                entity.isMoving = false;
            }
        }
    }
}
