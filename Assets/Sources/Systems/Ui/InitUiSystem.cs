using CodeUtils;
using Entitas;
using Entitas.Unity;
using UnityEngine;

public sealed class InitUiSystem : IInitializeSystem {

	private readonly Contexts _contexts;
    private readonly UiContext _context;
    private readonly UiConfig _config;
    
    
    public InitUiSystem(Contexts contexts, UiConfig config) {
        _contexts = contexts;
        _context = contexts.ui;
        _config = config;
    }

    public void Initialize()
    {
        GameObject mainUiGo = Object.Instantiate(_config.mainUi);
        mainUiGo.name = "MainUi";
        mainUiGo.transform.SetSiblingIndex(0);
        CreateMainUiRootEntity(mainUiGo);

        //Drag selection image
        GameObject dragSelectionImageGo = mainUiGo.transform.Find("DragSelectionCanvas/DragSelectionImage").gameObject;
        CreateDragSelectionImageEntity(dragSelectionImageGo);
        
        
        mainUiGo.SetActive(true);
    }

    private void CreateMainUiRootEntity(GameObject go)
    {
        UiEntity entity = _context.CreateEntity();
        entity.isMainUi = true;
        entity.isViewRoot = true;
        entity.isMainUiRoot = true;
        entity.AddView(go);
        go.Link(entity, _context);
    }
    
    private void CreateDragSelectionImageEntity(GameObject go)
    {
        UiEntity entity = _context.CreateEntity();
        entity.isUi = true;
        entity.isDragSelectionImageUi = true;
        entity.AddView(go);
        go.Link(entity, _context);
    }
}
