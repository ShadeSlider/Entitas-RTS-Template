using System.Collections.Generic;
using Pathfinding.Serialization;
using UnityEngine;

namespace Pathfinding {
	/** Node used for the GridGraph */
	public class GridNode : GridNodeBase {
		public GridNode (AstarPath astar) : base(astar) {
		}

		private static GridGraph[] _gridGraphs = new GridGraph[0];
		public static GridGraph GetGridGraph (uint graphIndex) { return _gridGraphs[(int)graphIndex]; }

		public static void SetGridGraph (int graphIndex, GridGraph graph) {
			if (_gridGraphs.Length <= graphIndex) {
				var gg = new GridGraph[graphIndex+1];
				for (int i = 0; i < _gridGraphs.Length; i++) gg[i] = _gridGraphs[i];
				_gridGraphs = gg;
			}

			_gridGraphs[graphIndex] = graph;
		}

		/** Internal use only */
		internal ushort InternalGridFlags {
			get { return gridFlags; }
			set { gridFlags = value; }
		}

		const int GridFlagsConnectionOffset = 0;
		const int GridFlagsConnectionBit0 = 1 << GridFlagsConnectionOffset;
		const int GridFlagsConnectionMask = 0xFF << GridFlagsConnectionOffset;

		const int GridFlagsEdgeNodeOffset = 10;
		const int GridFlagsEdgeNodeMask = 1 << GridFlagsEdgeNodeOffset;

		public override bool HasConnectionsToAllEightNeighbours {
			get {
				return (InternalGridFlags & GridFlagsConnectionMask) == GridFlagsConnectionMask;
			}
		}

		/** True if the node has a connection in the specified direction.
		 * The dir parameter corresponds to directions in the grid as:
		 * \code
		 *         Z
		 *         |
		 *         |
		 *
		 *      6  2  5
		 *       \ | /
		 * --  3 - X - 1  ----- X
		 *       / | \
		 *      7  0  4
		 *
		 *         |
		 *         |
		 * \endcode
		 *
		 * \see SetConnectionInternal
		 */
		public bool HasConnectionInDirection (int dir) {
			return (gridFlags >> dir & GridFlagsConnectionBit0) != 0;
		}

		/** True if the node has a connection in the specified direction.
		 * \deprecated Use HasConnectionInDirection
		 */
		[System.Obsolete("Use HasConnectionInDirection")]
		public bool GetConnectionInternal (int dir) {
			return HasConnectionInDirection(dir);
		}

		/** Enables or disables a connection in a specified direction on the graph.
		 *	\see HasConnectionInDirection
		 */
		public void SetConnectionInternal (int dir, bool value) {
			// Set bit number #dir to 1 or 0 depending on #value
			unchecked { gridFlags = (ushort)(gridFlags & ~((ushort)1 << GridFlagsConnectionOffset << dir) | (value ? (ushort)1 : (ushort)0) << GridFlagsConnectionOffset << dir); }
		}

		/** Sets the state of all grid connections.
		 * \param connections a bitmask of the connections (bit 0 is the first connection, bit 1 the second connection, etc.).
		 *
		 * \see SetConnectionInternal
		 */
		public void SetAllConnectionInternal (int connections) {
			unchecked { gridFlags = (ushort)((gridFlags & ~GridFlagsConnectionMask) | (connections << GridFlagsConnectionOffset)); }
		}

		/** Disables all grid connections from this node.
		 * \note Other nodes might still be able to get to this node.
		 * Therefore it is recommended to also disable the relevant connections on adjacent nodes.
		 */
		public void ResetConnectionsInternal () {
			unchecked {
				gridFlags = (ushort)(gridFlags & ~GridFlagsConnectionMask);
			}
		}

		/** Work in progress for a feature that required info about which nodes were at the border of the graph.
		 * \note This property is not functional at the moment.
		 */
		public bool EdgeNode {
			get {
				return (gridFlags & GridFlagsEdgeNodeMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsEdgeNodeMask | (value ? GridFlagsEdgeNodeMask : 0)); }
			}
		}

		public override GridNodeBase GetNeighbourAlongDirection (int direction) {
			if (HasConnectionInDirection(direction)) {
				GridGraph gg = GetGridGraph(GraphIndex);
				return gg.nodes[NodeInGridIndex+gg.neighbourOffsets[direction]];
			}
			return null;
		}

		public override void ClearConnections (bool alsoReverse) {
			if (alsoReverse) {
				// Note: This assumes that all connections are bidirectional
				// which should hold for all grid graphs unless some custom code has been added
				for (int i = 0; i < 8; i++) {
					var other = GetNeighbourAlongDirection(i) as GridNode;
					if (other != null) {
						// Remove reverse connection. See doc for GridGraph.neighbourOffsets to see which indices are used for what.
						other.SetConnectionInternal(i < 4 ? ((i + 2) % 4) : (((i-2) % 4) + 4), false);
					}
				}
			}

			ResetConnectionsInternal();

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.ClearConnections(alsoReverse);
#endif
		}

		public override void GetConnections (System.Action<GraphNode> action) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			for (int i = 0; i < 8; i++) {
				if (HasConnectionInDirection(i)) {
					GridNode other = nodes[NodeInGridIndex + neighbourOffsets[i]];
					if (other != null) action(other);
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.GetConnections(action);
#endif
		}

		public Vector3 ClosestPointOnNode (Vector3 p) {
			var gg = GetGridGraph(GraphIndex);

			// Convert to graph space
			p = gg.transform.InverseTransform(p);

			// Nodes are offset 0.5 graph space nodes
			float xf = position.x-0.5F;
			float zf = position.z-0.5f;

			// Calculate graph position of this node
			int x = NodeInGridIndex % gg.width;
			int z = NodeInGridIndex / gg.width;

			// Handle the y coordinate separately
			float y = gg.transform.InverseTransform((Vector3)position).y;

			var closestInGraphSpace = new Vector3(Mathf.Clamp(xf, x-0.5f, x+0.5f)+0.5f, y, Mathf.Clamp(zf, z-0.5f, z+0.5f)+0.5f);

			// Convert to world space
			return gg.transform.Transform(closestInGraphSpace);
		}

		public override bool GetPortal (GraphNode other, List<Vector3> left, List<Vector3> right, bool backwards) {
			if (backwards) return true;

			GridGraph gg = GetGridGraph(GraphIndex);
			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			for (int i = 0; i < 4; i++) {
				if (HasConnectionInDirection(i) && other == nodes[NodeInGridIndex + neighbourOffsets[i]]) {
					Vector3 middle = ((Vector3)(position + other.position))*0.5f;
					Vector3 cross = Vector3.Cross(gg.collision.up, (Vector3)(other.position-position));
					cross.Normalize();
					cross *= gg.nodeSize*0.5f;
					left.Add(middle - cross);
					right.Add(middle + cross);
					return true;
				}
			}

			for (int i = 4; i < 8; i++) {
				if (HasConnectionInDirection(i) && other == nodes[NodeInGridIndex + neighbourOffsets[i]]) {
					bool rClear = false;
					bool lClear = false;
					if (HasConnectionInDirection(i-4)) {
						GridNode n2 = nodes[NodeInGridIndex + neighbourOffsets[i-4]];
						if (n2.Walkable && n2.HasConnectionInDirection((i-4+1)%4)) {
							rClear = true;
						}
					}

					if (HasConnectionInDirection((i-4+1)%4)) {
						GridNode n2 = nodes[NodeInGridIndex + neighbourOffsets[(i-4+1)%4]];
						if (n2.Walkable && n2.HasConnectionInDirection(i-4)) {
							lClear = true;
						}
					}

					Vector3 middle = ((Vector3)(position + other.position))*0.5f;
					Vector3 cross = Vector3.Cross(gg.collision.up, (Vector3)(other.position-position));
					cross.Normalize();
					cross *= gg.nodeSize*1.4142f;
					left.Add(middle - (lClear ? cross : Vector3.zero));
					right.Add(middle + (rClear ? cross : Vector3.zero));
					return true;
				}
			}

			return false;
		}

		public override void FloodFill (Stack<GraphNode> stack, uint region) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;
			var index = NodeInGridIndex;

			for (int i = 0; i < 8; i++) {
				if (HasConnectionInDirection(i)) {
					GridNode other = nodes[index + neighbourOffsets[i]];
					if (other != null && other.Area != region) {
						other.Area = region;
						stack.Push(other);
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.FloodFill(stack, region);
#endif
		}

		public override void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			UpdateG(path, pathNode);
			handler.heap.Add(pathNode);

			ushort pid = handler.PathID;
			var index = NodeInGridIndex;
			for (int i = 0; i < 8; i++) {
				if (HasConnectionInDirection(i)) {
					GridNode other = nodes[index + neighbourOffsets[i]];
					PathNode otherPN = handler.GetPathNode(other);
					if (otherPN.parent == pathNode && otherPN.pathID == pid) other.UpdateRecursiveG(path, otherPN, handler);
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.UpdateRecursiveG(path, pathNode, handler);
#endif
		}


		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			GridGraph gg = GetGridGraph(GraphIndex);

			ushort pid = handler.PathID;

			{
				int[] neighbourOffsets = gg.neighbourOffsets;
				uint[] neighbourCosts = gg.neighbourCosts;
				GridNode[] nodes = gg.nodes;
				var index = NodeInGridIndex;

				for (int i = 0; i < 8; i++) {
					if (HasConnectionInDirection(i)) {
						GridNode other = nodes[index + neighbourOffsets[i]];
						if (!path.CanTraverse(other)) continue;

						PathNode otherPN = handler.GetPathNode(other);

						uint tmpCost = neighbourCosts[i];

						if (otherPN.pathID != pid) {
							otherPN.parent = pathNode;
							otherPN.pathID = pid;

							otherPN.cost = tmpCost;

							otherPN.H = path.CalculateHScore(other);
							other.UpdateG(path, otherPN);

							//Debug.Log ("G " + otherPN.G + " F " + otherPN.F);
							handler.heap.Add(otherPN);
							//Debug.DrawRay ((Vector3)otherPN.node.Position, Vector3.up,Color.blue);
						} else {
							// Sorry for the huge number of #ifs

							//If not we can test if the path from the current node to this one is a better one then the one already used

#if ASTAR_NO_TRAVERSAL_COST
							if (pathNode.G+tmpCost < otherPN.G)
#else
							if (pathNode.G+tmpCost+path.GetTraversalCost(other) < otherPN.G)
#endif
							{
								//Debug.Log ("Path better from " + NodeIndex + " to " + otherPN.node.NodeIndex + " " + (pathNode.G+tmpCost+path.GetTraversalCost(other)) + " < " + otherPN.G);
								otherPN.cost = tmpCost;

								otherPN.parent = pathNode;

								other.UpdateRecursiveG(path, otherPN, handler);

								//Or if the path from this node ("other") to the current ("current") is better
							}
#if ASTAR_NO_TRAVERSAL_COST
							else if (otherPN.G+tmpCost < pathNode.G)
#else
							else if (otherPN.G+tmpCost+path.GetTraversalCost(this) < pathNode.G)
#endif
							{
								//Debug.Log ("Path better from " + otherPN.node.NodeIndex + " to " + NodeIndex + " " + (otherPN.G+tmpCost+path.GetTraversalCost (this)) + " < " + pathNode.G);
								pathNode.parent = otherPN;
								pathNode.cost = tmpCost;

								UpdateRecursiveG(path, pathNode, handler);
							}
						}
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			base.Open(path, pathNode, handler);
#endif
		}

		public override void SerializeNode (GraphSerializationContext ctx) {
			base.SerializeNode(ctx);
			ctx.SerializeInt3(position);
			ctx.writer.Write(gridFlags);
		}

		public override void DeserializeNode (GraphSerializationContext ctx) {
			base.DeserializeNode(ctx);
			position = ctx.DeserializeInt3();
			gridFlags = ctx.reader.ReadUInt16();
		}
	}
}
