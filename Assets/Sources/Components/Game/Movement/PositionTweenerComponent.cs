using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Entitas;
using UnityEngine;

[Game]
public class PositionTweenerComponent : IComponent
{
    public TweenerCore<Vector3, Vector3, VectorOptions> value;
}