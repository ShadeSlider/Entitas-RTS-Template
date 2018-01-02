using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Pathfinding {
	/** Simple GUI utility functions */
	public static class GUIUtilityx {
		static Stack<Color> colors = new Stack<Color>();

		public static void PushTint (Color tint) {
			colors.Push(GUI.color);
			GUI.color *= tint;
		}

		public static void PopTint () {
			GUI.color = colors.Pop();
		}
	}

	/** Editor helper for hiding and showing a group of GUI elements.
	 * Call order in OnInspectorGUI should be:
	 * - Begin
	 * - Header/HeaderLabel (optional)
	 * - BeginFade
	 * - [your gui elements] (if BeginFade returns true)
	 * - End
	 */
	public class FadeArea {
		Rect lastRect;
		float value;
		float lastUpdate;
		GUIStyle labelStyle;
		GUIStyle areaStyle;
		bool visible;
		Editor editor;

		/** Is this area open.
		 * This is not the same as if any contents are visible, use #BeginFade for that.
		 */
		public bool open;

		public static bool fancyEffects;
		const float animationSpeed = 100f;

		public FadeArea (bool open, Editor editor, GUIStyle areaStyle, GUIStyle labelStyle = null) {
			this.areaStyle = areaStyle;
			this.labelStyle = labelStyle;
			this.editor = editor;
			visible = this.open = open;
			value = open ? 1 : 0;
		}

		void Tick () {
			if (Event.current.type == EventType.Repaint) {
				float deltaTime = Time.realtimeSinceStartup-lastUpdate;

				// Right at the start of a transition the deltaTime will
				// not be reliable, so use a very small value instead
				// until the next repaint
				if (value == 0f || value == 1f) deltaTime = 0.001f;
				deltaTime = Mathf.Clamp(deltaTime, 0.00001F, 0.1F);

				// Larger regions fade slightly slower
				deltaTime /= Mathf.Sqrt(Mathf.Max(lastRect.height, 100));

				lastUpdate = Time.realtimeSinceStartup;


				float targetValue = open ? 1F : 0F;
				if (!Mathf.Approximately(targetValue, value)) {
					value += deltaTime*animationSpeed*Mathf.Sign(targetValue-value);
					value = Mathf.Clamp01(value);
					editor.Repaint();

					if (!fancyEffects) {
						value = targetValue;
					}
				} else {
					value = targetValue;
				}
			}
		}

		public void Begin () {
			if (areaStyle != null) {
				lastRect = EditorGUILayout.BeginVertical(areaStyle);
			} else {
				lastRect = EditorGUILayout.BeginVertical();
			}
		}

		public void HeaderLabel (string label) {
			GUILayout.Label(label, labelStyle);
		}

		public void Header (string label) {
			Header(label, ref open);
		}

		public void Header (string label, ref bool open) {
			if (GUILayout.Button(label, labelStyle)) {
				open = !open;
				editor.Repaint();
			}
			this.open = open;
		}

		/** Hermite spline interpolation */
		static float Hermite (float start, float end, float value) {
			return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
		}

		public bool BeginFade () {
			var hermite = Hermite(0, 1, value);

			visible = EditorGUILayout.BeginFadeGroup(hermite);
			GUIUtilityx.PushTint(new Color(1, 1, 1, hermite));
			Tick();

			// Another vertical group is necessary to work around
			// a kink of the BeginFadeGroup implementation which
			// causes the padding to change when value!=0 && value!=1
			EditorGUILayout.BeginVertical();

			return visible;
		}

		public void End () {
			EditorGUILayout.EndVertical();

			if (visible) {
				// Some space that cannot be placed in the GUIStyle unfortunately
				GUILayout.Space(4);

				EditorGUILayout.EndFadeGroup();
			}

			EditorGUILayout.EndVertical();
			GUIUtilityx.PopTint();
		}
	}
	/** Handles fading effects and also some custom GUI functions such as LayerMaskField */
	public static class EditorGUILayoutx {
		static Dictionary<int, string[]> layerNames = new Dictionary<int, string[]>();
		static long lastUpdateTick;

		/** Tag names and an additional 'Edit Tags...' entry.
		 * Used for SingleTagField
		 */
		static string[] tagNamesAndEditTagsButton;

		/** Last time tagNamesAndEditTagsButton was updated.
		 * Uses EditorApplication.timeSinceStartup
		 */
		static double timeLastUpdatedTagNames;

		public static int TagField (string label, int value) {
			// Make sure the tagNamesAndEditTagsButton is relatively up to date
			if (tagNamesAndEditTagsButton == null || EditorApplication.timeSinceStartup - timeLastUpdatedTagNames > 1) {
				timeLastUpdatedTagNames = EditorApplication.timeSinceStartup;
				var tagNames = AstarPath.FindTagNames();
				tagNamesAndEditTagsButton = new string[tagNames.Length+1];
				tagNames.CopyTo(tagNamesAndEditTagsButton, 0);
				tagNamesAndEditTagsButton[tagNamesAndEditTagsButton.Length-1] = "Edit Tags...";
			}

			// Tags are between 0 and 31
			value = Mathf.Clamp(value, 0, 31);

			var newValue = EditorGUILayout.IntPopup(label, value, tagNamesAndEditTagsButton, new [] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, -1 });

			// Last element corresponds to the 'Edit Tags...' entry. Open the tag editor
			if (newValue == -1) {
				AstarPathEditor.EditTags();
			} else {
				value = newValue;
			}

			return value;
		}

		public static void TagMaskField (GUIContent label, int value, System.Action<int> callback) {
			GUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(label, EditorStyles.layerMaskField);

			string[] tagNames = AstarPath.FindTagNames();

			string text;
			if (value == 0) {
				text = "Nothing";
			} else if (value == ~0) {
				text = "Everything";
			} else {
				text = "";
				for (int i = 0; i < 32; i++) {
					if (((value >> i) & 0x1) != 0) {
						// Add spacing between words
						if (text.Length > 0) text += ", ";

						text += tagNames[i];

						// Too long, just give up
						if (text.Length > 40) {
							text = "Mixed...";
							break;
						}
					}
				}
			}

			if (GUILayout.Button(text, EditorStyles.layerMaskField, GUILayout.ExpandWidth(true))) {
				GenericMenu.MenuFunction2 wrappedCallback = obj => callback((int)obj);

				var menu = new GenericMenu();

				menu.AddItem(new GUIContent("Everything"), value == ~0, wrappedCallback, ~0);
				menu.AddItem(new GUIContent("Nothing"), value == 0, wrappedCallback, 0);

				for (int i = 0; i < tagNames.Length; i++) {
					bool on = (value >> i & 1) != 0;
					int result = on ? value & ~(1 << i) : value | 1<<i;
					menu.AddItem(new GUIContent(tagNames[i]), on, wrappedCallback, result);
				}

				// Shortcut to open the tag editor
				menu.AddItem(new GUIContent("Edit Tag Names..."), false, AstarPathEditor.EditTags);
				menu.ShowAsContext();

				Event.current.Use();
			}

			GUILayout.EndHorizontal();
		}

		public static bool UnityTagMaskList (GUIContent label, bool foldout, List<string> tagMask) {
			if (tagMask == null) throw new System.ArgumentNullException("tagMask");
			if (EditorGUILayout.Foldout(foldout, label)) {
				EditorGUI.indentLevel++;
				GUILayout.BeginVertical();
				for (int i = 0; i < tagMask.Count; i++) {
					tagMask[i] = EditorGUILayout.TagField(tagMask[i]);
				}
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Add Tag")) tagMask.Add("Untagged");

				EditorGUI.BeginDisabledGroup(tagMask.Count == 0);
				if (GUILayout.Button("Remove Last")) tagMask.RemoveAt(tagMask.Count-1);
				EditorGUI.EndDisabledGroup();

				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				EditorGUI.indentLevel--;
				return true;
			}
			return false;
		}

		/** Displays a LayerMask field.
		 * \param label Label to display
		 * \param selected Current LayerMask
		 * \note Unity 3.5 and up will use the EditorGUILayout.MaskField instead of a custom written one.
		 */
		public static LayerMask LayerMaskField (string label, LayerMask selected) {
			if (Event.current.type == EventType.Layout && System.DateTime.UtcNow.Ticks - lastUpdateTick > 10000000L) {
				layerNames.Clear();
				lastUpdateTick = System.DateTime.UtcNow.Ticks;
			}

			string[] currentLayerNames;
			if (!layerNames.TryGetValue(selected.value, out currentLayerNames)) {
				var layers = Pathfinding.Util.ListPool<string>.Claim();

				int emptyLayers = 0;
				for (int i = 0; i < 32; i++) {
					string layerName = LayerMask.LayerToName(i);

					if (layerName != "") {
						for (; emptyLayers > 0; emptyLayers--) layers.Add("Layer "+(i-emptyLayers));
						layers.Add(layerName);
					} else {
						emptyLayers++;
						if (((selected.value >> i) & 1) != 0 && selected.value != -1) {
							for (; emptyLayers > 0; emptyLayers--) layers.Add("Layer "+(i+1-emptyLayers));
						}
					}
				}

				currentLayerNames = layerNames[selected.value] = layers.ToArray();
				Pathfinding.Util.ListPool<string>.Release(layers);
			}

			selected.value = EditorGUILayout.MaskField(label, selected.value, currentLayerNames);
			return selected;
		}
	}
}
