namespace CodeUtils
{
    public static class ExternalInputUtils
    {
        public static bool AreHotkeysEqual(Hotkey hotkey1, Hotkey hotkey2)
        {
            return hotkey1.keyCode == hotkey2.keyCode
                   && hotkey1.isControl == hotkey2.isControl
                   && hotkey1.isAlt == hotkey2.isAlt
                   && hotkey1.isShift == hotkey2.isShift;
        }
    }
}