using System.Collections.Generic;
using Entitas;
using Entitas.Unity;
using UnityEngine;

// IDestroyed: "I'm an Entity, I can have a DestroyedComponent AND I can have a ViewComponent"
public interface IDestroyEntity : IEntity, IDestroyed, IView { }

// tell the compiler that our context-specific entities implement IDestroyed
public partial class GameEntity : IDestroyEntity { }
public partial class InputEntity : IDestroyEntity { }
public partial class UiEntity : IDestroyEntity { }

// inherit from MultiReactiveSystem using the IDestroyed interface defined above
public class EntityDestroySystem : MultiReactiveSystem<IDestroyEntity, Contexts>
{
    public EntityDestroySystem(Contexts contexts) : base(contexts)
    {
    }

    protected override ICollector[] GetTrigger(Contexts contexts)
    {
        return new ICollector[] {
            contexts.game.CreateCollector(GameMatcher.Destroyed),
            contexts.input.CreateCollector(InputMatcher.Destroyed),
            contexts.ui.CreateCollector(UiMatcher.Destroyed)
        };
    }

    protected override bool Filter(IDestroyEntity entity)
    {
        return entity.isDestroyed;
    }

    protected override void Execute(List<IDestroyEntity> entities)
    {
        foreach (var e in entities)
        {
            // now we can access the ViewComponent and the DestroyedComponent
            if (e.hasView)
            {
                GameObject go = e.view.gameObject;
                go.Unlink();
                Object.Destroy(go);
            }
            e.Destroy();
        }
    }
}