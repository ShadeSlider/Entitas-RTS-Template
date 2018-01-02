using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Entitas;
using Entitas.Unity;
using Object = UnityEngine.Object;

public class InitSceneEntitiesSystem : IInitializeSystem
{
    private readonly Contexts _contexts;
    private readonly GameContext _context;

    public InitSceneEntitiesSystem(Contexts contexts)
    {
        _contexts = contexts;
    }

    public void Initialize()
    {
        List<ViewableEntityInitializer> initializableEntities = Object.FindObjectsOfType<ViewableEntityInitializer>().ToList();
        
        foreach (ViewableEntityInitializer initializableEntity in initializableEntities)
        {
            List<BaseComponentMonoBehaviour> monoComponents = initializableEntity.GetComponents<BaseComponentMonoBehaviour>().ToList();

            IContext currentContext = _contexts.game;

            Entity entity = (Entity) currentContext.GetType().InvokeMember("CreateEntity", BindingFlags.InvokeMethod, null, currentContext, null);

            string componentLookupClassName = currentContext.contextInfo.name + "ComponentsLookup";
            Type[] componentTypes = (Type[]) Type.GetType(componentLookupClassName).GetField("componentTypes", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            IComponent viewComponent = new ViewComponent {gameObject = initializableEntity.gameObject};
            int viewComponentIndex = Array.IndexOf(componentTypes, viewComponent.GetType());
            entity.AddComponent(viewComponentIndex, viewComponent);
            
            foreach (BaseComponentMonoBehaviour monoComponent in monoComponents)
            {
                var component = monoComponent.Component;
                int componentIndex = Array.IndexOf(componentTypes, component.GetType());
                
                //@todo Implement univeral 'special case' initializers
                if (component.GetType() == typeof(NavAgentComponent))
                {
                    ((NavAgentComponent) component).value = initializableEntity.GetComponent<NavAgentBehaviour>();
                }
                
                entity.AddComponent(componentIndex, component);

                Object.Destroy(monoComponent);
            } 
            
            initializableEntity.gameObject.Link(entity, currentContext);
            Object.Destroy(initializableEntity);
        }
    }
}