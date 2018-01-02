using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace CodeUtils
{
    public static class DebugExt
    {
        public static void LogMethod()
        {
            
            StackTrace st = new StackTrace ();
            StackFrame sf = st.GetFrame (1);

            Debug.Log(sf.GetMethod().DeclaringType + " => " + sf.GetMethod().Name);
        }
    }
}