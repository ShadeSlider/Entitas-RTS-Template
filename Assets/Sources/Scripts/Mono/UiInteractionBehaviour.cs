using UnityEngine;
using UnityEngine.EventSystems;

public class UiInteractionBehaviour : MonoBehaviour	
    , IPointerEnterHandler
    , IPointerExitHandler
{
    public bool IsMouseOverUi { get; private set; }

    public void OnPointerEnter(PointerEventData pEventData)
    {
        IsMouseOverUi = true;
    }

    public void OnPointerExit(PointerEventData pEventData)
    {
        IsMouseOverUi = false;
    }
}