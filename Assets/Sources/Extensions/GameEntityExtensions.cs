using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public static class GameEntityExtensions
{
    public static void AnimatePosition(this GameEntity entity, Vector3 targetPosition, float duration, TweenParams tweenParams = null)
    {
        TweenerCore<Vector3, Vector3, VectorOptions> tweener;

        if (entity.hasPositionTweener)
        {
            tweener = entity.positionTweener.value;
            tweener.ChangeValues(entity.position.value, targetPosition, duration);
            
            entity.ReplacePositionTweener(tweener);
        }
        else
        {
            tweener = DOTween.To(
                () => entity.position.value,
                p => entity.position.value = p,
                targetPosition,
                duration
            )
            .OnUpdate(() => {  
                    entity.ReplacePosition(entity.position.value);
                }
            )
            .OnComplete(entity.RemovePositionTweener)            
            .OnKill(entity.RemovePositionTweener)
            ;

            if (tweenParams != null)
            {
                tweener.SetAs(tweenParams);
            }
            
            entity.AddPositionTweener(tweener);
        }
    }
    
    public static void AnimateRotation(this GameEntity entity, Quaternion targetRotation, float duration, TweenParams tweenParams = null)
    {
        TweenerCore<Quaternion, Vector3, QuaternionOptions> tweener;

        if (entity.hasRotationTweener)
        {
            tweener = entity.rotationTweener.value;
            tweener.ChangeValues(entity.rotation.value, targetRotation, duration);
            
            entity.ReplaceRotationTweener(tweener);
        }
        else
        {
            tweener = DOTween.To(
                () => entity.rotation.value,
                p => entity.rotation.value = p,
                targetRotation.eulerAngles,
                duration
            )
            .OnUpdate(() => {  
                    entity.ReplaceRotation(entity.rotation.value);
                }
            )
            .OnComplete(entity.RemoveRotationTweener)            
            .OnKill(entity.RemoveRotationTweener)
            ;

            if (tweenParams != null)
            {
                tweener.SetAs(tweenParams);
            }
        }
    }
}