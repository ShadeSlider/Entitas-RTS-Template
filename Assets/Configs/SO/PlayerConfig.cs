using UnityEngine;

[CreateAssetMenu(menuName="Configs/Player")]
public class PlayerConfig : ScriptableObject
{
    public bool isAi;
    public new string name;
    public Color mainColor;
}
