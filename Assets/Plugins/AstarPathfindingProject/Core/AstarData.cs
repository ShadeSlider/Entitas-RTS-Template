using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.WindowsStore;
#if UNITY_WINRT && !UNITY_EDITOR
//using MarkerMetro.Unity.WinLegacy.IO;
//using MarkerMetro.Unity.WinLegacy.Reflection;
#endif

namespace Pathfinding {
	[System.Serializable]
	/** Stores the navigation graphs for the A* Pathfinding System.
	 * \ingroup relevant
	 *
	 * An instance of this class is assigned to AstarPath.data, from it you can access all graphs loaded through the #graphs variable.\n
	 * This class also handles a lot of the high level serialization.
	 */
	public class AstarData {
		/** Shortcut to AstarPath.active */
		public static AstarPath active {
			get {
				return AstarPath.active;
			}
		}

		#region Fields
		/** Shortcut to the first NavMeshGraph.
		 * Updated at scanning time
		 */
		public NavMeshGraph navmesh { get; private set; }

		/** Shortcut to the first GridGraph.
		 * Updated at scanning time
		 */
		public GridGraph gridGraph { get; private set; }


		/** Shortcut to the first PointGraph.
		 * Updated at scanning time
		 */
		public PointGraph pointGraph { get; private set; }


		/** All supported graph types.
		 * Populated through reflection search
		 */
		public System.Type[] graphTypes { get; private set; }

#if ASTAR_FAST_NO_EXCEPTIONS || UNITY_WINRT || UNITY_WEBGL
		/** Graph types to use when building with Fast But No Exceptions for iPhone.
		 * If you add any custom graph types, you need to add them to this hard-coded list.
		 */
		public static readonly System.Type[] DefaultGraphTypes = new System.Type[] {
			typeof(GridGraph),
			typeof(PointGraph),
			typeof(NavMeshGraph),
		};
#endif

		/** All graphs this instance holds.
		 * This will be filled only after deserialization has completed.
		 * May contain null entries if graph have been removed.
		 */
		[System.NonSerialized]
		public NavGraph[] graphs = new NavGraph[0];

		//Serialization Settings

		/** Serialized data for all graphs and settings.
		 * Stored as a base64 encoded string because otherwise Unity's Undo system would sometimes corrupt the byte data (because it only stores deltas).
		 *
		 * This can be accessed as a byte array from the #data property.
		 *
		 * \since 3.6.1
		 */
		[SerializeField]
		string dataString;

		/** Data from versions from before 3.6.1.
		 * Used for handling upgrades
		 * \since 3.6.1
		 */
		[SerializeField]
		[UnityEngine.Serialization.FormerlySerializedAs("data")]
		private byte[] upgradeData;

		/** Serialized data for all graphs and settings */
		private byte[] data {
			get {
				// Handle upgrading from earlier versions than 3.6.1
				if (upgradeData != null && upgradeData.Length > 0) {
					data = upgradeData;
					upgradeData = null;
				}
				return dataString != null ? System.Convert.FromBase64String(dataString) : null;
			}
			set {
				dataString = value != null ? System.Convert.ToBase64String(value) : null;
			}
		}

		/** Backup data if deserialization failed.
		 */
		public byte[] data_backup;

		/** Serialized data for cached startup.
		 * If set, on start the graphs will be deserialized from this file.
		 */
		public TextAsset file_cachedStartup;

		/** Serialized data for cached startup.
		 *
		 * \deprecated Deprecated since 3.6, AstarData.file_cachedStartup is now used instead
		 */
		public byte[] data_cachedStartup;

		/** Should graph-data be cached.
		 * Caching the startup means saving the whole graphs, not only the settings to an internal array (#data_cachedStartup) which can
		 * be loaded faster than scanning all graphs at startup. This is setup from the editor.
		 */
		[SerializeField]
		public bool cacheStartup;

		//End Serialization Settings

		List<bool> graphStructureLocked = new List<bool>();

		#endregion

		public byte[] GetData () {
			return data;
		}

		public void SetData (byte[] data) {
			this.data = data;
		}

		/** Loads the graphs from memory, will load cached graphs if any exists */
		public void Awake () {
			graphs = new NavGraph[0];

			if (cacheStartup && file_cachedStartup != null) {
				LoadFromCache();
			} else {
				DeserializeGraphs();
			}
		}

		/** Prevent the graph structure from changing during the time this lock is held.
		 * This prevents graphs from being added or removed and also prevents graphs from being serialized or deserialized.
		 * This is used when e.g an async scan is happening to ensure that for example a graph that is being scanned is not destroyed.
		 *
		 * Each call to this method *must* be paired with exactly one call to #UnlockGraphStructure.
		 * The calls may be nested.
		 */
		internal void LockGraphStructure (bool allowAddingGraphs = false) {
			graphStructureLocked.Add(allowAddingGraphs);
		}

		/** Allows the graph structure to change again.
		 * \see #LockGraphStructure
		 */
		internal void UnlockGraphStructure () {
			if (graphStructureLocked.Count == 0) throw new System.InvalidOperationException();
			graphStructureLocked.RemoveAt(graphStructureLocked.Count - 1);
		}

		PathProcessor.GraphUpdateLock AssertSafe (bool onlyAddingGraph = false) {
			if (graphStructureLocked.Count > 0) {
				bool allowAdding = true;
				for (int i = 0; i < graphStructureLocked.Count; i++) allowAdding &= graphStructureLocked[i];
				if (!(onlyAddingGraph && allowAdding)) throw new System.InvalidOperationException("Graphs cannot be added, removed or serialized while the graph structure is locked. This is the case when a graph is currently being scanned and when executing graph updates and work items.\nHowever as a special case, graphs can be added inside work items.");
			}

			// Pause the pathfinding threads
			var graphLock = active.PausePathfinding();
			if (!active.IsInsideWorkItem) {
				// Make sure all graph updates and other callbacks are done
				// Only do this if this code is not being called from a work item itself as that would cause a recursive wait that could never complete.
				// There are some valid cases when this can happen. For example it may be necessary to add a new graph inside a work item.
				active.FlushWorkItems();

				// Paths that are already calculated and waiting to be returned to the Seeker component need to be
				// processed immediately as their results usually depend on graphs that currently exist. If this was
				// not done then after destroying a graph one could get a path result with destroyed nodes in it.
				active.pathReturnQueue.ReturnPaths(false);
			}
			return graphLock;
		}

		/** Updates shortcuts to the first graph of different types.
		 * Hard coding references to some graph types is not really a good thing imo. I want to keep it dynamic and flexible.
		 * But these references ease the use of the system, so I decided to keep them.\n
		 */
		public void UpdateShortcuts () {
			navmesh = (NavMeshGraph)FindGraphOfType(typeof(NavMeshGraph));

			gridGraph = (GridGraph)FindGraphOfType(typeof(GridGraph));

			pointGraph = (PointGraph)FindGraphOfType(typeof(PointGraph));
		}

		/** Load from data from #file_cachedStartup */
		public void LoadFromCache () {
			var graphLock = AssertSafe();

			if (file_cachedStartup != null) {
				var bytes = file_cachedStartup.bytes;
				DeserializeGraphs(bytes);

				GraphModifier.TriggerEvent(GraphModifier.EventType.PostCacheLoad);
			} else {
				Debug.LogError("Can't load from cache since the cache is empty");
			}
			graphLock.Release();
		}

		#region Serialization

		/** Serializes all graphs settings to a byte array.
		 * \see DeserializeGraphs(byte[])
		 */
		public byte[] SerializeGraphs () {
			return SerializeGraphs(Pathfinding.Serialization.SerializeSettings.Settings);
		}

		/** Serializes all graphs settings and optionally node data to a byte array.
		 * \see DeserializeGraphs(byte[])
		 * \see Pathfinding.Serialization.SerializeSettings
		 */
		public byte[] SerializeGraphs (Pathfinding.Serialization.SerializeSettings settings) {
			uint checksum;

			return SerializeGraphs(settings, out checksum);
		}

		/** Main serializer function.
		 * Serializes all graphs to a byte array
		 * A similar function exists in the AstarPathEditor.cs script to save additional info */
		public byte[] SerializeGraphs (Pathfinding.Serialization.SerializeSettings settings, out uint checksum) {
			var graphLock = AssertSafe();
			var sr = new Pathfinding.Serialization.AstarSerializer(this, settings);

			sr.OpenSerialize();
			SerializeGraphsPart(sr);
			byte[] bytes = sr.CloseSerialize();
			checksum = sr.GetChecksum();
			graphLock.Release();
			return bytes;
		}

		/** Serializes common info to the serializer.
		 * Common info is what is shared between the editor serialization and the runtime serializer.
		 * This is mostly everything except the graph inspectors which serialize some extra data in the editor
		 */
		public void SerializeGraphsPart (Pathfinding.Serialization.AstarSerializer sr) {
			sr.SerializeGraphs(graphs);
			sr.SerializeExtraInfo();
		}

		/** Deserializes graphs from #data */
		public void DeserializeGraphs () {
			if (data != null) {
				DeserializeGraphs(data);
			}
		}

		/** Destroys all graphs and sets graphs to null */
		void ClearGraphs () {
			if (graphs == null) return;
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] != null) {
					graphs[i].OnDestroy();
					graphs[i].active = null;
				}
			}
			graphs = null;
			UpdateShortcuts();
		}

		public void OnDestroy () {
			ClearGraphs();
		}

		/** Deserializes graphs from the specified byte array.
		 * An error will be logged if deserialization fails.
		 */
		public void DeserializeGraphs (byte[] bytes) {
			var graphLock = AssertSafe();

			ClearGraphs();
			DeserializeGraphsAdditive(bytes);
			graphLock.Release();
		}

		/** Deserializes graphs from the specified byte array additively.
		 * An error will be logged if deserialization fails.
		 * This function will add loaded graphs to the current ones.
		 */
		public void DeserializeGraphsAdditive (byte[] bytes) {
			var graphLock = AssertSafe();

			try {
				if (bytes != null) {
					var sr = new Pathfinding.Serialization.AstarSerializer(this);

					if (sr.OpenDeserialize(bytes)) {
						DeserializeGraphsPartAdditive(sr);
						sr.CloseDeserialize();
					} else {
						Debug.Log("Invalid data file (cannot read zip).\nThe data is either corrupt or it was saved using a 3.0.x or earlier version of the system");
					}
				} else {
					throw new System.ArgumentNullException("bytes");
				}
				active.VerifyIntegrity();
			} catch (System.Exception e) {
				Debug.LogError("Caught exception while deserializing data.\n"+e);
				graphs = new NavGraph[0];
				data_backup = bytes;
			}

			UpdateShortcuts();
			graphLock.Release();
		}

		/** Deserializes common info.
		 * Common info is what is shared between the editor serialization and the runtime serializer.
		 * This is mostly everything except the graph inspectors which serialize some extra data in the editor
		 *
		 * In most cases you should use the DeserializeGraphs or DeserializeGraphsAdditive method instead.
		 */
		public void DeserializeGraphsPart (Pathfinding.Serialization.AstarSerializer sr) {
			var graphLock = AssertSafe();

			ClearGraphs();
			DeserializeGraphsPartAdditive(sr);
			graphLock.Release();
		}

		/** Deserializes common info additively
		 * Common info is what is shared between the editor serialization and the runtime serializer.
		 * This is mostly everything except the graph inspectors which serialize some extra data in the editor
		 *
		 * In most cases you should use the DeserializeGraphs or DeserializeGraphsAdditive method instead.
		 */
		public void DeserializeGraphsPartAdditive (Pathfinding.Serialization.AstarSerializer sr) {
			if (graphs == null) graphs = new NavGraph[0];

			var gr = new List<NavGraph>(graphs);

			// Set an offset so that the deserializer will load
			// the graphs with the correct graph indexes
			sr.SetGraphIndexOffset(gr.Count);

			gr.AddRange(sr.DeserializeGraphs());
			graphs = gr.ToArray();

			sr.DeserializeExtraInfo();

			//Assign correct graph indices.
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;
				graphs[i].GetNodes(node => node.GraphIndex = (uint)i);
			}

			for (int i = 0; i < graphs.Length; i++) {
				for (int j = i+1; j < graphs.Length; j++) {
					if (graphs[i] != null && graphs[j] != null && graphs[i].guid == graphs[j].guid) {
						Debug.LogWarning("Guid Conflict when importing graphs additively. Imported graph will get a new Guid.\nThis message is (relatively) harmless.");
						graphs[i].guid = Pathfinding.Util.Guid.NewGuid();
						break;
					}
				}
			}

			sr.PostDeserialization();
		}

		#endregion

		/** Find all graph types supported in this build.
		 * Using reflection, the assembly is searched for types which inherit from NavGraph. */
		public void FindGraphTypes () {
#if !ASTAR_FAST_NO_EXCEPTIONS && !UNITY_WINRT && !UNITY_WEBGL
			var assembly = WindowsStoreCompatibility.GetTypeInfo(typeof(AstarPath)).Assembly;
			System.Type[] types = assembly.GetTypes();
			var graphList = new List<System.Type>();

			foreach (System.Type type in types) {
#if NETFX_CORE && !UNITY_EDITOR
				System.Type baseType = type.GetTypeInfo().BaseType;
#else
				System.Type baseType = type.BaseType;
#endif
				while (baseType != null) {
					if (System.Type.Equals(baseType, typeof(NavGraph))) {
						graphList.Add(type);

						break;
					}

#if NETFX_CORE && !UNITY_EDITOR
					baseType = baseType.GetTypeInfo().BaseType;
#else
					baseType = baseType.BaseType;
#endif
				}
			}

			graphTypes = graphList.ToArray();
#else
			graphTypes = DefaultGraphTypes;
#endif
		}

		#region GraphCreation
		/**
		 * \returns A System.Type which matches the specified \a type string. If no mathing graph type was found, null is returned
		 *
		 * \deprecated
		 */
		[System.Obsolete("If really necessary. Use System.Type.GetType instead.")]
		public System.Type GetGraphType (string type) {
			for (int i = 0; i < graphTypes.Length; i++) {
				if (graphTypes[i].Name == type) {
					return graphTypes[i];
				}
			}
			return null;
		}

		/** Creates a new instance of a graph of type \a type. If no matching graph type was found, an error is logged and null is returned
		 * \returns The created graph
		 * \see CreateGraph(System.Type)
		 *
		 * \deprecated
		 */
		[System.Obsolete("Use CreateGraph(System.Type) instead")]
		public NavGraph CreateGraph (string type) {
			Debug.Log("Creating Graph of type '"+type+"'");

			for (int i = 0; i < graphTypes.Length; i++) {
				if (graphTypes[i].Name == type) {
					return CreateGraph(graphTypes[i]);
				}
			}
			Debug.LogError("Graph type ("+type+") wasn't found");
			return null;
		}

		/** Creates a new graph instance of type \a type
		 * \see CreateGraph(string)
		 */
		internal NavGraph CreateGraph (System.Type type) {
			var graph = System.Activator.CreateInstance(type) as NavGraph;

			graph.active = active;
			return graph;
		}

		/** Adds a graph of type \a type to the #graphs array
		 *
		 * \deprecated
		 */
		[System.Obsolete("Use AddGraph(System.Type) instead")]
		public NavGraph AddGraph (string type) {
			NavGraph graph = null;

			for (int i = 0; i < graphTypes.Length; i++) {
				if (graphTypes[i].Name == type) {
					graph = CreateGraph(graphTypes[i]);
				}
			}

			if (graph == null) {
				Debug.LogError("No NavGraph of type '"+type+"' could be found");
				return null;
			}

			AddGraph(graph);

			return graph;
		}

		/** Adds a graph of type \a type to the #graphs array */
		public NavGraph AddGraph (System.Type type) {
			NavGraph graph = null;

			for (int i = 0; i < graphTypes.Length; i++) {
				if (System.Type.Equals(graphTypes[i], type)) {
					graph = CreateGraph(graphTypes[i]);
				}
			}

			if (graph == null) {
				Debug.LogError("No NavGraph of type '"+type+"' could be found, "+graphTypes.Length+" graph types are avaliable");
				return null;
			}

			AddGraph(graph);

			return graph;
		}

		/** Adds the specified graph to the #graphs array */
		void AddGraph (NavGraph graph) {
			// Make sure to not interfere with pathfinding
			var graphLock = AssertSafe(true);

			// Try to fill in an empty position
			bool foundEmpty = false;

			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) {
					graphs[i] = graph;
					graph.graphIndex = (uint)i;
					foundEmpty = true;
					break;
				}
			}

			if (!foundEmpty) {
				if (graphs != null && graphs.Length >= GraphNode.MaxGraphIndex) {
					throw new System.Exception("Graph Count Limit Reached. You cannot have more than " + GraphNode.MaxGraphIndex + " graphs.");
				}

				// Add a new entry to the list
				var graphList = new List<NavGraph>(graphs ?? new NavGraph[0]);
				graphList.Add(graph);
				graphs = graphList.ToArray();
				graph.graphIndex = (uint)(graphs.Length-1);
			}

			UpdateShortcuts();
			graph.active = active;
			graphLock.Release();
		}

		/** Removes the specified graph from the #graphs array and Destroys it in a safe manner.
		 * To avoid changing graph indices for the other graphs, the graph is simply nulled in the array instead
		 * of actually removing it from the array.
		 * The empty position will be reused if a new graph is added.
		 *
		 * \returns True if the graph was sucessfully removed (i.e it did exist in the #graphs array). False otherwise.
		 *
		 * \version Changed in 3.2.5 to call SafeOnDestroy before removing
		 * and nulling it in the array instead of removing the element completely in the #graphs array.
		 */
		public bool RemoveGraph (NavGraph graph) {
			// Make sure the pathfinding threads are stopped
			// If we don't wait until pathfinding that is potentially running on
			// this graph right now we could end up with NullReferenceExceptions
			var graphLock = AssertSafe();

			graph.OnDestroy();
			graph.active = null;

			int i = System.Array.IndexOf(graphs, graph);
			if (i != -1) graphs[i] = null;

			UpdateShortcuts();
			graphLock.Release();
			return i != -1;
		}

		#endregion

		#region GraphUtility

		/** Returns the graph which contains the specified node.
		 * The graph must be in the #graphs array.
		 *
		 * \returns Returns the graph which contains the node. Null if the graph wasn't found
		 */
		public static NavGraph GetGraph (GraphNode node) {
			if (node == null) return null;

			AstarPath script = AstarPath.active;
			if (script == null) return null;

			AstarData data = script.data;
			if (data == null || data.graphs == null) return null;

			uint graphIndex = node.GraphIndex;

			if (graphIndex >= data.graphs.Length) {
				return null;
			}

			return data.graphs[(int)graphIndex];
		}

		/** Returns the first graph of type \a type found in the #graphs array. Returns null if none was found */
		public NavGraph FindGraphOfType (System.Type type) {
			if (graphs != null) {
				for (int i = 0; i < graphs.Length; i++) {
					if (graphs[i] != null && System.Type.Equals(graphs[i].GetType(), type)) {
						return graphs[i];
					}
				}
			}
			return null;
		}

		/** Loop through this function to get all graphs of type 'type'
		 * \code
		 * foreach (GridGraph graph in AstarPath.data.FindGraphsOfType (typeof(GridGraph))) {
		 *     //Do something with the graph
		 * }
		 * \endcode
		 * \see AstarPath.RegisterSafeNodeUpdate */
		public IEnumerable FindGraphsOfType (System.Type type) {
			if (graphs == null) yield break;
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] != null && System.Type.Equals(graphs[i].GetType(), type)) {
					yield return graphs[i];
				}
			}
		}

		/** All graphs which implements the UpdateableGraph interface
		 * \code foreach (IUpdatableGraph graph in AstarPath.data.GetUpdateableGraphs ()) {
		 *  //Do something with the graph
		 * } \endcode
		 * \see AstarPath.RegisterSafeNodeUpdate
		 * \see Pathfinding.IUpdatableGraph */
		public IEnumerable GetUpdateableGraphs () {
			if (graphs == null) yield break;
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] is IUpdatableGraph) {
					yield return graphs[i];
				}
			}
		}

		/** All graphs which implements the UpdateableGraph interface
		 * \code foreach (IRaycastableGraph graph in AstarPath.data.GetRaycastableGraphs ()) {
		 *  //Do something with the graph
		 * } \endcode
		 * \see Pathfinding.IRaycastableGraph
		 * \deprecated Deprecated because it is not used by the package internally and the use cases are few. Iterate through the #graphs array instead.
		 */
		[System.Obsolete("Obsolete because it is not used by the package internally and the use cases are few. Iterate through the graphs array instead.")]
		public IEnumerable GetRaycastableGraphs () {
			if (graphs == null) yield break;
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] is IRaycastableGraph) {
					yield return graphs[i];
				}
			}
		}

		/** Gets the index of the NavGraph in the #graphs array */
		public int GetGraphIndex (NavGraph graph) {
			if (graph == null) throw new System.ArgumentNullException("graph");

			var index = -1;
			if (graphs != null) {
				index = System.Array.IndexOf(graphs, graph);
				if (index == -1) Debug.LogError("Graph doesn't exist");
			}
			return index;
		}

		#endregion
	}
}
