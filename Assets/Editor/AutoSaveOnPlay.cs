using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AutosaveOnRun
{
	static AutosaveOnRun()
	{
		EditorApplication.playmodeStateChanged = () =>
		{
			if(EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
			{
				EditorSceneManager.SaveOpenScenes();
				AssetDatabase.SaveAssets();
			}
		};
	}
}