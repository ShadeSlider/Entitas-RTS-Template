//#define ASTAR_POOL_DEBUG //@SHOWINEDITOR Enables debugging of path pooling. Will log warnings and info messages about paths not beeing pooled correctly.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding {
	/** Provides additional traversal information to a path request.
	 * \see \ref turnbased
	 */
	public interface ITraversalProvider {
		bool CanTraverse (Path path, GraphNode node);
		uint GetTraversalCost (Path path, GraphNode node);
	}

	/** Base class for all path types */
	public abstract class Path : IPathInternals {
		/** Data for the thread calculating this path */
		protected PathHandler pathHandler;

		/** Callback to call when the path is complete.
		 * This is usually sent to the Seeker component which post processes the path and then calls a callback to the script which requested the path
		 */
		public OnPathDelegate callback;

		/** Immediate callback to call when the path is complete.
		 * \warning This may be called from a separate thread. Usually you do not want to use this one.
		 *
		 * \see callback
		 */
		public OnPathDelegate immediateCallback;

		/** Returns the state of the path in the pathfinding pipeline */
		internal PathState PipelineState { get; private set; }
		System.Object stateLock = new object();

		/** Provides additional traversal information to a path request.
		 * \see \ref turnbased
		 */
		public ITraversalProvider traversalProvider;


		/** Current state of the path */
		public PathCompleteState CompleteState { get; protected set; }

		/** If the path failed, this is true.
		 * \see #errorLog
		 */
		public bool error { get { return CompleteState == PathCompleteState.Error; } }

		/** Additional info on what went wrong.
		 * \see #error
		 */
		private string _errorLog = "";

		/** Log messages with info about any errors. */
		public string errorLog {
			get { return _errorLog; }
		}

		/** Holds the path as a Node array. All nodes the path traverses.
		 * This may not be the same nodes as the post processed path traverses.
		 */
		public List<GraphNode> path;

		/** Holds the (possibly post processed) path as a Vector3 list */
		public List<Vector3> vectorPath;

		/** The node currently being processed */
		protected PathNode currentR;

		/** How long it took to calculate this path in milliseconds */
		internal float duration;

		/** Number of nodes this path has searched */
		protected int searchedNodes;

		/** True if the path is currently pooled.
		 * Do not set this value. Only read. It is used internally.
		 *
		 * \see PathPool
		 * \version Was named 'recycled' in 3.7.5 and earlier.
		 */
		bool IPathInternals.Pooled { get; set; }

		/** True if the path is currently recycled (i.e in the path pool).
		 * Do not set this value. Only read. It is used internally.
		 *
		 * \deprecated Has been renamed to 'pooled' to use more widely underestood terminology
		 */
		[System.Obsolete("Has been renamed to 'Pooled' to use more widely underestood terminology", true)]
		internal bool recycled { get { return false; } }

		/** True if the Reset function has been called.
		 * Used to alert users when they are doing something wrong.
		 */
		protected bool hasBeenReset;

		/** Constraint for how to search for nodes */
		public NNConstraint nnConstraint = PathNNConstraint.Default;

		/** Internal linked list implementation.
		 * \warning This is used internally by the system. You should never change this.
		 */
		internal Path next;

		/** Determines which heuristic to use */
		public Heuristic heuristic;

		/** Scale of the heuristic values.
		 * \see AstarPath.heuristicScale
		 */
		public float heuristicScale = 1F;

		/** ID of this path. Used to distinguish between different paths */
		internal ushort pathID { get; private set; }

		/** Target to use for H score calculation. Used alongside #hTarget. */
		protected GraphNode hTargetNode;

		/** Target to use for H score calculations. \see Pathfinding.Node.H */
		protected Int3 hTarget;

		/** Which graph tags are traversable.
		 * This is a bitmask so -1 = all bits set = all tags traversable.
		 * For example, to set bit 5 to true, you would do
		 * \code myPath.enabledTags |= 1 << 5; \endcode
		 * To set it to false, you would do
		 * \code myPath.enabledTags &= ~(1 << 5); \endcode
		 *
		 * The Seeker has a popup field where you can set which tags to use.
		 * \note If you are using a Seeker. The Seeker will set this value to what is set in the inspector field on StartPath.
		 * So you need to change the Seeker value via script, not set this value if you want to change it via script.
		 *
		 * \see CanTraverse
		 */
		public int enabledTags = -1;

		/** List of zeroes to use as default tag penalties */
		static readonly int[] ZeroTagPenalties = new int[32];

		/** The tag penalties that are actually used.
		 * If manualTagPenalties is null, this will be ZeroTagPenalties
		 * \see tagPenalties
		 */
		protected int[] internalTagPenalties;

		/** Tag penalties set by other scripts
		 * \see tagPenalties
		 */
		protected int[] manualTagPenalties;

		/** Penalties for each tag.
		 * Tag 0 which is the default tag, will have added a penalty of tagPenalties[0].
		 * These should only be positive values since the A* algorithm cannot handle negative penalties.
		 * \note This array will never be null. If you try to set it to null or with a length which is not 32. It will be set to "new int[0]".
		 *
		 * \note If you are using a Seeker. The Seeker will set this value to what is set in the inspector field on StartPath.
		 * So you need to change the Seeker value via script, not set this value if you want to change it via script.
		 *
		 * \see Seeker.tagPenalties
		 */
		public int[] tagPenalties {
			get {
				return manualTagPenalties;
			}
			set {
				if (value == null || value.Length != 32) {
					manualTagPenalties = null;
					internalTagPenalties = ZeroTagPenalties;
				} else {
					manualTagPenalties = value;
					internalTagPenalties = value;
				}
			}
		}

		/** True for paths that want to search all nodes and not jump over nodes as optimizations.
		 * This disables Jump Point Search when that is enabled to prevent e.g ConstantPath and FloodPath
		 * to become completely useless.
		 */
		internal virtual bool FloodingPath {
			get {
				return false;
			}
		}

		/** Total Length of the path.
		 * Calculates the total length of the #vectorPath.
		 * Cache this rather than call this function every time since it will calculate the length every time, not just return a cached value.
		 * \returns Total length of #vectorPath, if #vectorPath is null positive infinity is returned.
		 */
		public float GetTotalLength () {
			if (vectorPath == null) return float.PositiveInfinity;
			float tot = 0;
			for (int i = 0; i < vectorPath.Count-1; i++) tot += Vector3.Distance(vectorPath[i], vectorPath[i+1]);
			return tot;
		}

		/** Waits until this path has been calculated and returned.
		 * Allows for very easy scripting.
		 * \code
		 * IEnumerator Start () {
		 *  var path = seeker.StartPath (transform.position, transform.position+transform.forward*10, OnPathComplete);
		 *  yield return StartCoroutine (path.WaitForPath());
		 *  // The path is calculated no
		 * }
		 * \endcode
		 *
		 * \note Do not confuse this with AstarPath.WaitForPath. This one will wait using yield until it has been calculated
		 * while AstarPath.WaitForPath will halt all operations until the path has been calculated.
		 *
		 * \throws System.InvalidOperationException if the path is not started. Send the path to Seeker.StartPath or AstarPath.StartPath before calling this function.
		 *
		 * \see BlockUntilCalculated
		 */
		public IEnumerator WaitForPath () {
			if (PipelineState == PathState.Created) throw new System.InvalidOperationException("This path has not been started yet");

			while (PipelineState != PathState.Returned) yield return null;
		}

		/** Blocks until this path has been calculated and returned.
		 * Normally it takes a few frames for a path to be calculated and returned.
		 * This function will ensure that the path will be calculated when this function returns
		 * and that the callback for that path has been called.
		 *
		 * Use this function only if you really need to.
		 * There is a point to spreading path calculations out over several frames.
		 * It smoothes out the framerate and makes sure requesting a large
		 * number of paths at the same time does not cause lag.
		 *
		 * \note Graph updates and other callbacks might get called during the execution of this function.
		 *
		 * \code
		 * Path p = seeker.StartPath (transform.position, transform.position + Vector3.forward * 10);
		 * p.BlockUntilCalculated();
		 * // The path is calculated now
		 * \endcode
		 *
		 * \see This is equivalent to calling AstarPath.BlockUntilCalculated(Path)
		 * \see WaitForPath
		 */
		public void BlockUntilCalculated () {
			AstarPath.BlockUntilCalculated(this);
		}

		/** Estimated cost from the specified node to the target.
		 * \see https://en.wikipedia.org/wiki/A*_search_algorithm
		 */
		internal uint CalculateHScore (GraphNode node) {
			uint h;

			switch (heuristic) {
			case Heuristic.Euclidean:
				h = (uint)(((GetHTarget() - node.position).costMagnitude)*heuristicScale);
				return h;
			case Heuristic.Manhattan:
				Int3 p2 = node.position;
				h = (uint)((System.Math.Abs(hTarget.x-p2.x) + System.Math.Abs(hTarget.y-p2.y) + System.Math.Abs(hTarget.z-p2.z))*heuristicScale);
				return h;
			case Heuristic.DiagonalManhattan:
				Int3 p = GetHTarget() - node.position;
				p.x = System.Math.Abs(p.x);
				p.y = System.Math.Abs(p.y);
				p.z = System.Math.Abs(p.z);
				int diag = System.Math.Min(p.x, p.z);
				int diag2 = System.Math.Max(p.x, p.z);
				h = (uint)((((14*diag)/10) + (diag2-diag) + p.y) * heuristicScale);
				return h;
			}
			return 0U;
		}

		/** Returns penalty for the given tag.
		 * \param tag A value between 0 (inclusive) and 32 (exclusive).
		 */
		internal uint GetTagPenalty (int tag) {
			return (uint)internalTagPenalties[tag];
		}

		internal Int3 GetHTarget () {
			return hTarget;
		}

		/** Returns if the node can be traversed.
		 * This per default equals to if the node is walkable and if the node's tag is included in #enabledTags */
		internal bool CanTraverse (GraphNode node) {
			// Use traversal provider if set, otherwise fall back on default behaviour
			// This method is hot, but this branch is extremely well predicted so it
			// doesn't affect performance much (profiling indicates it is just above
			// the noise level, somewhere around 0%-0.3%)
			if (traversalProvider != null)
				return traversalProvider.CanTraverse(this, node);

			unchecked { return node.Walkable && (enabledTags >> (int)node.Tag & 0x1) != 0; }
		}

		internal uint GetTraversalCost (GraphNode node) {
#if ASTAR_NO_TRAVERSAL_COST
			return 0;
#else
			// Use traversal provider if set, otherwise fall back on default behaviour
			if (traversalProvider != null)
				return traversalProvider.GetTraversalCost(this, node);

			unchecked { return GetTagPenalty((int)node.Tag) + node.Penalty; }
#endif
		}

		/** May be called by graph nodes to get a special cost for some connections.
		 * Nodes may call it when PathNode.flag2 is set to true, for example mesh nodes, which have
		 * a very large area can be marked on the start and end nodes, this method will be called
		 * to get the actual cost for moving from the start position to its neighbours instead
		 * of as would otherwise be the case, from the start node's position to its neighbours.
		 * The position of a node and the actual start point on the node can vary quite a lot.
		 *
		 * The default behaviour of this method is to return the previous cost of the connection,
		 * essentiall making no change at all.
		 *
		 * This method should return the same regardless of the order of a and b.
		 * That is f(a,b) == f(b,a) should hold.
		 *
		 * \param a Moving from this node
		 * \param b Moving to this node
		 * \param currentCost The cost of moving between the nodes. Return this value if there is no meaningful special cost to return.
		 */
		internal virtual uint GetConnectionSpecialCost (GraphNode a, GraphNode b, uint currentCost) {
			return currentCost;
		}

		/** Returns if this path is done calculating.
		 * \returns If CompleteState is not PathCompleteState.NotCalculated.
		 *
		 * \note The callback for the path might not have been called yet.
		 *
		 * \since Added in 3.0.8
		 *
		 * \see Seeker.IsDone
		 */
		public bool IsDone () {
			return CompleteState != PathCompleteState.NotCalculated;
		}

		/** Threadsafe increment of the state */
		void IPathInternals.AdvanceState (PathState s) {
			lock (stateLock) {
				PipelineState = (PathState)System.Math.Max((int)PipelineState, (int)s);
			}
		}

		/** Returns the state of the path in the pathfinding pipeline.
		 * \deprecated Use the #Pathfinding.Path.PipelineState property instead
		 */
		[System.Obsolete("Use the 'PipelineState' property instead")]
		public PathState GetState () {
			return PipelineState;
		}

		/** Appends \a msg to #errorLog and logs \a msg to the console.
		 * Debug.Log call is only made if AstarPath.logPathResults is not equal to None and not equal to InGame.
		 * Consider calling Error() along with this call.
		 */
// Ugly Code Inc. wrote the below code :D
// What it does is that it disables the LogError function if ASTAR_NO_LOGGING is enabled
// since the DISABLED define will never be enabled
// Ugly way of writing Conditional("!ASTAR_NO_LOGGING")
		internal void LogError (string msg) {
#if !UNITY_EDITOR
			// Optimize for release builds
			// If no path logging is enabled
			// don't append to the error log string
			// to reduce allocations
			if (AstarPath.active.logPathResults != PathLog.None) {
				_errorLog += msg;
			}
#else
			_errorLog += msg;
#endif

			if (AstarPath.active.logPathResults != PathLog.None && AstarPath.active.logPathResults != PathLog.InGame) {
				Debug.LogWarning(msg);
			}
		}

		/** Logs an error and calls Error().
		 * This is called only if something is very wrong or the user is doing something he/she really should not be doing.
		 */
		internal void ForceLogError (string msg) {
			Error();
			_errorLog += msg;
			Debug.LogError(msg);
		}

		/** Appends a message to the #errorLog.
		 * Nothing is logged to the console.
		 *
		 * \note If AstarPath.logPathResults is PathLog.None and this is a standalone player, nothing will be logged as an optimization.
		 */
		internal void Log (string msg) {
#if !UNITY_EDITOR
			// Optimize for release builds
			// If no path logging is enabled
			// don't append to the error log string
			// to reduce allocations
			if (AstarPath.active.logPathResults != PathLog.None) {
				_errorLog += msg;
			}
#endif
		}

		/** Aborts the path because of an error.
		 * Sets #error to true.
		 * This function is called when an error has ocurred (e.g a valid path could not be found).
		 * \see LogError
		 */
		public void Error () {
			CompleteState = PathCompleteState.Error;
		}

		/** Does some error checking.
		 * Makes sure the user isn't using old code paths and that no major errors have been done.
		 *
		 * \throws An exception if any errors are found
		 */
		private void ErrorCheck () {
			if (!hasBeenReset) throw new System.Exception("The path has never been reset. Use the static Construct call, do not use the normal constructors.");
			if (((IPathInternals)this).Pooled) throw new System.Exception("The path is currently in a path pool. Are you sending the path for calculation twice?");
			if (pathHandler == null) throw new System.Exception("Field pathHandler is not set. Please report this bug.");
			if (PipelineState > PathState.Processing) throw new System.Exception("This path has already been processed. Do not request a path with the same path object twice.");
		}

		/** Called when the path enters the pool.
		 * This method should release e.g pooled lists and other pooled resources
		 * The base version of this method releases vectorPath and path lists.
		 * Reset() will be called after this function, not before.
		 * \warning Do not call this function manually.
		 */
		protected virtual void OnEnterPool () {
			if (vectorPath != null) Pathfinding.Util.ListPool<Vector3>.Release(vectorPath);
			if (path != null) Pathfinding.Util.ListPool<GraphNode>.Release(path);
			vectorPath = null;
			path = null;
			// Clear the callback to remove a potential memory leak
			// while the path is in the pool (which it could be for a long time).
			callback = null;
			immediateCallback = null;
			traversalProvider = null;
		}

		/** Reset all values to their default values.
		 *
		 * \note All inheriting path types (e.g ConstantPath, RandomPath, etc.) which declare their own variables need to
		 * override this function, resetting ALL their variables to enable pooling of paths.
		 * If this is not done, trying to use that path type for pooling could result in weird behaviour.
		 * The best way is to reset to default values the variables declared in the extended path type and then
		 * call the base function in inheriting types with base.Reset().
		 */
		protected virtual void Reset () {
			if (System.Object.ReferenceEquals(AstarPath.active, null))
				throw new System.NullReferenceException("No AstarPath object found in the scene. " +
					"Make sure there is one or do not create paths in Awake");

			hasBeenReset = true;
			PipelineState = (int)PathState.Created;
			releasedNotSilent = false;

			pathHandler = null;
			callback = null;
			immediateCallback = null;
			_errorLog = "";
			CompleteState = PathCompleteState.NotCalculated;

			path = Pathfinding.Util.ListPool<GraphNode>.Claim();
			vectorPath = Pathfinding.Util.ListPool<Vector3>.Claim();

			currentR = null;

			duration = 0;
			searchedNodes = 0;

			nnConstraint = PathNNConstraint.Default;
			next = null;

			heuristic = AstarPath.active.heuristic;
			heuristicScale = AstarPath.active.heuristicScale;

			enabledTags = -1;
			tagPenalties = null;

			pathID = AstarPath.active.GetNextPathID();

			hTarget = Int3.zero;
			hTargetNode = null;

			traversalProvider = null;
		}

		/** List of claims on this path with reference objects */
		private List<System.Object> claimed = new List<System.Object>();

		/** True if the path has been released with a non-silent call yet.
		 *
		 * \see Release
		 * \see Claim
		 */
		private bool releasedNotSilent;

		/** Claim this path (pooling).
		 * A claim on a path will ensure that it is not pooled.
		 * If you are using a path, you will want to claim it when you first get it and then release it when you will not
		 * use it anymore. When there are no claims on the path, it will be reset and put in a pool.
		 *
		 * This is essentially just reference counting.
		 *
		 * The object passed to this method is merely used as a way to more easily detect when pooling is not done correctly.
		 * It can be any object, when used from a movement script you can just pass "this". This class will throw an exception
		 * if you try to call Claim on the same path twice with the same object (which is usually not what you want) or
		 * if you try to call Release with an object that has not been used in a Claim call for that path.
		 * The object passed to the Claim method needs to be the same as the one you pass to this method.
		 *
		 * \see Release
		 * \see Pool
		 * \see \ref pooling
		 * \see https://en.wikipedia.org/wiki/Reference_counting
		 */
		public void Claim (System.Object o) {
			if (System.Object.ReferenceEquals(o, null)) throw new System.ArgumentNullException("o");

			for (int i = 0; i < claimed.Count; i++) {
				// Need to use ReferenceEquals because it might be called from another thread
				if (System.Object.ReferenceEquals(claimed[i], o))
					throw new System.ArgumentException("You have already claimed the path with that object ("+o+"). Are you claiming the path with the same object twice?");
			}

			claimed.Add(o);
		}

		/** Releases the path silently (pooling).
		 * \deprecated Use Release(o, true) instead
		 */
		[System.Obsolete("Use Release(o, true) instead")]
		internal void ReleaseSilent (System.Object o) {
			Release(o, true);
		}

		/** Releases a path claim (pooling).
		 * Removes the claim of the path by the specified object.
		 * When the claim count reaches zero, the path will be pooled, all variables will be cleared and the path will be put in a pool to be used again.
		 * This is great for memory since less allocations are made.
		 *
		 * If the silent parameter is true, this method will remove the claim by the specified object
		 * but the path will not be pooled if the claim count reches zero unless a Release call (not silent) has been made earlier.
		 * This is used by the internal pathfinding components such as Seeker and AstarPath so that they will not cause paths to be pooled.
		 * This enables users to skip the claim/release calls if they want without the path being pooled by the Seeker or AstarPath and
		 * thus causing strange bugs.
		 *
		 * \see Claim
		 * \see PathPool
		 */
		public void Release (System.Object o, bool silent = false) {
			if (o == null) throw new System.ArgumentNullException("o");

			for (int i = 0; i < claimed.Count; i++) {
				// Need to use ReferenceEquals because it might be called from another thread
				if (System.Object.ReferenceEquals(claimed[i], o)) {
					claimed.RemoveAt(i);
					if (!silent) {
						releasedNotSilent = true;
					}

					if (claimed.Count == 0 && releasedNotSilent) {
						PathPool.Pool(this);
					}
					return;
				}
			}
			if (claimed.Count == 0) {
				throw new System.ArgumentException("You are releasing a path which is not claimed at all (most likely it has been pooled already). " +
					"Are you releasing the path with the same object ("+o+") twice?" +
					"\nCheck out the documentation on path pooling for help.");
			}
			throw new System.ArgumentException("You are releasing a path which has not been claimed with this object ("+o+"). " +
				"Are you releasing the path with the same object twice?\n" +
				"Check out the documentation on path pooling for help.");
		}

		/** Traces the calculated path from the end node to the start.
		 * This will build an array (#path) of the nodes this path will pass through and also set the #vectorPath array to the #path arrays positions.
		 * Assumes the #vectorPath and #path are empty and not null (which will be the case for a correctly initialized path).
		 */
		protected virtual void Trace (PathNode from) {
			// Current node we are processing
			PathNode c = from;
			int count = 0;

			while (c != null) {
				c = c.parent;
				count++;
				if (count > 2048) {
					Debug.LogWarning("Infinite loop? >2048 node path. Remove this message if you really have that long paths (Path.cs, Trace method)");
					break;
				}
			}

			// Ensure capacities for lists
			AstarProfiler.StartProfile("Check List Capacities");

			if (path.Capacity < count) path.Capacity = count;
			if (vectorPath.Capacity < count) vectorPath.Capacity = count;

			AstarProfiler.EndProfile();

			c = from;

			for (int i = 0; i < count; i++) {
				path.Add(c.node);
				c = c.parent;
			}

			// Reverse
			int half = count/2;
			for (int i = 0; i < half; i++) {
				var tmp = path[i];
				path[i] = path[count-i-1];
				path[count - i - 1] = tmp;
			}

			for (int i = 0; i < count; i++) {
				vectorPath.Add((Vector3)path[i].position);
			}
		}

		/** Writes text shared for all overrides of DebugString to the string builder.
		 * \see DebugString
		 */
		protected void DebugStringPrefix (PathLog logMode, System.Text.StringBuilder text) {
			text.Append(error ? "Path Failed : " : "Path Completed : ");
			text.Append("Computation Time ");
			text.Append(duration.ToString(logMode == PathLog.Heavy ? "0.000 ms " : "0.00 ms "));

			text.Append("Searched Nodes ").Append(searchedNodes);

			if (!error) {
				text.Append(" Path Length ");
				text.Append(path == null ? "Null" : path.Count.ToString());
			}
		}

		/** Writes text shared for all overrides of DebugString to the string builder.
		 * \see DebugString
		 */
		protected void DebugStringSuffix (PathLog logMode, System.Text.StringBuilder text) {
			if (error) {
				text.Append("\nError: ").Append(errorLog);
			}

			// Can only print this from the Unity thread
			// since otherwise an exception might be thrown
			if (logMode == PathLog.Heavy && !AstarPath.active.IsUsingMultithreading) {
				text.Append("\nCallback references ");
				if (callback != null) text.Append(callback.Target.GetType().FullName).AppendLine();
				else text.AppendLine("NULL");
			}

			text.Append("\nPath Number ").Append(pathID).Append(" (unique id)");
		}

		/** Returns a string with information about it.
		 * More information is emitted when logMode == Heavy.
		 * An empty string is returned if logMode == None
		 * or logMode == OnlyErrors and this path did not fail.
		 */
		internal virtual string DebugString (PathLog logMode) {
			if (logMode == PathLog.None || (!error && logMode == PathLog.OnlyErrors)) {
				return "";
			}

			// Get a cached string builder for this thread
			System.Text.StringBuilder text = pathHandler.DebugStringBuilder;
			text.Length = 0;

			DebugStringPrefix(logMode, text);
			DebugStringSuffix(logMode, text);

			return text.ToString();
		}

		/** Calls callback to return the calculated path. \see #callback */
		protected virtual void ReturnPath () {
			if (callback != null) {
				callback(this);
			}
		}

		/** Prepares low level path variables for calculation.
		 * Called before a path search will take place.
		 * Always called before the Prepare, Initialize and CalculateStep functions
		 */
		protected void PrepareBase (PathHandler pathHandler) {
			//Path IDs have overflowed 65K, cleanup is needed
			//Since pathIDs are handed out sequentially, we can do this
			if (pathHandler.PathID > pathID) {
				pathHandler.ClearPathIDs();
			}

			//Make sure the path has a reference to the pathHandler
			this.pathHandler = pathHandler;
			//Assign relevant path data to the pathHandler
			pathHandler.InitializeForPath(this);

			// Make sure that internalTagPenalties is an array which has the length 32
			if (internalTagPenalties == null || internalTagPenalties.Length != 32)
				internalTagPenalties = ZeroTagPenalties;

			try {
				ErrorCheck();
			} catch (System.Exception e) {
				ForceLogError("Exception in path "+pathID+"\n"+e);
			}
		}

		/** Called before the path is started.
		 * Called right before Initialize
		 */
		protected abstract void Prepare ();

		/** Always called after the path has been calculated.
		 * Guaranteed to be called before other paths have been calculated on
		 * the same thread.
		 * Use for cleaning up things like node tagging and similar.
		 */
		protected virtual void Cleanup () {}

		/** Initializes the path.
		 * Sets up the open list and adds the first node to it
		 */
		protected abstract void Initialize ();

		/** Calculates the until it is complete or the time has progressed past \a targetTick */
		protected abstract void CalculateStep (long targetTick);

		PathHandler IPathInternals.PathHandler { get { return pathHandler; } }
		void IPathInternals.OnEnterPool () { OnEnterPool(); }
		void IPathInternals.Reset () { Reset(); }
		void IPathInternals.ReturnPath () { ReturnPath(); }
		void IPathInternals.PrepareBase (PathHandler handler) { PrepareBase(handler); }
		void IPathInternals.Prepare () { Prepare(); }
		void IPathInternals.Cleanup () { Cleanup(); }
		void IPathInternals.Initialize () { Initialize(); }
		void IPathInternals.CalculateStep (long targetTick) { CalculateStep(targetTick); }
	}

	/** Used for hiding internal methods of the Path class */
	internal interface IPathInternals {
		PathHandler PathHandler { get; }
		bool Pooled { get; set; }
		void AdvanceState (PathState s);
		void OnEnterPool ();
		void Reset ();
		void ReturnPath ();
		void PrepareBase (PathHandler handler);
		void Prepare ();
		void Initialize ();
		void Cleanup ();
		void CalculateStep (long targetTick);
	}
}
