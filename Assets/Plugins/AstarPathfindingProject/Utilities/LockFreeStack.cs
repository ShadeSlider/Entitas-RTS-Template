using System.Threading;

namespace Pathfinding.Util {
	/** Implements a lock free multiple producer - single consumer stack for the Path object.
	 * Though it probably works for multiple producer - multiple consumer as well.
	 *
	 * On iOS and some other platforms it falls back to using locking since Interlocked.CompareExchange is not available
	 * on those platforms.
	 */
	public class LockFreeStack {
		public Path head;

#if UNITY_IPHONE || UNITY_PSP2 || UNITY_XBOXONE || UNITY_PS3 || UNITY_PS4 || UNITY_WIIU || UNITY_SWITCH
		private System.Object lockObj = new System.Object();
#endif

		/** Pushes a path onto the stack.
		 * Will loop while trying to set the head of the stack to \a p. */
		public void Push (Path p) {
#if UNITY_IPHONE || UNITY_PSP2 || UNITY_XBOXONE || UNITY_PS3 || UNITY_PS4 || UNITY_WIIU || UNITY_SWITCH
			lock (lockObj) {
				p.next = head;
				head = p;
			}
#else
			while (true) {
				p.next = head;
				// Compare head and p.next, if they are equal, set head to p
				Path old = Interlocked.CompareExchange<Path>(ref head, p, p.next);
				// If the exchange suceeded, break. Otherwise, try again
				if (old == p.next) break;
			}
#endif
		}

		/** Pops all items from the stack and returns the head.
		 * To loop through all popped items, simple traverse the linked list starting with the head and continuing with item.next until item equals null
		 * \code
		 * Path p = stack.PopAll ();
		 * while (p != null) {
		 *  //Do something
		 *  p = p.next;
		 * }
		 * \endcode
		 */
		public Path PopAll () {
#if UNITY_IPHONE || UNITY_PSP2 || UNITY_XBOXONE || UNITY_PS3 || UNITY_PS4 || UNITY_WIIU || UNITY_SWITCH
			lock (lockObj) {
				Path h = head;
				head = null;
				return h;
			}
#else
			return Interlocked.Exchange<Path>(ref head, null);
#endif
		}
	}
}
