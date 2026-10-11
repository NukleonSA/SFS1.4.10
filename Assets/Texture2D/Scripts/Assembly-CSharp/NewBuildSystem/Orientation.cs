using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace NewBuildSystem
{
	[Serializable]
	public class Orientation
	{
		[HorizontalGroup(0f, 0, 0, 0)]
		[HideLabel]
		public int x = 1;

		[HorizontalGroup(0f, 0, 0, 0)]
		[HideLabel]
		public int y = 1;

		[HorizontalGroup(0f, 0, 0, 0)]
		[HideLabel]
		public int z;

		public Orientation(int flipedX, int flipedY, int rotation)
		{
			x = ((flipedX == 0) ? 1 : flipedX);
			y = ((flipedY == 0) ? 1 : flipedY);
			z = rotation;
		}

		public Orientation DeepCopy()
		{
			return new Orientation(x, y, z);
		}

		public void Rotate90()
		{
			z = (z - 15) % 360;
		}

		public void FlipX()
		{
			x = -x;
		}

		public void FlipY()
		{
			y = -y;
		}

		public bool InversedAxis()
		{
			return z % 180 != 0;
		}

		public static float operator *(float radianAngle, Orientation orientation)
		{
			if (orientation.y == -1)
			{
				radianAngle = 0f - radianAngle;
			}
			if (orientation.x == -1)
			{
				radianAngle = 0f - (radianAngle - (float)Math.PI / 2f) + (float)Math.PI / 2f;
			}
			return radianAngle + (float)orientation.z * ((float)Math.PI / 180f);
		}

		public static int operator *(int radianAngle, Orientation orientation)
		{
			if (orientation.y == -1)
			{
				radianAngle = -radianAngle;
			}
			if (orientation.x == -1)
			{
				radianAngle = -(radianAngle - 90) + 90;
			}
			return radianAngle + orientation.z;
		}

		public static Vector2 operator *(Vector2 a, Orientation orientation)
		{
			return Quaternion.Euler(0f, 0f, orientation.z) * new Vector2(a.x * (float)orientation.x, a.y * (float)orientation.y);
		}

		public static Vector3 operator *(Vector3 a, Orientation orientation)
		{
			return Quaternion.Euler(0f, 0f, orientation.z) * new Vector3(a.x * (float)orientation.x, a.y * (float)orientation.y, 0f);
		}

		public static Orientation operator -(Orientation a, Orientation b)
		{
			return new Orientation((a.x == b.x) ? 1 : (-1), (a.y == b.y) ? 1 : (-1), a.z - b.z);
		}

		public static Orientation operator +(Orientation a, Orientation b)
		{
			return new Orientation(a.x, a.y, a.z + b.z);
		}

		public static void ApplyOrientation(Transform a, Orientation b)
		{
			a.localScale = new Vector3(b.x, b.y, 1f);
			a.localEulerAngles = new Vector3(0f, 0f, b.z);
		}
	}
}
