//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public partial class InputEntity {

    static readonly DragSelectionEventComponent dragSelectionEventComponent = new DragSelectionEventComponent();

    public bool isDragSelectionEvent {
        get { return HasComponent(InputComponentsLookup.DragSelectionEvent); }
        set {
            if (value != isDragSelectionEvent) {
                var index = InputComponentsLookup.DragSelectionEvent;
                if (value) {
                    var componentPool = GetComponentPool(index);
                    var component = componentPool.Count > 0
                            ? componentPool.Pop()
                            : dragSelectionEventComponent;

                    AddComponent(index, component);
                } else {
                    RemoveComponent(index);
                }
            }
        }
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentMatcherGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public sealed partial class InputMatcher {

    static Entitas.IMatcher<InputEntity> _matcherDragSelectionEvent;

    public static Entitas.IMatcher<InputEntity> DragSelectionEvent {
        get {
            if (_matcherDragSelectionEvent == null) {
                var matcher = (Entitas.Matcher<InputEntity>)Entitas.Matcher<InputEntity>.AllOf(InputComponentsLookup.DragSelectionEvent);
                matcher.componentNames = InputComponentsLookup.componentNames;
                _matcherDragSelectionEvent = matcher;
            }

            return _matcherDragSelectionEvent;
        }
    }
}
