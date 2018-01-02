using Configs.SO;
using Entitas;
using Entitas.CodeGeneration.Attributes;

[Meta]
[Unique]
public class GameConfigComponent : IComponent
{
    public GameConfig value;
}