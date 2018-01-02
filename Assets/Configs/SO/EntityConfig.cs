using Entitas;
using UnityEngine;

[CreateAssetMenu(menuName="Configs/Entity")]
public class EntityConfig : ScriptableObject
{
	public IComponent[] components;
}
