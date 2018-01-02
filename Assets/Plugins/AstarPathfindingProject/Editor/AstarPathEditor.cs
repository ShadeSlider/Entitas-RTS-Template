using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace Pathfinding {
	[CustomEditor(typeof(AstarPath))]
	public class AstarPathEditor : Editor {
		/** List of all graph editors available (e.g GridGraphEditor) */
		static Dictionary<string, CustomGraphEditorAttribute> graphEditorTypes = new Dictionary<string, CustomGraphEditorAttribute>();

		/**
		 * Holds node counts for each graph to avoid calculating it every frame.
		 * Only used for visualization purposes
		 */
		static Dictionary<NavGraph, KeyValuePair<float, KeyValuePair<int, int> > > graphNodeCounts;

		/** List of all graph editors for the graphs */
		GraphEditor[] graphEditors;

		System.Type[] graphTypes {
			get {
				return script.data.graphTypes;
			}
		}

		static int lastUndoGroup = -1000;

		/** Used to make sure correct behaviour when handling undos */
		static uint ignoredChecksum;

		const string scriptsFolder = "Assets/AstarPathfindingProject";

		/** Used to show notifications to the user the first time the system is used in a project */
		static bool firstRun = true;

		#region SectionFlags

		static bool showSettings;
		static bool customAreaColorsOpen;
		static bool editTags;

		static FadeArea settingsArea;
		static FadeArea colorSettingsArea;
		static FadeArea editorSettingsArea;
		static FadeArea aboutArea;
		static FadeArea optimizationSettingsArea;
		static FadeArea serializationSettingsArea;
		static FadeArea tagsArea;
		static FadeArea graphsArea;
		static FadeArea addGraphsArea;
		static FadeArea alwaysVisibleArea;
		static FadeArea alwaysVisibleTopLevelArea;

		#endregion

		/** AstarPath instance that is being inspected */
		public AstarPath script { get; private set; }

		#region Styles

		static bool stylesLoaded;
		public static GUISkin astarSkin { get; private set; }

		static GUIStyle level0AreaStyle, level0LabelStyle;
		static GUIStyle level1AreaStyle, level1LabelStyle;

		static GUIStyle graphDeleteButtonStyle, graphInfoButtonStyle, graphGizmoButtonStyle;

		public static GUIStyle helpBox  { get; private set; }
		public static GUIStyle thinHelpBox  { get; private set; }

		#endregion


		/** Enables editor stuff. Loads graphs, reads settings and sets everything up */
		public void OnEnable () {
			script = target as AstarPath;

			// Make sure all references are set up to avoid NullReferenceExceptions
			script.ConfigureReferencesInternal();

			Undo.undoRedoPerformed += OnUndoRedoPerformed;

			// Search the assembly for graph types and graph editors
			if (graphEditorTypes == null || graphEditorTypes.Count == 0)
				FindGraphTypes();

			try {
				GetAstarEditorSettings();
			} catch (System.Exception e) {
				Debug.LogException(e);
			}

			LoadStyles();

			// Load graphs only when not playing, or in extreme cases, when data.graphs is null
			if ((!Application.isPlaying && (script.data == null || script.data.graphs == null || script.data.graphs.Length == 0)) || script.data.graphs == null) {
				LoadGraphs();
			}

			CreateFadeAreas();
		}

		void CreateFadeAreas () {
			if (settingsArea == null) {
				aboutArea                 = new FadeArea(false, this, level0AreaStyle, level0LabelStyle);
				optimizationSettingsArea  = new FadeArea(false, this, level0AreaStyle, level0LabelStyle);
				graphsArea                = new FadeArea(false, this, level0AreaStyle, level0LabelStyle);
				serializationSettingsArea = new FadeArea(false, this, level0AreaStyle, level0LabelStyle);
				alwaysVisibleTopLevelArea = new FadeArea(true, this, level0AreaStyle, level0LabelStyle);
				settingsArea              = new FadeArea(showSettings, this, level0AreaStyle, level0LabelStyle);

				addGraphsArea             = new FadeArea(false, this, level1AreaStyle, level1LabelStyle);
				colorSettingsArea         = new FadeArea(false, this, level1AreaStyle, level1LabelStyle);
				editorSettingsArea        = new FadeArea(false, this, level1AreaStyle, level1LabelStyle);
				alwaysVisibleArea         = new FadeArea(true, this, level1AreaStyle, level1LabelStyle);
				tagsArea                  = new FadeArea(editTags, this, level1AreaStyle, level1LabelStyle);
			}
		}

		/** Cleans up editor stuff */
		public void OnDisable () {
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;

			if (target == null) {
				return;
			}

			SetAstarEditorSettings();
			CheckGraphEditors();

			SaveGraphsAndUndo();
		}

		/** Reads settings frome EditorPrefs */
		void GetAstarEditorSettings () {
			FadeArea.fancyEffects = EditorPrefs.GetBool("EditorGUILayoutx.fancyEffects", true);

			// Check if this is the first run of the A* Pathfinding Project in this project
			string runBeforeProjects = EditorPrefs.GetString("AstarUsedProjects", "");

			string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));
			firstRun = !runBeforeProjects.Contains(projectName);
		}

		void SetAstarEditorSettings () {
			EditorPrefs.SetBool("EditorGUILayoutx.fancyEffects", FadeArea.fancyEffects);
		}

		/** Checks if JS support is enabled. This is done by checking if the directory 'Assets/AstarPathfindingEditor/Editor' exists */
		static bool IsJsEnabled () {
			return System.IO.Directory.Exists(Application.dataPath+"/AstarPathfindingEditor/Editor");
		}

		/** Enables JS support. This is done by restructuring folders in the project */
		static void EnableJs () {
			// Path to the project folder (with /Assets at the end)
			string projectPath = Application.dataPath;

			if (projectPath.EndsWith("/Assets")) {
				projectPath = projectPath.Remove(projectPath.Length-("Assets".Length));
			}

			if (!System.IO.Directory.Exists(projectPath + scriptsFolder)) {
				string error = "Could not enable Js support. AstarPathfindingProject folder did not exist in the default location.\n" +
							   "If you get this message and the AstarPathfindingProject is not at the root of your Assets folder (i.e at Assets/AstarPathfindingProject)" +
							   " then you should move it to the root";

				Debug.LogError(error);
				EditorUtility.DisplayDialog("Could not enable Js support", error, "ok");
				return;
			}

			if (!System.IO.Directory.Exists(Application.dataPath+"/AstarPathfindingEditor")) {
				System.IO.Directory.CreateDirectory(Application.dataPath+"/AstarPathfindingEditor");
				AssetDatabase.Refresh();
			}
			if (!System.IO.Directory.Exists(Application.dataPath+"/Plugins")) {
				System.IO.Directory.CreateDirectory(Application.dataPath+"/Plugins");
				AssetDatabase.Refresh();
			}


			AssetDatabase.MoveAsset(scriptsFolder + "/Editor", "Assets/AstarPathfindingEditor/Editor");
			AssetDatabase.MoveAsset(scriptsFolder, "Assets/Plugins/AstarPathfindingProject");
			AssetDatabase.Refresh();
		}

		/** Disables JS support if it was enabled. This is done by restructuring folders in the project */
		static void DisableJs () {
			if (System.IO.Directory.Exists(Application.dataPath+"/Plugins/AstarPathfindingProject")) {
				string error = AssetDatabase.MoveAsset("Assets/Plugins/AstarPathfindingProject", scriptsFolder);
				if (error != "") {
					Debug.LogError("Couldn't disable Js - "+error);
				} else {
					try {
						System.IO.Directory.Delete(Application.dataPath+"/Plugins");
					} catch (System.Exception) {}
				}
			} else {
				Debug.LogWarning("Could not disable JS - Could not find directory '"+Application.dataPath+"/Plugins/AstarPathfindingProject'");
			}

			if (System.IO.Directory.Exists(Application.dataPath+"/AstarPathfindingEditor/Editor")) {
				string error = AssetDatabase.MoveAsset("Assets/AstarPathfindingEditor/Editor", scriptsFolder + "/Editor");
				if (error != "") {
					Debug.LogError("Couldn't disable Js - "+error);
				} else {
					try {
						System.IO.Directory.Delete(Application.dataPath+"/AstarPathfindingEditor");
					} catch (System.Exception) {}
				}
			} else {
				Debug.LogWarning("Could not disable JS - Could not find directory '"+Application.dataPath+"/AstarPathfindingEditor/Editor'");
			}

			AssetDatabase.Refresh();
		}

		/** Discards the first run window.
		 * It will not be shown for this project again */
		static void DiscardFirstRun () {
			string runBeforeProjects = EditorPrefs.GetString("AstarUsedProjects", "");

			string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));

			if (!runBeforeProjects.Contains(projectName)) {
				runBeforeProjects += "|"+projectName;
			}
			EditorPrefs.SetString("AstarUsedProjects", runBeforeProjects);

			firstRun = false;
		}

		/** Repaints Scene View.
		 * \warning Uses Undocumented Unity Calls (should be safe for Unity 3.x though) */
		void RepaintSceneView () {
			if (!Application.isPlaying || EditorApplication.isPaused) SceneView.RepaintAll();
		}

		/** Tell Unity that we want to use the whole inspector width */
		public override bool UseDefaultMargins () {
			return false;
		}

		public override void OnInspectorGUI () {
			//Do some loading and checking
			if (!stylesLoaded) {
				if (!LoadStyles()) {
					EditorGUILayout.HelpBox("The GUISkin 'AstarEditorSkin.guiskin' in the folder "+EditorResourceHelper.editorAssets+"/ was not found or some custom styles in it does not exist.\n"+
						"This file is required for the A* Pathfinding Project editor.\n\n"+
						"If you are trying to add A* to a new project, please do not copy the files outside Unity, "+
						"export them as a UnityPackage and import them to this project or download the package from the Asset Store"+
						"or the 'scripts only' package from the A* Pathfinding Project website.\n\n\n"+
						"Skin loading is done in the AstarPathEditor.cs --> LoadStyles method", MessageType.Error);
					return;
				}
			}

			bool preChanged = GUI.changed;
			GUI.changed = false;

			Undo.RecordObject(script, "A* inspector");

			CheckGraphEditors();

			//End loading and checking

			EditorGUI.indentLevel = 1;

			// Apparently these can sometimes get eaten by unity components
			// so I catch them here for later use
			EventType storedEventType = Event.current.type;
			string storedEventCommand = Event.current.commandName;

			DrawMainArea();

			GUILayout.Space(5);

			if (GUILayout.Button(new GUIContent("Scan", "Recaculate all graphs. Shortcut cmd+alt+s ( ctrl+alt+s on windows )"))) {
				MenuScan();
			}



			// Handle undo
			SaveGraphsAndUndo(storedEventType, storedEventCommand);

			GUI.changed = preChanged || GUI.changed;

			if (GUI.changed) {
				RepaintSceneView();
				EditorUtility.SetDirty(script);
			}
		}

		/** Loads GUISkin and sets up styles.
		 * \see EditorResourceHelper.LocateEditorAssets
		 * \returns True if all styles were found, false if there was an error somewhere
		 */
		static bool LoadStyles () {
			// Dummy styles in case the loading fails
			var inspectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

			if (!EditorResourceHelper.LocateEditorAssets()) {
				return false;
			}

			var skinPath = EditorResourceHelper.editorAssets + "/AstarEditorSkin" + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".guiskin";
			astarSkin = AssetDatabase.LoadAssetAtPath(skinPath, typeof(GUISkin)) as GUISkin;

			if (astarSkin != null) {
				astarSkin.button = inspectorSkin.button;
			} else {
				Debug.LogWarning("Could not load editor skin at '" + skinPath + "'");
				return false;
			}

			level0AreaStyle = astarSkin.FindStyle("PixelBox");

			// If the first style is null, then the rest are likely corrupted as well
			// Probably due to the user not copying meta files
			if (level0AreaStyle == null) {
				return false;
			}

			level1LabelStyle = astarSkin.FindStyle("BoxHeader");
			level0LabelStyle = astarSkin.FindStyle("TopBoxHeader");

			level1AreaStyle = astarSkin.FindStyle("PixelBox3");
			graphDeleteButtonStyle = astarSkin.FindStyle("PixelButton");
			graphInfoButtonStyle = astarSkin.FindStyle("InfoButton");
			graphGizmoButtonStyle = astarSkin.FindStyle("GizmoButton");

			helpBox = inspectorSkin.FindStyle("HelpBox") ?? inspectorSkin.box;

			thinHelpBox = new GUIStyle(helpBox);
			thinHelpBox.stretchWidth = false;
			thinHelpBox.clipping = TextClipping.Overflow;
			thinHelpBox.overflow.bottom += 2;

			stylesLoaded = true;
			return true;
		}

		/** Draws the first run dialog.
		 * Asks if the user wants to enable JS support
		 */
		void DrawFirstRun () {
			if (!firstRun) {
				return;
			}

			if (IsJsEnabled()) {
				DiscardFirstRun();
				return;
			}

			alwaysVisibleTopLevelArea.Begin();
			alwaysVisibleTopLevelArea.HeaderLabel("Do you want to enable Javascript support?");
			alwaysVisibleTopLevelArea.BeginFade();

			EditorGUILayout.HelpBox("Folders can be restructured to enable pathfinding calls from Js\n" +
				"This setting can be changed later in Settings->Editor", MessageType.Info);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Yes")) EnableJs();
			if (GUILayout.Button("No")) DiscardFirstRun();
			GUILayout.EndHorizontal();
			alwaysVisibleTopLevelArea.End();
		}

		/** Draws the main area in the inspector */
		void DrawMainArea () {
			DrawFirstRun();

			CheckGraphEditors();

			graphsArea.Begin();
			graphsArea.Header("Graphs", ref script.showGraphs);

			if (graphsArea.BeginFade()) {
				bool anyNonNull = false;
				for (int i = 0; i < script.graphs.Length; i++) {
					if (script.graphs[i] != null) {
						anyNonNull = true;
						DrawGraph(graphEditors[i]);
					}
				}

				// Draw the Add Graph button
				addGraphsArea.Begin();
				addGraphsArea.open |= !anyNonNull;
				addGraphsArea.Header("Add New Graph");

				if (addGraphsArea.BeginFade()) {
					if (graphTypes == null) script.data.FindGraphTypes();
					for (int i = 0; i < graphTypes.Length; i++) {
						if (graphEditorTypes.ContainsKey(graphTypes[i].Name)) {
							if (GUILayout.Button(graphEditorTypes[graphTypes[i].Name].displayName)) {
								addGraphsArea.open = false;
								AddGraph(graphTypes[i]);
							}
						} else if (!graphTypes[i].Name.Contains("Base")) {
							EditorGUI.BeginDisabledGroup(true);
							GUILayout.Label(graphTypes[i].Name + " (no editor found)", "Button");
							EditorGUI.EndDisabledGroup();
						}
					}
				}
				addGraphsArea.End();

				if (script.data.data_backup != null && script.data.data_backup.Length != 0) {
					alwaysVisibleArea.Begin();
					alwaysVisibleArea.HeaderLabel("Backup data detected");
					if (alwaysVisibleArea.BeginFade()) {
						EditorGUILayout.HelpBox("Backup data was found, this can have been stored because there was an error during deserialization. Check the log.\n" +
							"If you load again and everything goes well, you can discard the backup data\n" +
							"When trying to load again, the deserializer will ignore version differences (for example 3.0 would try to load 3.0.1 files)\n" +
							"The backup data is stored in AstarData.data_backup", MessageType.Warning);
						GUILayout.BeginHorizontal();
						if (GUILayout.Button("Try loading data again")) {
							if (script.data.graphs == null || script.data.graphs.Length == 0
								|| EditorUtility.DisplayDialog("Do you want to load from backup data?",
									"Are you sure you want to load from backup data?\nThis will delete your current graphs.", "Yes", "Cancel")) {
								script.data.SetData(script.data.data_backup);
								LoadGraphs();
							}
						}
						if (GUILayout.Button("Discard backup data")) {
							script.data.data_backup = null;
						}
						GUILayout.EndHorizontal();
					}
					alwaysVisibleArea.End();
				}
			}

			graphsArea.End();

			DrawSettings();
			DrawSerializationSettings();
			DrawOptimizationSettings();
			DrawAboutArea();

			bool showNavGraphs = EditorGUILayout.Toggle("Show Graphs", script.showNavGraphs);
			if (script.showNavGraphs != showNavGraphs) {
				script.showNavGraphs = showNavGraphs;
				RepaintSceneView();
			}
		}

		/** Draws optimizations settings.
		 * \astarpro
		 */
		void DrawOptimizationSettings () {
			optimizationSettingsArea.Begin();
			optimizationSettingsArea.Header("Optimization");

			if (optimizationSettingsArea.BeginFade()) {
				GUIUtilityx.PushTint(Color.Lerp(Color.yellow, Color.white, 0.5F));
				if (GUILayout.Button("Optimizations is an A* Pathfinding Project Pro only feature\nThe Pro version can be bought on the A* Pathfinding Project homepage, click here for info", helpBox)) {
					Application.OpenURL(AstarUpdateChecker.GetURL("astarpro"));
				}
				GUIUtilityx.PopTint();
			}

			optimizationSettingsArea.End();
		}

		/** Returns a version with all fields fully defined.
		 * This is used because by default new Version(3,0,0) > new Version(3,0).
		 * This is not the desired behaviour so we make sure that all fields are defined here
		 */
		public static System.Version FullyDefinedVersion (System.Version v) {
			return new System.Version(Mathf.Max(v.Major, 0), Mathf.Max(v.Minor, 0), Mathf.Max(v.Build, 0), Mathf.Max(v.Revision, 0));
		}

		void DrawAboutArea () {
			System.Version newVersion = AstarUpdateChecker.latestVersion;
			bool beta = false;

			// Check if either the latest release version or the latest beta version is newer than this version
			if (FullyDefinedVersion(AstarUpdateChecker.latestVersion) > FullyDefinedVersion(AstarPath.Version) || FullyDefinedVersion(AstarUpdateChecker.latestBetaVersion) > FullyDefinedVersion(AstarPath.Version)) {
				if (FullyDefinedVersion(AstarUpdateChecker.latestVersion) <= FullyDefinedVersion(AstarPath.Version)) {
					newVersion = AstarUpdateChecker.latestBetaVersion;
					beta = true;
				}
			}

			aboutArea.Begin();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("About", level0LabelStyle)) {
				aboutArea.open = !aboutArea.open;
				GUI.changed = true;
			}

			// Check if the latest version is newer than this version
			if (FullyDefinedVersion(newVersion) > FullyDefinedVersion(AstarPath.Version)) {
				GUIUtilityx.PushTint(Color.green);
				if (GUILayout.Button((beta ? "Beta" : "New") + " Version Available! "+newVersion, thinHelpBox, GUILayout.Height(15))) {
					Application.OpenURL(AstarUpdateChecker.GetURL("download"));
				}
				GUIUtilityx.PopTint();
				GUILayout.Space(20);
			}

			GUILayout.EndHorizontal();

			if (aboutArea.BeginFade()) {
				GUILayout.Label("The A* Pathfinding Project was made by Aron Granberg\nYour current version is "+AstarPath.Version);

				if (FullyDefinedVersion(newVersion) > FullyDefinedVersion(AstarPath.Version)) {
					EditorGUILayout.HelpBox("A new "+(beta ? "beta " : "")+"version of the A* Pathfinding Project is available, the new version is "+
						newVersion, MessageType.Info);

					if (GUILayout.Button("What's new?")) {
						Application.OpenURL(AstarUpdateChecker.GetURL(beta ? "beta_changelog" : "changelog"));
					}

					if (GUILayout.Button("Click here to find out more")) {
						Application.OpenURL(AstarUpdateChecker.GetURL("findoutmore"));
					}

					GUIUtilityx.PushTint(new Color(0.3F, 0.9F, 0.3F));

					if (GUILayout.Button("Download new version")) {
						Application.OpenURL(AstarUpdateChecker.GetURL("download"));
					}

					GUIUtilityx.PopTint();
				}

				if (GUILayout.Button(new GUIContent("Documentation", "Open the documentation for the A* Pathfinding Project"))) {
					Application.OpenURL(AstarUpdateChecker.GetURL("documentation"));
				}

				if (GUILayout.Button(new GUIContent("Project Homepage", "Open the homepage for the A* Pathfinding Project"))) {
					Application.OpenURL(AstarUpdateChecker.GetURL("homepage"));
				}
			}

			aboutArea.End();
		}

		void DrawGraphHeader (GraphEditor graphEditor) {
			var graph = graphEditor.target;

			// Graph guid, just used to get a unique value
			string graphGUIDString = graph.guid.ToString();

			GUILayout.BeginHorizontal();

			GUI.SetNextControlName(graphGUIDString);
			graph.name = GUILayout.TextField(graph.name ?? "", level1LabelStyle, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

			// If the graph name text field is not focused and the graph name is empty, then fill it in
			if (graph.name == "" && Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != graphGUIDString) {
				graph.name = graphEditorTypes[graph.GetType().Name].displayName;
			}

			if (GUILayout.Button("", level1LabelStyle)) {
				graphEditor.fadeArea.open = graph.open = !graph.open;
				if (!graph.open) {
					graph.infoScreenOpen = false;
				}
				RepaintSceneView();
			}

			if (script.prioritizeGraphs) {
				var moveUp = GUILayout.Button(new GUIContent("Up", "Increase the graph priority"), GUILayout.Width(40));
				var moveDown = GUILayout.Button(new GUIContent("Down", "Decrease the graph priority"), GUILayout.Width(40));

				if (moveUp || moveDown) {
					int index = script.data.GetGraphIndex(graph);

					int next;
					if (moveUp) {
						// Find the previous non null graph
						next = index-1;
						for (; next >= 0; next--) if (script.graphs[next] != null) break;
					} else {
						// Find the next non null graph
						next = index+1;
						for (; next < script.graphs.Length; next++) if (script.graphs[next] != null) break;
					}

					if (next >= 0 && next < script.graphs.Length) {
						NavGraph tmp = script.graphs[next];
						script.graphs[next] = graph;
						script.graphs[index] = tmp;

						GraphEditor tmpEditor = graphEditors[next];
						graphEditors[next] = graphEditors[index];
						graphEditors[index] = tmpEditor;
					}
					CheckGraphEditors();
					Repaint();
				}
			}

			bool drawGizmos = GUILayout.Toggle(graph.drawGizmos, "Draw Gizmos", graphGizmoButtonStyle);
			if (drawGizmos != graph.drawGizmos) {
				graph.drawGizmos = drawGizmos;

				// Make sure that the scene view is repainted when gizmos are toggled on or off
				RepaintSceneView();
			}

			if (GUILayout.Toggle(graph.infoScreenOpen, "Info", graphInfoButtonStyle)) {
				if (!graph.infoScreenOpen) {
					graphEditor.infoFadeArea.open = graph.infoScreenOpen = true;
					graphEditor.fadeArea.open = graph.open = true;
				}
			} else {
				graphEditor.infoFadeArea.open = graph.infoScreenOpen = false;
			}

			if (GUILayout.Button("Delete", graphDeleteButtonStyle)) {
				RemoveGraph(graph);
			}
			GUILayout.EndHorizontal();
		}

		void DrawGraphInfoArea (GraphEditor graphEditor) {
			graphEditor.infoFadeArea.Begin();

			if (graphEditor.infoFadeArea.BeginFade()) {
				bool anyNodesNull = false;
				int total = 0;
				int numWalkable = 0;

				// Calculate number of nodes in the graph
				KeyValuePair<float, KeyValuePair<int, int> > pair;
				graphNodeCounts = graphNodeCounts ?? new Dictionary<NavGraph, KeyValuePair<float, KeyValuePair<int, int> > >();

				if (!graphNodeCounts.TryGetValue(graphEditor.target, out pair) || (Time.realtimeSinceStartup-pair.Key) > 2) {
					graphEditor.target.GetNodes(node => {
						if (node == null) {
							anyNodesNull = true;
						} else {
							total++;
							if (node.Walkable) numWalkable++;
						}
					});
					pair = new KeyValuePair<float, KeyValuePair<int, int> >(Time.realtimeSinceStartup, new KeyValuePair<int, int>(total, numWalkable));
					graphNodeCounts[graphEditor.target] = pair;
				}

				total = pair.Value.Key;
				numWalkable = pair.Value.Value;


				EditorGUI.indentLevel++;

				if (anyNodesNull) {
					Debug.LogError("Some nodes in the graph are null. Please report this error.");
				}

				EditorGUILayout.LabelField("Nodes", total.ToString());
				EditorGUILayout.LabelField("Walkable", numWalkable.ToString());
				EditorGUILayout.LabelField("Unwalkable", (total-numWalkable).ToString());
				if (total == 0) EditorGUILayout.HelpBox("The number of nodes in the graph is zero. The graph might not be scanned", MessageType.Info);

				EditorGUI.indentLevel--;
			}

			graphEditor.infoFadeArea.End();
		}

		/** Draws the inspector for the given graph with the given graph editor */
		void DrawGraph (GraphEditor graphEditor) {
			var graph = graphEditor.target;

			graphEditor.fadeArea.Begin();
			DrawGraphHeader(graphEditor);

			if (graphEditor.fadeArea.BeginFade()) {
				DrawGraphInfoArea(graphEditor);
				graphEditor.OnInspectorGUI(graph);
				graphEditor.OnBaseInspectorGUI(graph);
			}

			graphEditor.fadeArea.End();
		}

		public void OnSceneGUI () {
			script = target as AstarPath;

			// OnSceneGUI may be called from EditorUtility.DisplayProgressBar
			// which is called repeatedly while the graphs are scanned in the
			// editor. However running the OnSceneGUI method while the graphs
			// are being scanned is a bad idea since it can interfere with
			// scanning, especially by serializing changes
			if (script.isScanning) {
				return;
			}

			script.ConfigureReferencesInternal();
			EditorGUI.BeginChangeCheck();

			if (!stylesLoaded) {
				LoadStyles();
				return;
			}

			// Some GUI controls might change this to Used, so we need to grab it here
			EventType et = Event.current.type;

			CheckGraphEditors();
			for (int i = 0; i < script.graphs.Length; i++) {
				NavGraph graph = script.graphs[i];
				if (graph != null) {
					graphEditors[i].OnSceneGUI(graph);
				}
			}

			SaveGraphsAndUndo(et);

			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(target);
			}
		}

		TextAsset SaveGraphData (byte[] bytes, TextAsset target = null) {
			string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath) + "/";

			string path;

			if (target != null) {
				path = AssetDatabase.GetAssetPath(target);
			} else {
				// Find a valid file name
				int i = 0;
				do {
					path = "Assets/GraphCaches/GraphCache" + (i == 0 ? "" : i.ToString()) + ".bytes";
					i++;
				} while (System.IO.File.Exists(projectPath+path));
			}

			string fullPath = projectPath + path;
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
			var fileInfo = new System.IO.FileInfo(fullPath);
			// Make sure we can write to the file
			if (fileInfo.Exists && fileInfo.IsReadOnly)
				fileInfo.IsReadOnly = false;
			System.IO.File.WriteAllBytes(fullPath, bytes);

			AssetDatabase.Refresh();
			return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
		}

		void DrawSerializationSettings () {
			serializationSettingsArea.Begin();
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Save & Load", level0LabelStyle)) {
				serializationSettingsArea.open = !serializationSettingsArea.open;
			}

			if (script.data.cacheStartup && script.data.file_cachedStartup != null) {
				GUIUtilityx.PushTint(Color.yellow);
				GUILayout.Label("Startup cached", thinHelpBox, GUILayout.Height(15));
				GUILayout.Space(20);
				GUIUtilityx.PopTint();
			}

			GUILayout.EndHorizontal();

			// This displays the serialization settings
			if (serializationSettingsArea.BeginFade()) {
				script.data.cacheStartup = EditorGUILayout.Toggle(new GUIContent("Cache startup", "If enabled, will cache the graphs so they don't have to be scanned at startup"), script.data.cacheStartup);

				script.data.file_cachedStartup = EditorGUILayout.ObjectField(script.data.file_cachedStartup, typeof(TextAsset), false) as TextAsset;

				if (script.data.cacheStartup && script.data.file_cachedStartup == null) {
					EditorGUILayout.HelpBox("No cache has been generated", MessageType.Error);
				}

				if (script.data.cacheStartup && script.data.file_cachedStartup != null) {
					EditorGUILayout.HelpBox("All graph settings will be replaced with the ones from the cache when the game starts", MessageType.Info);
				}

				GUILayout.BeginHorizontal();

				if (GUILayout.Button("Generate cache")) {
					var serializationSettings = new Pathfinding.Serialization.SerializeSettings();
					serializationSettings.nodes = true;

					if (EditorUtility.DisplayDialog("Scan before generating cache?", "Do you want to scan the graphs before saving the cache.\n" +
							"If the graphs have not been scanned then the cache may not contain node data and then the graphs will have to be scanned at startup anyway.", "Scan", "Don't scan")) {
						MenuScan();
					}

					// Save graphs
					var bytes = script.data.SerializeGraphs(serializationSettings);

					// Store it in a file
					script.data.file_cachedStartup = SaveGraphData(bytes, script.data.file_cachedStartup);
					script.data.cacheStartup = true;
				}

				if (GUILayout.Button("Load from cache")) {
					if (EditorUtility.DisplayDialog("Are you sure you want to load from cache?", "Are you sure you want to load graphs from the cache, this will replace your current graphs?", "Yes", "Cancel")) {
						script.data.LoadFromCache();
					}
				}

				GUILayout.EndHorizontal();

				if (script.data.data_cachedStartup != null && script.data.data_cachedStartup.Length > 0) {
					EditorGUILayout.HelpBox("Storing the cached starup data on the AstarPath object has been deprecated. It is now stored " +
						"in a separate file.", MessageType.Error);

					if (GUILayout.Button("Transfer cache data to separate file")) {
						script.data.file_cachedStartup = SaveGraphData(script.data.data_cachedStartup);
						script.data.data_cachedStartup = null;
					}
				}

				GUILayout.Space(5);

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Save to file")) {
					string path = EditorUtility.SaveFilePanel("Save Graphs", "", "graph.bytes", "bytes");

					if (path != "") {
						var serializationSettings = Pathfinding.Serialization.SerializeSettings.Settings;
						if (EditorUtility.DisplayDialog("Include node data?", "Do you want to include node data in the save file. " +
								"If node data is included the graph can be restored completely without having to scan it first.", "Include node data", "Only settings")) {
							serializationSettings.nodes = true;
						}

						if (serializationSettings.nodes && EditorUtility.DisplayDialog("Scan before saving?", "Do you want to scan the graphs before saving? " +
								"\nNot scanning can cause node data to be omitted from the file if the graph is not yet scanned.", "Scan", "Don't scan")) {
							MenuScan();
						}

						uint checksum;
						var bytes = SerializeGraphs(serializationSettings, out checksum);
						Pathfinding.Serialization.AstarSerializer.SaveToFile(path, bytes);

						EditorUtility.DisplayDialog("Done Saving", "Done saving graph data.", "Ok");
					}
				}

				if (GUILayout.Button("Load from file")) {
					string path = EditorUtility.OpenFilePanel("Load Graphs", "", "");

					if (path != "") {
						byte[] bytes;
						try {
							bytes = Pathfinding.Serialization.AstarSerializer.LoadFromFile(path);
						} catch (System.Exception e) {
							Debug.LogError("Could not load from file at '"+path+"'\n"+e);
							bytes = null;
						}

						if (bytes != null) DeserializeGraphs(bytes);
					}
				}

				GUILayout.EndHorizontal();
			}

			serializationSettingsArea.End();
		}

		void DrawSettings () {
			settingsArea.Begin();
			settingsArea.Header("Settings", ref showSettings);

			if (settingsArea.BeginFade()) {
				DrawPathfindingSettings();
				DrawDebugSettings();
				DrawColorSettings();
				DrawTagSettings();
				DrawEditorSettings();
			}

			settingsArea.End();
		}

		void DrawPathfindingSettings () {
			alwaysVisibleArea.Begin();
			alwaysVisibleArea.HeaderLabel("Pathfinding");
			alwaysVisibleArea.BeginFade();

			EditorGUI.BeginDisabledGroup(Application.isPlaying);

			script.threadCount = (ThreadCount)EditorGUILayout.EnumPopup(new GUIContent("Thread Count", "Number of threads to run the pathfinding in (if any). More threads " +
					"can boost performance on multi core systems. \n" +
					"Use None for debugging or if you dont use pathfinding that much.\n " +
					"See docs for more info"), script.threadCount);

			EditorGUI.EndDisabledGroup();

			int threads = AstarPath.CalculateThreadCount(script.threadCount);
			if (threads > 0) EditorGUILayout.HelpBox("Using " + threads +" thread(s)" + (script.threadCount < 0 ? " on your machine" : "") + ".\n" +
					"The free version of the A* Pathfinding Project is limited to at most one thread.", MessageType.None);
			else EditorGUILayout.HelpBox("Using a single coroutine (no threads)" + (script.threadCount < 0 ? " on your machine" : ""), MessageType.None);

			if (script.threadCount == ThreadCount.None) {
				script.maxFrameTime = EditorGUILayout.FloatField(new GUIContent("Max Frame Time", "Max number of milliseconds to use for path calculation per frame"), script.maxFrameTime);
			} else {
				script.maxFrameTime = 10;
			}

			script.heuristic = (Heuristic)EditorGUILayout.EnumPopup("Heuristic", script.heuristic);

			if (script.heuristic == Heuristic.Manhattan || script.heuristic == Heuristic.Euclidean || script.heuristic == Heuristic.DiagonalManhattan) {
				EditorGUI.indentLevel++;
				script.heuristicScale = EditorGUILayout.FloatField("Heuristic Scale", script.heuristicScale);
				EditorGUI.indentLevel--;
			}

			script.maxNearestNodeDistance = EditorGUILayout.FloatField(new GUIContent("Max Nearest Node Distance",
					"Normally, if the nearest node to e.g the start point of a path was not walkable" +
					" a search will be done for the nearest node which is walkble. This is the maximum distance (world units) which it will serarch"),
				script.maxNearestNodeDistance);

			GUILayout.Label(new GUIContent("Advanced"), EditorStyles.boldLabel);

			DrawHeuristicOptimizationSettings();

			script.batchGraphUpdates = EditorGUILayout.Toggle(new GUIContent("Batch Graph Updates", "Limit graph updates to only run every x seconds. Can have positive impact on performance if many graph updates are done"), script.batchGraphUpdates);

			if (script.batchGraphUpdates) {
				EditorGUI.indentLevel++;
				script.graphUpdateBatchingInterval = EditorGUILayout.FloatField(new GUIContent("Update Interval (s)", "Minimum number of seconds between each graph update"), script.graphUpdateBatchingInterval);
				EditorGUI.indentLevel--;
			}

			script.prioritizeGraphs = EditorGUILayout.Toggle(new GUIContent("Prioritize Graphs", "Normally, the system will search for the closest node in all graphs and choose the closest one" +
					"but if Prioritize Graphs is enabled, the first graph which has a node closer than Priority Limit will be chosen and additional search (e.g for the closest WALKABLE node) will be carried out on that graph only"),
				script.prioritizeGraphs);
			if (script.prioritizeGraphs) {
				EditorGUI.indentLevel++;
				script.prioritizeGraphsLimit = EditorGUILayout.FloatField("Priority Limit", script.prioritizeGraphsLimit);
				EditorGUI.indentLevel--;
			}

			script.fullGetNearestSearch = EditorGUILayout.Toggle(new GUIContent("Full Get Nearest Node Search", "Forces more accurate searches on all graphs. " +
					"Normally only the closest graph in the initial fast check will perform additional searches, " +
					"if this is toggled, all graphs will do additional searches. Slower, but more accurate"), script.fullGetNearestSearch);
			script.scanOnStartup = EditorGUILayout.Toggle(new GUIContent("Scan on Awake", "Scan all graphs on Awake. If this is false, you must call AstarPath.active.Scan () yourself. Useful if you want to make changes to the graphs with code."), script.scanOnStartup);

			alwaysVisibleArea.End();
		}

		void DrawHeuristicOptimizationSettings () {
			// Pro only feature
		}

		/** Opens the A* Inspector and shows the section for editing tags */
		public static void EditTags () {
			AstarPath astar = AstarPath.active;

			if (astar == null) astar = GameObject.FindObjectOfType<AstarPath>();

			if (astar != null) {
				editTags = true;
				showSettings = true;
				Selection.activeGameObject = astar.gameObject;
			} else {
				Debug.LogWarning("No AstarPath component in the scene");
			}
		}

		void DrawTagSettings () {
			tagsArea.Begin();
			tagsArea.Header("Tag Names", ref editTags);

			if (tagsArea.BeginFade()) {
				string[] tagNames = script.GetTagNames();

				for (int i = 0; i < tagNames.Length; i++) {
					tagNames[i] = EditorGUILayout.TextField(new GUIContent("Tag "+i, "Name for tag "+i), tagNames[i]);
					if (tagNames[i] == "") tagNames[i] = ""+i;
				}
			}

			tagsArea.End();
		}

		void DrawEditorSettings () {
			editorSettingsArea.Begin();
			editorSettingsArea.Header("Editor");

			if (editorSettingsArea.BeginFade()) {
				FadeArea.fancyEffects = EditorGUILayout.Toggle("Fancy fading effects", FadeArea.fancyEffects);

				if (IsJsEnabled()) {
					if (GUILayout.Button(new GUIContent("Disable Js Support", "Revert to only enable pathfinding calls from C#"))) {
						DisableJs();
					}
				} else {
					if (GUILayout.Button(new GUIContent("Enable Js Support", "Folders can be restructured to enable pathfinding calls from Js instead of just from C#"))) {
						EnableJs();
					}
				}
			}

			editorSettingsArea.End();
		}

		static void DrawColorSlider (ref float left, ref float right, bool editable) {
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.BeginVertical();

			GUILayout.Box("", astarSkin.GetStyle("ColorInterpolationBox"));
			GUILayout.BeginHorizontal();
			if (editable) {
				left = EditorGUILayout.IntField((int)left);
			} else {
				GUILayout.Label(left.ToString("0"));
			}
			GUILayout.FlexibleSpace();
			if (editable) {
				right = EditorGUILayout.IntField((int)right);
			} else {
				GUILayout.Label(right.ToString("0"));
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUILayout.Space(4);
			GUILayout.EndHorizontal();
		}

		void DrawDebugSettings () {
			alwaysVisibleArea.Begin();
			alwaysVisibleArea.HeaderLabel("Debug");
			alwaysVisibleArea.BeginFade();

			script.logPathResults = (PathLog)EditorGUILayout.EnumPopup("Path Logging", script.logPathResults);
			script.debugMode = (GraphDebugMode)EditorGUILayout.EnumPopup("Graph Coloring", script.debugMode);

			if (script.debugMode == GraphDebugMode.G || script.debugMode == GraphDebugMode.H || script.debugMode == GraphDebugMode.F || script.debugMode == GraphDebugMode.Penalty) {
				script.manualDebugFloorRoof = !EditorGUILayout.Toggle("Automatic Limits", !script.manualDebugFloorRoof);
				DrawColorSlider(ref script.debugFloor, ref script.debugRoof, script.manualDebugFloorRoof);
			}

			script.showSearchTree = EditorGUILayout.Toggle("Show Search Tree", script.showSearchTree);
			if (script.showSearchTree) {
				EditorGUILayout.HelpBox("Show Search Tree is enabled, you may see rendering glitches in the graph rendering" +
					" while the game is running. This is nothing to worry about and is simply due to the paths being calculated at the same time as the gizmos" +
					" are being rendered. You can pause the game to see an accurate rendering.", MessageType.Info);
			}
			script.showUnwalkableNodes = EditorGUILayout.Toggle("Show Unwalkable Nodes", script.showUnwalkableNodes);

			if (script.showUnwalkableNodes) {
				EditorGUI.indentLevel++;
				script.unwalkableNodeDebugSize = EditorGUILayout.FloatField("Size", script.unwalkableNodeDebugSize);
				EditorGUI.indentLevel--;
			}

			alwaysVisibleArea.End();
		}

		void DrawColorSettings () {
			colorSettingsArea.Begin();
			colorSettingsArea.Header("Colors");

			if (colorSettingsArea.BeginFade()) {
				// Make sure the object is not null
				AstarColor colors = script.colorSettings = script.colorSettings ?? new AstarColor();

				colors._NodeConnection = EditorGUILayout.ColorField(new GUIContent("Node Connection", "Color used for node connections when 'Path Debug Mode'='Connections'"), colors._NodeConnection);
				colors._UnwalkableNode = EditorGUILayout.ColorField("Unwalkable Node", colors._UnwalkableNode);
				colors._BoundsHandles = EditorGUILayout.ColorField("Bounds Handles", colors._BoundsHandles);

				colors._ConnectionLowLerp = EditorGUILayout.ColorField("Connection Gradient (low)", colors._ConnectionLowLerp);
				colors._ConnectionHighLerp = EditorGUILayout.ColorField("Connection Gradient (high)", colors._ConnectionHighLerp);

				colors._MeshEdgeColor = EditorGUILayout.ColorField("Mesh Edge", colors._MeshEdgeColor);

				if (EditorResourceHelper.GizmoSurfaceMaterial != null && EditorResourceHelper.GizmoLineMaterial != null) {
					EditorGUI.BeginChangeCheck();
					var col1 = EditorResourceHelper.GizmoSurfaceMaterial.color;
					col1.a = EditorGUILayout.Slider("Navmesh Surface Opacity", col1.a, 0, 1);

					var col2 = EditorResourceHelper.GizmoLineMaterial.color;
					col2.a = EditorGUILayout.Slider("Navmesh Outline Opacity", col2.a, 0, 1);

					var fade = EditorResourceHelper.GizmoSurfaceMaterial.GetColor("_FadeColor");
					fade.a = EditorGUILayout.Slider("Opacity Behind Objects", fade.a, 0, 1);

					if (EditorGUI.EndChangeCheck()) {
						Undo.RecordObjects(new [] { EditorResourceHelper.GizmoSurfaceMaterial, EditorResourceHelper.GizmoLineMaterial }, "Change navmesh transparency");
						EditorResourceHelper.GizmoSurfaceMaterial.color = col1;
						EditorResourceHelper.GizmoLineMaterial.color = col2;
						EditorResourceHelper.GizmoSurfaceMaterial.SetColor("_FadeColor", fade);
						EditorResourceHelper.GizmoLineMaterial.SetColor("_FadeColor", fade * new Color(1, 1, 1, 0.7f));
					}
				}
				if (colors._AreaColors == null) {
					colors._AreaColors = new Color[0];
				}

				// Custom Area Colors
				customAreaColorsOpen = EditorGUILayout.Foldout(customAreaColorsOpen, "Custom Area Colors");
				if (customAreaColorsOpen) {
					EditorGUI.indentLevel += 2;

					for (int i = 0; i < colors._AreaColors.Length; i++) {
						GUILayout.BeginHorizontal();
						colors._AreaColors[i] = EditorGUILayout.ColorField("Area "+i+(i == 0 ? " (not used usually)" : ""), colors._AreaColors[i]);
						if (GUILayout.Button(new GUIContent("", "Reset to the default color"), astarSkin.FindStyle("SmallReset"), GUILayout.Width(20))) {
							colors._AreaColors[i] = AstarMath.IntToColor(i, 1F);
						}
						GUILayout.EndHorizontal();
					}

					GUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup(colors._AreaColors.Length > 255);

					if (GUILayout.Button("Add New")) {
						var newcols = new Color[colors._AreaColors.Length+1];
						for (int i = 0; i < colors._AreaColors.Length; i++) {
							newcols[i] = colors._AreaColors[i];
						}
						newcols[newcols.Length-1] = AstarMath.IntToColor(newcols.Length-1, 1F);
						colors._AreaColors = newcols;
					}

					EditorGUI.EndDisabledGroup();
					EditorGUI.BeginDisabledGroup(colors._AreaColors.Length == 0);

					if (GUILayout.Button("Remove last") && colors._AreaColors.Length > 0) {
						var newcols = new Color[colors._AreaColors.Length-1];
						for (int i = 0; i < colors._AreaColors.Length-1; i++) {
							newcols[i] = colors._AreaColors[i];
						}
						colors._AreaColors = newcols;
					}

					EditorGUI.EndDisabledGroup();
					GUILayout.EndHorizontal();

					EditorGUI.indentLevel -= 2;
				}

				if (GUI.changed) {
					colors.OnEnable();
				}
			}

			colorSettingsArea.End();
		}

		/** Make sure every graph has a graph editor */
		void CheckGraphEditors (bool forceRebuild = false) {
			if (forceRebuild || graphEditors == null || script.graphs == null || script.graphs.Length != graphEditors.Length) {
				if (script.data.graphs == null) {
					script.data.graphs = new NavGraph[0];
				}

				graphEditors = new GraphEditor[script.graphs.Length];

				for (int i = 0; i < script.graphs.Length; i++) {
					NavGraph graph = script.graphs[i];

					if (graph == null) continue;

					if (graph.guid == new Pathfinding.Util.Guid()) {
						graph.guid = Pathfinding.Util.Guid.NewGuid();
					}

					graphEditors[i] = CreateGraphEditor(graph);
				}
			} else {
				for (int i = 0; i < script.graphs.Length; i++) {
					if (script.graphs[i] == null) continue;

					if (graphEditors[i] == null || graphEditorTypes[script.graphs[i].GetType().Name].editorType != graphEditors[i].GetType()) {
						CheckGraphEditors(true);
						return;
					}

					if (script.graphs[i].guid == new Pathfinding.Util.Guid()) {
						script.graphs[i].guid = Pathfinding.Util.Guid.NewGuid();
					}

					graphEditors[i].target = script.graphs[i];
				}
			}
		}

		void RemoveGraph (NavGraph graph) {
			script.data.RemoveGraph(graph);
			CheckGraphEditors(true);
			GUI.changed = true;
			Repaint();
		}

		void AddGraph (System.Type type) {
			script.data.AddGraph(type);
			CheckGraphEditors();
			GUI.changed = true;
		}

		/** Creates a GraphEditor for a graph */
		GraphEditor CreateGraphEditor (NavGraph graph) {
			var graphType = graph.GetType().Name;
			GraphEditor result;

			if (graphEditorTypes.ContainsKey(graphType)) {
				result = System.Activator.CreateInstance(graphEditorTypes[graphType].editorType) as GraphEditor;
			} else {
				Debug.LogError("Couldn't find an editor for the graph type '" + graphType + "' There are " + graphEditorTypes.Count + " available graph editors");
				result = new GraphEditor();
			}

			result.editor = this;
			result.fadeArea = new FadeArea(graph.open, this, level1AreaStyle, level1LabelStyle);
			result.infoFadeArea = new FadeArea(graph.infoScreenOpen, this, null, null);
			result.target = graph;
			result.OnEnable();
			return result;
		}

		bool HandleUndo () {
			// The user has tried to undo something, apply that
			if (script.data.GetData() == null) {
				script.data.SetData(new byte[0]);
			} else {
				LoadGraphs();
				return true;
			}
			return false;
		}

		/** Hashes the contents of a byte array */
		static int ByteArrayHash (byte[] arr) {
			if (arr == null) return -1;
			int h = -1;
			for (int i = 0; i < arr.Length; i++) {
				h ^= (arr[i]^i)*3221;
			}
			return h;
		}

		void SerializeIfDataChanged () {
			uint checksum;

			byte[] bytes = SerializeGraphs(out checksum);

			int byteHash = ByteArrayHash(bytes);
			int dataHash = ByteArrayHash(script.data.GetData());
			//Check if the data is different than the previous data, use checksums
			bool isDifferent = checksum != ignoredChecksum && dataHash != byteHash;

			//Only save undo if the data was different from the last saved undo
			if (isDifferent) {
				//Assign the new data
				script.data.SetData(bytes);

				EditorUtility.SetDirty(script);
				Undo.IncrementCurrentGroup();
				Undo.RegisterCompleteObjectUndo(script, "A* Graph Settings");
			}
		}

		/** Called when an undo or redo operation has been performed */
		void OnUndoRedoPerformed () {
			if (!this) return;

			uint checksum;
			byte[] bytes = SerializeGraphs(out checksum);

			//Check if the data is different than the previous data, use checksums
			bool isDifferent = ByteArrayHash(script.data.GetData()) != ByteArrayHash(bytes);

			if (isDifferent) {
				HandleUndo();
			}

			CheckGraphEditors();
			// Deserializing a graph does not necessarily yield the same hash as the data loaded from
			// this is (probably) because editor settings are not saved all the time
			// so we explicitly ignore the new hash
			SerializeGraphs(out checksum);
			ignoredChecksum = checksum;
		}

		public void SaveGraphsAndUndo (EventType et = EventType.Used, string eventCommand = "") {
			// Serialize the settings of the graphs

			// Dont process undo events in editor, we don't want to reset graphs
			// Also don't do this if the graph is being updated as serializing the graph
			// might interfere with that (in particular it might unblock the path queue)
			if (Application.isPlaying || script.isScanning || script.IsAnyWorkItemInProgress) {
				return;
			}

			if ((Undo.GetCurrentGroup() != lastUndoGroup || et == EventType.MouseUp) && eventCommand != "UndoRedoPerformed") {
				SerializeIfDataChanged();

				lastUndoGroup = Undo.GetCurrentGroup();
			}

			if (Event.current == null || script.data.GetData() == null) {
				SerializeIfDataChanged();
				return;
			}
		}

		public void LoadGraphs () {
			//Load graphs from serialized data
			DeserializeGraphs();
		}

		public byte[] SerializeGraphs (out uint checksum) {
			var settings = Pathfinding.Serialization.SerializeSettings.Settings;

			settings.editorSettings = true;
			byte[] bytes = SerializeGraphs(settings, out checksum);
			return bytes;
		}

		public byte[] SerializeGraphs (Pathfinding.Serialization.SerializeSettings settings, out uint checksum) {
			byte[] bytes = null;
			uint ch = 0;

			// Add a work item since we cannot be sure that pathfinding (or graph updates)
			// is not running at the same time
			AstarPath.active.AddWorkItem(new AstarWorkItem(force => {
				var sr = new Pathfinding.Serialization.AstarSerializer(script.data, settings);
				sr.OpenSerialize();
				script.data.SerializeGraphsPart(sr);
				sr.SerializeEditorSettings(graphEditors);
				bytes = sr.CloseSerialize();
				ch = sr.GetChecksum();
				return true;
			}));

			// Make sure the above work item is executed immediately
			AstarPath.active.FlushWorkItems();
			checksum = ch;
			return bytes;
		}

		void DeserializeGraphs () {
			if (script.data.GetData() == null || script.data.GetData().Length == 0) {
				script.data.graphs = new NavGraph[0];
				return;
			}

			DeserializeGraphs(script.data.GetData());
		}

		void DeserializeGraphs (byte[] bytes) {
			try {
				var sr = new Pathfinding.Serialization.AstarSerializer(script.data);
				if (sr.OpenDeserialize(bytes)) {
					script.data.DeserializeGraphsPart(sr);

					// Make sure every graph has a graph editor
					CheckGraphEditors();
					sr.DeserializeEditorSettings(graphEditors);

					sr.CloseDeserialize();
				} else {
					Debug.LogWarning("Invalid data file (cannot read zip).\nThe data is either corrupt or it was saved using a 3.0.x or earlier version of the system");
					// Make sure every graph has a graph editor
					CheckGraphEditors();
				}
			} catch (System.Exception e) {
				Debug.LogError("Failed to deserialize graphs");
				Debug.LogException(e);
				script.data.data_backup = script.data.GetData();
				script.data.SetData(null);
			}
		}

		[MenuItem("Edit/Pathfinding/Scan All Graphs %&s")]
		public static void MenuScan () {
			if (AstarPath.active == null) {
				AstarPath.active = FindObjectOfType(typeof(AstarPath)) as AstarPath;
				if (AstarPath.active == null) {
					return;
				}
			}

			if (!Application.isPlaying && (AstarPath.active.data.graphs == null || AstarPath.active.data.graphTypes == null)) {
				EditorUtility.DisplayProgressBar("Scanning", "Deserializing", 0);
				AstarPath.active.data.DeserializeGraphs();
			}

			try {
				var lastMessageTime = Time.realtimeSinceStartup;
				foreach (var p in AstarPath.active.ScanAsync()) {
					// Displaying the progress bar is pretty slow, so don't do it too often
					if (Time.realtimeSinceStartup - lastMessageTime > 0.2f) {
						// Display a progress bar of the scan
						UnityEditor.EditorUtility.DisplayProgressBar("Scanning", p.description, p.progress);
						lastMessageTime = Time.realtimeSinceStartup;
					}
				}
			} catch (System.Exception e) {
				Debug.LogError("There was an error generating the graphs:\n"+e+"\n\nIf you think this is a bug, please contact me on forum.arongranberg.com (post a new thread)\n");
				EditorUtility.DisplayDialog("Error Generating Graphs", "There was an error when generating graphs, check the console for more info", "Ok");
				throw e;
			} finally {
				EditorUtility.ClearProgressBar();
			}
		}

		/** Searches in the current assembly for GraphEditor and NavGraph types */
		void FindGraphTypes () {
			graphEditorTypes = new Dictionary<string, CustomGraphEditorAttribute>();

			Assembly asm = Assembly.GetAssembly(typeof(AstarPathEditor));
			System.Type[] types = asm.GetTypes();
			var graphList = new List<System.Type>();

			// Iterate through the assembly for classes which inherit from GraphEditor
			foreach (var type in types) {
				System.Type baseType = type.BaseType;
				while (!System.Type.Equals(baseType, null)) {
					if (System.Type.Equals(baseType, typeof(GraphEditor))) {
						System.Object[] att = type.GetCustomAttributes(false);

						// Loop through the attributes for the CustomGraphEditorAttribute attribute
						foreach (System.Object attribute in att) {
							var cge = attribute as CustomGraphEditorAttribute;

							if (cge != null && !System.Type.Equals(cge.graphType, null)) {
								cge.editorType = type;
								graphList.Add(cge.graphType);
								graphEditorTypes.Add(cge.graphType.Name, cge);
							}
						}
						break;
					}

					baseType = baseType.BaseType;
				}
			}

			// Make sure graph types (not graph editor types) are also up to date
			script.data.FindGraphTypes();
		}
	}
}
