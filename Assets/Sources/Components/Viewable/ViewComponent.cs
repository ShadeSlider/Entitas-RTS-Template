using Entitas;
using Entitas.CodeGeneration.Attributes;
using UnityEngine;

[Game, Input, Ui]
public class ViewComponent : IComponent
{
    [EntityIndex]
    public GameObject gameObject;
}