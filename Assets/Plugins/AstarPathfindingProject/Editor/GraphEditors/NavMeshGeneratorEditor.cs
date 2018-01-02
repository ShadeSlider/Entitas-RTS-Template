using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomGraphEditor(typeof(NavMeshGraph), "Navmesh Graph")]
	public class NavMeshGraphEditor : GraphEditor {
		public override void OnInspectorGUI (NavGraph target) {
			var graph = target as NavMeshGraph;

			graph.sourceMesh = ObjectField("Source Mesh", graph.sourceMesh, typeof(Mesh), false) as Mesh;

			graph.offset = EditorGUILayout.Vector3Field("Offset", graph.offset);

			graph.rotation = EditorGUILayout.Vector3Field("Rotation", graph.rotation);

			graph.scale = EditorGUILayout.FloatField(new GUIContent("Scale", "Scale of the mesh"), graph.scale);
			graph.scale = Mathf.Abs(graph.scale) < 0.01F ? (graph.scale >= 0 ? 0.01F : -0.01F) : graph.scale;

			graph.nearestSearchOnlyXZ = EditorGUILayout.Toggle(new GUIContent("Nearest node queries in XZ space",
					"Recomended for single-layered environments.\nFaster but can be inaccurate esp. in multilayered contexts."), graph.nearestSearchOnlyXZ);

			if (graph.nearestSearchOnlyXZ && (Mathf.Abs(graph.rotation.x) > 1 || Mathf.Abs(graph.rotation.z) > 1)) {
				EditorGUILayout.HelpBox("Nearest node queries in XZ space is not recommended for rotated graphs since XZ space no longer corresponds to the ground plane", MessageType.Warning);
			}

			GUILayout.BeginHorizontal();
			GUILayout.Space(18);
			graph.showMeshSurface = GUILayout.Toggle(graph.showMeshSurface, new GUIContent("Show surface", "Toggles gizmos for drawing the surface of the mesh"), EditorStyles.miniButtonLeft);
			graph.showMeshOutline = GUILayout.Toggle(graph.showMeshOutline, new GUIContent("Show outline", "Toggles gizmos for drawing an outline of the nodes"), EditorStyles.miniButtonMid);
			graph.showNodeConnections = GUILayout.Toggle(graph.showNodeConnections, new GUIContent("Show connections", "Toggles gizmos for drawing node connections"), EditorStyles.miniButtonRight);
			GUILayout.EndHorizontal();
		}
	}
}
