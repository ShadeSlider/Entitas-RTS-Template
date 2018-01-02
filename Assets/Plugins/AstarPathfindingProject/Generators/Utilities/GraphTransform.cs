using UnityEngine;

namespace Pathfinding.Util {
	/** Transforms to and from world space to a 2D movement plane.
	 * The transformation is guaranteed to be purely a rotation
	 * so no scale or offset is used. This interface is primarily
	 * used to make it easier to write movement scripts which can
	 * handle movement both in the XZ plane and in the XY plane.
	 *
	 * \see #Pathfinding.Util.GraphTransform
	 */
	public interface IMovementPlane {
		Vector2 ToPlane (Vector3 p);
		Vector2 ToPlane (Vector3 p, out float elevation);
		Vector3 ToWorld (Vector2 p, float elevation = 0);
	}

	/** Generic 3D coordinate transformation */
	public interface ITransform {
		Vector3 Transform (Vector3 position);
		Vector3 InverseTransform (Vector3 position);
	}

	/** Defines a transformation from graph space to world space.
	 * This is essentially just a simple wrapper around a matrix, but it has several utilities that are useful.
	 */
	public class GraphTransform : IMovementPlane, ITransform {
		public readonly bool identity;
		public readonly bool onlyTranslational;
		readonly bool isXY;
		readonly bool isXZ;

		readonly Matrix4x4 matrix;
		readonly Matrix4x4 inverseMatrix;
		readonly Vector3 up;
		readonly Vector3 translation;
		readonly Int3 i3translation;
		readonly Quaternion rotation;
		readonly Quaternion inverseRotation;

		public static readonly GraphTransform identityTransform = new GraphTransform(Matrix4x4.identity);

		public GraphTransform (Matrix4x4 matrix) {
			this.matrix = matrix;
			inverseMatrix = matrix.inverse;
			identity = matrix.isIdentity;
			onlyTranslational = MatrixIsTranslational(matrix);
			up = matrix.MultiplyVector(Vector3.up).normalized;
			translation = matrix.MultiplyPoint3x4(Vector3.zero);
			i3translation = (Int3)translation;

			// Extract the rotation from the matrix. This is only correct if the matrix has no skew, but we only
			// want to use it for the movement plane so as long as the Up axis is parpendicular to the Forward
			// axis everything should be ok. In fact the only case in the project when all three axes are not
			// perpendicular is when hexagon or isometric grid graphs are used, but in those cases only the
			// X and Z axes are not perpendicular.
			rotation = Quaternion.LookRotation(TransformVector(Vector3.forward), TransformVector(Vector3.up));
			inverseRotation = Quaternion.Inverse(rotation);
			// Some short circuiting code for the movement plane calculations
			isXY = rotation == Quaternion.Euler(-90, 0, 0);
			isXZ = rotation == Quaternion.Euler(0, 0, 0);
		}

		public Vector3 WorldUpAtGraphPosition (Vector3 p) {
			return up;
		}

		static bool MatrixIsTranslational (Matrix4x4 m) {
			return m.GetColumn(0) == new Vector4(1, 0, 0, 0) && m.GetColumn(1) == new Vector4(0, 1, 0, 0) && m.GetColumn(2) == new Vector4(0, 0, 1, 0) && m.m33 == 1;
		}

		public Vector3 Transform (Vector3 p) {
			if (onlyTranslational) return p + translation;
			return matrix.MultiplyPoint3x4(p);
		}

		public Vector3 TransformVector (Vector3 p) {
			if (onlyTranslational) return p;
			return matrix.MultiplyVector(p);
		}

		public void Transform (Int3[] arr) {
			if (onlyTranslational) {
				for (int i = arr.Length - 1; i >= 0; i--) arr[i] += i3translation;
			} else {
				for (int i = arr.Length - 1; i >= 0; i--) arr[i] = (Int3)matrix.MultiplyPoint3x4((Vector3)arr[i]);
			}
		}

		public void Transform (Vector3[] arr) {
			if (onlyTranslational) {
				for (int i = arr.Length - 1; i >= 0; i--) arr[i] += translation;
			} else {
				for (int i = arr.Length - 1; i >= 0; i--) arr[i] = matrix.MultiplyPoint3x4(arr[i]);
			}
		}

		public Vector3 InverseTransform (Vector3 p) {
			if (onlyTranslational) return p - translation;
			return inverseMatrix.MultiplyPoint3x4(p);
		}

		public Int3 InverseTransform (Int3 p) {
			if (onlyTranslational) return p - i3translation;
			return (Int3)inverseMatrix.MultiplyPoint3x4((Vector3)p);
		}

		public void InverseTransform (Int3[] arr) {
			for (int i = arr.Length - 1; i >= 0; i--) arr[i] = (Int3)inverseMatrix.MultiplyPoint3x4((Vector3)arr[i]);
		}

		public static GraphTransform operator * (GraphTransform lhs, Matrix4x4 rhs) {
			return new GraphTransform(lhs.matrix * rhs);
		}

		public static GraphTransform operator * (Matrix4x4 lhs, GraphTransform rhs) {
			return new GraphTransform(lhs * rhs.matrix);
		}

		public Bounds Transform (Bounds b) {
			if (onlyTranslational) return new Bounds(b.center + translation, b.size);

			var corners = ArrayPool<Vector3>.Claim(8);
			var extents = b.extents;
			corners[0] = Transform(b.center + new Vector3(extents.x, extents.y, extents.z));
			corners[1] = Transform(b.center + new Vector3(extents.x, extents.y, -extents.z));
			corners[2] = Transform(b.center + new Vector3(extents.x, -extents.y, extents.z));
			corners[3] = Transform(b.center + new Vector3(extents.x, -extents.y, -extents.z));
			corners[4] = Transform(b.center + new Vector3(-extents.x, extents.y, extents.z));
			corners[5] = Transform(b.center + new Vector3(-extents.x, extents.y, -extents.z));
			corners[6] = Transform(b.center + new Vector3(-extents.x, -extents.y, extents.z));
			corners[7] = Transform(b.center + new Vector3(-extents.x, -extents.y, -extents.z));

			var min = corners[0];
			var max = corners[0];
			for (int i = 1; i < 8; i++) {
				min = Vector3.Min(min, corners[i]);
				max = Vector3.Max(max, corners[i]);
			}
			ArrayPool<Vector3>.Release(ref corners);
			return new Bounds((min+max)*0.5f, max - min);
		}

		public Bounds InverseTransform (Bounds b) {
			if (onlyTranslational) return new Bounds(b.center - translation, b.size);

			var corners = ArrayPool<Vector3>.Claim(8);
			var extents = b.extents;
			corners[0] = InverseTransform(b.center + new Vector3(extents.x, extents.y, extents.z));
			corners[1] = InverseTransform(b.center + new Vector3(extents.x, extents.y, -extents.z));
			corners[2] = InverseTransform(b.center + new Vector3(extents.x, -extents.y, extents.z));
			corners[3] = InverseTransform(b.center + new Vector3(extents.x, -extents.y, -extents.z));
			corners[4] = InverseTransform(b.center + new Vector3(-extents.x, extents.y, extents.z));
			corners[5] = InverseTransform(b.center + new Vector3(-extents.x, extents.y, -extents.z));
			corners[6] = InverseTransform(b.center + new Vector3(-extents.x, -extents.y, extents.z));
			corners[7] = InverseTransform(b.center + new Vector3(-extents.x, -extents.y, -extents.z));

			var min = corners[0];
			var max = corners[0];
			for (int i = 1; i < 8; i++) {
				min = Vector3.Min(min, corners[i]);
				max = Vector3.Max(max, corners[i]);
			}
			ArrayPool<Vector3>.Release(ref corners);
			return new Bounds((min+max)*0.5f, max - min);
		}

		#region IMovementPlane implementation

		/** Transforms from world space to the 'ground' plane of the graph.
		 * The transformation is purely a rotation so no scale or offset is used.
		 *
		 * For a graph rotated with the rotation (-90, 0, 0) this will transform
		 * a coordinate (x,y,z) to (x,y). For a graph with the rotation (0,0,0)
		 * this will tranform a coordinate (x,y,z) to (x,z). More generally for
		 * a graph with a quaternion rotation R this will transform a vector V
		 * to R * V (i.e rotate the vector V using the rotation R).
		 */
		Vector2 IMovementPlane.ToPlane (Vector3 p) {
			// These special cases cover most graph orientations used in practice.
			// Having them here improves performance in those cases by a factor of
			// 2.5 without impacting the generic case in any significant way.
			if (isXY) return new Vector2(p.x, p.y);
			if (!isXZ) p = inverseRotation * p;
			return new Vector2(p.x, p.z);
		}

		/** Transforms from world space to the 'ground' plane of the graph.
		 * The transformation is purely a rotation so no scale or offset is used.
		 */
		Vector2 IMovementPlane.ToPlane (Vector3 p, out float elevation) {
			if (!isXZ) p = inverseRotation * p;
			elevation = p.y;
			return new Vector2(p.x, p.z);
		}

		/** Transforms from the 'ground' plane of the graph to world space.
		 * The transformation is purely a rotation so no scale or offset is used.
		 */
		Vector3 IMovementPlane.ToWorld (Vector2 p, float elevation) {
			return rotation * new Vector3(p.x, elevation, p.y);
		}

		#endregion
	}
}
