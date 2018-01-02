using System.Collections.Generic;
using CodeUtils;
using DG.Tweening;
using Entitas;
using UnityEngine;

public sealed class CameraControllSystem : ReactiveSystem<InputEntity>, IInitializeSystem {

    private readonly Contexts _contexts;
    readonly InputContext _context;
    private GameEntity _mainCameraRigEntity;
    private GameEntity _mainCameraEntity;
    private ControlsConfig _controls;
    private HotkeysConfig _hotkeys;

    public CameraControllSystem(Contexts contexts) : base(contexts.input) {
        _contexts = contexts;
        _context = contexts.input;
    }

    protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context)
    {

        return new Collector<InputEntity>(
            new[]
            {
                context.GetGroup(InputMatcher
                    .AllOf(
                        InputMatcher.KeyEvent,
                        InputMatcher.KeyHeld
                    )
                ),
                context.GetGroup(InputMatcher.AnyOf(
                    InputMatcher.MouseScrollDown,
                    InputMatcher.MouseScrollUp
                ))
            },
            new[]
            {
                GroupEvent.AddedOrRemoved,
                GroupEvent.AddedOrRemoved
            }
        );
    }

    protected override bool Filter(InputEntity entity)
    {
        return entity.hasKeyEvent || entity.isMouseEvent;
    }

    public void Initialize()
    {
        _mainCameraRigEntity = _contexts.game.CreateEntity();
        _mainCameraRigEntity.AddView(Camera.main.transform.parent.gameObject);
        _mainCameraRigEntity.AddPosition(Camera.main.transform.parent.position);
        _mainCameraRigEntity.AddRotation(Camera.main.transform.parent.rotation);        
        _mainCameraRigEntity.isMainCameraRig = true;
        
        _mainCameraEntity = _contexts.game.CreateEntity();
        _mainCameraEntity.AddView(Camera.main.transform.gameObject);
        _mainCameraEntity.AddPosition(Camera.main.transform.position);
        _mainCameraEntity.AddRotation(Camera.main.transform.rotation);        
        _mainCameraEntity.isMainCamera = true;

        _controls = _contexts.meta.gameConfig.value.controls;
        _hotkeys = _contexts.meta.gameConfig.value.hotkeys;
    }

    protected override void Execute(List<InputEntity> entities) {
        Vector3 cameraMovementDirection = Vector3.zero;
        Vector3 cameraZoomDirection = Vector3.zero;
        Vector3 cameraRotationDirection = Vector3.zero;
        
        foreach (var entity in entities) {

            if (entity.isKeyHeld)
            {
                if (ExternalInputUtils.AreHotkeysEqual(entity.keyEvent.value, _hotkeys.cameraMoveForward))
                {
                    cameraMovementDirection += (_mainCameraRigEntity.rotation.value * Vector3.forward);
                }
                if (ExternalInputUtils.AreHotkeysEqual(entity.keyEvent.value, _hotkeys.cameraMoveBackward))
                {
                    cameraMovementDirection += (_mainCameraRigEntity.rotation.value * Vector3.back);
                }
                if (ExternalInputUtils.AreHotkeysEqual(entity.keyEvent.value, _hotkeys.cameraMoveLeft))
                {
                    cameraMovementDirection += (_mainCameraRigEntity.rotation.value * Vector3.left);
                }
                if (ExternalInputUtils.AreHotkeysEqual(entity.keyEvent.value, _hotkeys.cameraMoveRight))
                {
                    cameraMovementDirection += (_mainCameraRigEntity.rotation.value * Vector3.right);
                }
            
                if (ExternalInputUtils.AreHotkeysEqual(entity.keyEvent.value, _hotkeys.cameraRotateRight))
                {
                    cameraRotationDirection += Vector3.up;
                }
            
                if (ExternalInputUtils.AreHotkeysEqual(entity.keyEvent.value, _hotkeys.cameraRotateLeft))
                {
                    cameraRotationDirection += -Vector3.up;
                }
            }
            
            if (entity.isMouseEvent)
            {
                if (entity.isMouseScrollDown)
                {
                    cameraZoomDirection += Vector3.up;
                }
                if (entity.isMouseScrollUp)
                {
                    cameraZoomDirection += Vector3.down;
                }
            }
            
        }

        float distanceToMove = _controls.cameraMovementSpeed * Time.fixedUnscaledDeltaTime * 5;
        float angleToRotate = _controls.cameraRotationSpeed * 3.6f * Time.fixedUnscaledDeltaTime * 1.5f;

        Vector3 finalZoomVector = Vector3.zero;
        if (cameraZoomDirection != Vector3.zero)
        {
            finalZoomVector = cameraZoomDirection * _controls.cameraZoomSpeed * Time.fixedUnscaledDeltaTime * 10;    
        }
        
        Vector3 finalMovementVector = cameraMovementDirection * distanceToMove + finalZoomVector;
        
        if (finalMovementVector != Vector3.zero)
        {
            Vector3 targetPosition = _mainCameraRigEntity.position.value + finalMovementVector;

            targetPosition.y = Mathf.Max(targetPosition.y, _controls.cameraMinHeight);
            targetPosition.y = Mathf.Min(targetPosition.y, _controls.cameraMaxHeight);
            
            _mainCameraRigEntity.AnimatePosition(targetPosition, 0.1f);
        }

        if (cameraRotationDirection != Vector3.zero)
        {
            _mainCameraRigEntity.AnimateRotation(_mainCameraRigEntity.rotation.value * Quaternion.Euler(cameraRotationDirection * angleToRotate), 0.1f);
        }
        
    }
}
