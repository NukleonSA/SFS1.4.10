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

		// 小数倍率：有效尺寸 = |x|*Mx；必须 public 字段（JsonUtility 只存字段），0 一律视作 1 以兼容旧蓝图。
		public float sxf;

		public float syf;

		private static float SafeScale(float v)
		{
			return (v > 0f && v < 1E+09f) ? v : 1f;
		}

		/// <summary>X 方向小数倍率（1 = 原始尺寸）。</summary>
		public float Mx
		{
			get { return SafeScale(sxf); }
			set { sxf = SafeScale(value); }
		}

		/// <summary>Y 方向小数倍率（1 = 原始尺寸）。</summary>
		public float My
		{
			get { return SafeScale(syf); }
			set { syf = SafeScale(value); }
		}

		/// <summary>带符号的 X 有效尺寸（格）。飞行物理用：推力面积 / 质心偏移 / 分离力。</summary>
		public float SizeX
		{
			get { return (float)x * SafeScale(sxf); }
		}

		/// <summary>带符号的 Y 有效尺寸（格）。</summary>
		public float SizeY
		{
			get { return (float)y * SafeScale(syf); }
		}

		public Orientation(int flipedX, int flipedY, int rotation)
		{
			x = ((flipedX == 0) ? 1 : flipedX);
			y = ((flipedY == 0) ? 1 : flipedY);
			z = rotation;
			sxf = 1f;
			syf = 1f;
		}

		public Orientation DeepCopy()
		{
			Orientation orientation = new Orientation(x, y, z);
			// 带上倍率：PlacedPart 拷贝构造 / UndoStep / 从零件栏拖出 / 进世界都走这里。
			orientation.sxf = Mx;
			orientation.syf = My;
			return orientation;
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
			// 几何唯一漏斗：hitbox、连接面、旋转中心、吸附点全经此，倍率乘在这里即功能性缩放（Mx=1 与原版逐位一致）。
			return Quaternion.Euler(0f, 0f, orientation.z) * new Vector2(a.x * (float)orientation.x * orientation.Mx, a.y * (float)orientation.y * orientation.My);
		}

		public static Vector3 operator *(Vector3 a, Orientation orientation)
		{
			// 同 Vector2 版本。
			return Quaternion.Euler(0f, 0f, orientation.z) * new Vector3(a.x * (float)orientation.x * orientation.Mx, a.y * (float)orientation.y * orientation.My, 0f);
		}

		public static Orientation operator -(Orientation a, Orientation b)
		{
			Orientation orientation = new Orientation((a.x == b.x) ? 1 : (-1), (a.y == b.y) ? 1 : (-1), a.z - b.z);
			orientation.sxf = a.Mx;
			orientation.syf = a.My;
			return orientation;
		}

		public static Orientation operator +(Orientation a, Orientation b)
		{
			Orientation orientation = new Orientation(a.x, a.y, a.z + b.z);
			orientation.sxf = a.Mx;
			orientation.syf = a.My;
			return orientation;
		}

		public static void ApplyOrientation(Transform a, Orientation b)
		{
			// 视觉缩放的唯一权威：PartGrid.LoadIcon / CreateRocket / GameSaver / PartsManager 全走这里。
			a.localScale = new Vector3((float)b.x * b.Mx, (float)b.y * b.My, 1f);
			a.localEulerAngles = new Vector3(0f, 0f, b.z);
		}
	}
}
