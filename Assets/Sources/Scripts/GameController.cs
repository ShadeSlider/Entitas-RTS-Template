using Configs.SO;
using DG.Tweening;
using Entitas;
using Sources.Systems;
using Sources.Systems.ExternalInput;
using Sources.Systems.General;
using Sources.Systems.View;
using UnityEngine;

public class GameController : MonoBehaviour
{
	public GameConfig gameConfig;
	public Contexts contexts
	{
		get; private set;
	}

	private Systems _systems;
	
	// Use this for initialization
	private void OnEnable()
	{
		DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(1000, 10);
//		Application.targetFrameRate = 60;
// 		var contexts = new Contexts();  
 		contexts = Contexts.sharedInstance;
		
		_systems = new Feature("Systems")
				.Add(new Feature("General")
					//Initializers
					.Add(new TickSystem(contexts))	
					.Add(new InitGameConfigSystem(contexts, gameConfig))	
					.Add(new InitSceneEntitiesSystem(contexts))	
					
					//Reactive
					.Add(new CameraControllSystem(contexts))	
					
					//Cleaning up
					.Add(new EntityDestroySystem(contexts))				
				) 
				.Add(new Feature("Ui")
					//Initializers
					.Add(new InitUiSystem(contexts, gameConfig.uiConfig))	
					
					//Cleaning up
				) 
				.Add(new Feature("Input")
					.Add(new UserInputProcessor(contexts))				
					.Add(new RightClickNavigationSystem(contexts))				
					.Add(new ClickSelectionSystem(contexts))				
					.Add(new DragSelectionSystem(contexts))				
				) 
				.Add(new Feature("Movement")
					.Add(new NavAgentMovementSystem(contexts))				
					.Add(new UpdateViewPositionAndRotationSystem(contexts))				
				) 
				.Add(new Feature("View")
					.Add(new DisplaySelectedEntitesSystem(contexts))				
					.Add(new DisplayDragSelectionSystem(contexts))				
				) 

		;
		
		_systems.Initialize(); 
	}
	
	// Update is called once per frame
	void Update () {
		_systems.Execute();
		_systems.Cleanup();
	}
}
