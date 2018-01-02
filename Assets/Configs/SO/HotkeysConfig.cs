using UnityEngine;

[CreateAssetMenu(menuName="Configs/Hotkeys")]
public class HotkeysConfig : ScriptableObject
{
	#region CameraHotkeys

	public Hotkey cameraMoveForward;
	public Hotkey cameraMoveBackward;
	public Hotkey cameraMoveLeft;
	public Hotkey cameraMoveRight;
	
	public Hotkey cameraRotateRight;
	public Hotkey cameraRotateLeft;

	#endregion
	
}
