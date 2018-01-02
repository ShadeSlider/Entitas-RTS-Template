using Entitas;
using Entitas.CodeGeneration.Attributes;

[Input]
[Unique]
public class DragSelectionDataComponent : IComponent
{
    public ScreenPointComponent mouseDownScreenPointComponent;
    public ScreenPointComponent mouseHeldScreenPointComponent;
    public ScreenPointComponent mouseUpScreenPointComponent;
}