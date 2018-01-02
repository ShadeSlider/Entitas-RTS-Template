using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Entitas;
using UnityEngine;

[Game]
public class RotationTweenerComponent : IComponent
{
    public TweenerCore<Quaternion, Vector3, QuaternionOptions> value;
}