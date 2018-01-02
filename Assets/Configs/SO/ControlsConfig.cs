using UnityEngine;

[CreateAssetMenu(menuName="Configs/Controls")]
public class ControlsConfig : ScriptableObject
{
	#region CameraControls

	public float cameraMinHeight;
	
	public float cameraMaxHeight;

	[Range(0, 100)]
	public float cameraMovementSpeed;
	
	[Range(0, 100)]
	public float cameraRotationSpeed;
	
	[Range(0, 100)]
	public float cameraZoomSpeed;

	#endregion
	
}
