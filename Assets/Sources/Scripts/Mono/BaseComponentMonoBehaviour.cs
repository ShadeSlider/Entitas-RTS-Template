using Entitas;
using UnityEngine;

public abstract class BaseComponentMonoBehaviour : MonoBehaviour
{
     public abstract IComponent Component { get; }
}