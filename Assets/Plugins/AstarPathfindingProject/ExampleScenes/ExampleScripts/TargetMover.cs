using UnityEngine;

namespace Pathfinding {
	/** Moves the target in example scenes.
	 * This is a simple script which has the sole purpose
	 * of moving the target point of agents in the example
	 * scenes for the A* Pathfinding Project.
	 *
	 * It is not meant to be pretty, but it does the job.
	 */
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_target_mover.php")]
	public class TargetMover : MonoBehaviour {
		/** Mask for the raycast placement */
		public LayerMask mask;

		public Transform target;
		AIPath[] ais2;
		AILerp[] ais3;

		/** Determines if the target position should be updated every frame or only on double-click */
		public bool onlyOnDoubleClick;
		public bool use2D;

		Camera cam;

		public void Start () {
			//Cache the Main Camera
			cam = Camera.main;
			ais2 = FindObjectsOfType<AIPath>();
			ais3 = FindObjectsOfType<AILerp>();

			useGUILayout = false;
		}

		public void OnGUI () {
			if (onlyOnDoubleClick && cam != null && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2) {
				UpdateTargetPosition();
			}
		}

		// Update is called once per frame
		void Update () {
			if (!onlyOnDoubleClick && cam != null) {
				UpdateTargetPosition();
			}
		}

		public void UpdateTargetPosition () {
			Vector3 newPosition = Vector3.zero;
			bool positionFound = false;

			if (use2D) {
				newPosition = cam.ScreenToWorldPoint(Input.mousePosition);
				newPosition.z = 0;
				positionFound = true;
			} else {
				//Fire a ray through the scene at the mouse position and place the target where it hits
				RaycastHit hit;
				if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, mask)) {
					newPosition = hit.point;
					positionFound = true;
				}
			}

			if (positionFound && newPosition != target.position) {
				target.position = newPosition;

				if (onlyOnDoubleClick) {
					if (ais2 != null) {
						for (int i = 0; i < ais2.Length; i++) {
							if (ais2[i] != null) ais2[i].SearchPath();
						}
					}

					if (ais3 != null) {
						for (int i = 0; i < ais3.Length; i++) {
							if (ais3[i] != null) ais3[i].SearchPath();
						}
					}
				}
			}
		}
	}
}
