using System.Collections.Generic;
using Entitas;

public sealed class UpdateViewPositionAndRotationSystem : ReactiveSystem<GameEntity> {

    readonly GameContext _context;

    public UpdateViewPositionAndRotationSystem(Contexts contexts) : base(contexts.game) {
        _context = contexts.game;
    }

    protected override ICollector<GameEntity> GetTrigger(IContext<GameEntity> context) {
        return context.CreateCollector(GameMatcher.AllOf(GameMatcher.Position, GameMatcher.Rotation));
    }

    protected override bool Filter(GameEntity entity) {
        return entity.hasPosition && entity.hasRotation;
    }

    protected override void Execute(List<GameEntity> entities) {
        foreach (var entity in entities) {
            entity.view.gameObject.transform.position = entity.position.value;
            entity.view.gameObject.transform.rotation = entity.rotation.value;
        }
    }
}
