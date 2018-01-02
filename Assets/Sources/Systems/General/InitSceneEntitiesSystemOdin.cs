using System;
using System.Collections.Generic;
using System.Linq;
using Entitas;
using Entitas.Unity;
using Entitas.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

public class InitSceneEntitiesSystemOdin : IInitializeSystem
{
    private readonly Contexts _contexts;
    private readonly GameContext _context;

    public InitSceneEntitiesSystemOdin(Contexts contexts)
    {
        _contexts = contexts;
        _context = contexts.game;
    }

    public void Initialize()
    {
      /*  List<ViewableEntityInitializer> initializableEntitiesBehaviours = Object.FindObjectsOfType<ViewableEntityInitializer>().ToList();
        
        foreach (ViewableEntityInitializer initializableEntity in initializableEntitiesBehaviours)
        {
            GameEntity entity = _context.CreateEntity();
            
            foreach (IComponent component in initializableEntity.config.components)
            {
                IComponent overrideComponent = null;
                if (initializableEntity.overrides != null)
                {
                    overrideComponent = initializableEntity.overrides.SingleOrDefault(c => c.GetType() == component.GetType());
                }
                IComponent finalComponent = overrideComponent ?? component;

                int componentIndex = Array.IndexOf(GameComponentsLookup.componentTypes, finalComponent.GetType());

                //@todo Implement univeral 'special case' initializers
                if (finalComponent.GetType() == typeof(NavAgentComponent))
                {
                    ((NavAgentComponent) finalComponent).value = initializableEntity.GetComponent<NavAgentBehaviour>();
                }

                IComponent finalComponentCopy = (IComponent)Activator.CreateInstance(finalComponent.GetType());
                finalComponent.CopyPublicMemberValues(finalComponentCopy);
                
                entity.AddComponent(componentIndex, finalComponentCopy);
            }
            
            entity.AddView(initializableEntity.gameObject);
            entity.AddPosition(initializableEntity.gameObject.transform.position);
            entity.AddRotation(initializableEntity.gameObject.transform.rotation);
            
            initializableEntity.gameObject.Link(entity, _context);
            Object.Destroy(initializableEntity);
        }*/
    }
}