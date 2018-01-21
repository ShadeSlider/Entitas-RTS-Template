using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Input]
[Unique]
public class DragSelectionDataComponent : IComponent
{
    public Vector2 mouseDownScreenPoint;
    public Vector2 mouseHeldScreenPoint;
    public Vector2 mouseUpScreenPoint;
}