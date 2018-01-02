using UnityEngine;

[System.Serializable]
public class Hotkey
{
    public KeyCode keyCode;
    public bool isShift;
    public bool isControl;
    public bool isAlt;

    public Hotkey(KeyCode keyCode, bool isShift, bool isControl, bool isAlt)
    {
        this.keyCode = keyCode;
        this.isShift = isShift;
        this.isControl = isControl;
        this.isAlt = isAlt;
    }
}