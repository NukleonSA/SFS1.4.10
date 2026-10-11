using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class SpriteMesh : MonoBehaviour
{
	[Serializable]
	public struct Point
	{
		public Vector2 position;

		public float width;

		public float uv;

		public Color color;

		public Point(Vector2 position, float width, float uv, Color color)
		{
			this.position = position;
			this.width = width;
			this.uv = uv;
			this.color = color;
		}
	}

	public enum DrawMode
	{
		Default = 0,
		Clamp = 1,
		Repeat = 2,
		Stretch = 3
	}

	public Sprite sprite;

	public Sprite shade;

	public DrawMode drawMode;

	public float borderScale = 1f;

	public float centerOffset;

	public float tileScale = 1f;

	public Color color = Color.white;

	[Range(-1f, 1f)]
	public float cutout;

	[Range(-1f, 1f)]
	public float cutoutUV;

	public bool flipX;

	[TableList]
	public Point[] points = new Point[0];

	[HorizontalGroup(0f, 0, 0, 0)]
	public string SortingLayerName = "Default";

	[HorizontalGroup(0f, 0, 0, 0)]
	public int SortingOrder;

	private void Start()
	{
		Generate();
		GenerateCollider();
	}

	private List<Point> GetMeshPoints()
	{
		float num = points[0].uv - centerOffset;
		float num2 = points[points.Length - 1].uv - centerOffset;
		float num3 = num + GetBottomBorderSize() * borderScale;
		float num4 = num2 - GetTopBorderSize() * borderScale;
		List<Point> list = new List<Point>();
		for (int i = 0; i < points.Length - 1; i++)
		{
			if (drawMode == DrawMode.Default)
			{
				list.Add(points[i]);
				list.Add(points[i + 1]);
				continue;
			}
			float num5 = points[i].uv - centerOffset;
			float num6 = points[i + 1].uv - centerOffset;
			if (drawMode == DrawMode.Stretch || drawMode == DrawMode.Repeat || drawMode == DrawMode.Clamp)
			{
				float num7 = Mathf.Max(num5 - num, 0f);
				float num8 = Utility.OverlapAmount(num5, num6, num, num3);
				CreateSegment(num5, num8, num7, num7 + num8 / borderScale, ref list, points[i], points[i + 1]);
			}
			if (drawMode == DrawMode.Stretch)
			{
				float t = Mathf.InverseLerp(num3, num4, Mathf.Max(num5, num3));
				float t2 = Mathf.InverseLerp(num3, num4, Mathf.Max(num5, num3) + Utility.OverlapAmount(num5, num6, num3, num4));
				CreateSegment(Mathf.Max(num5, num3), Utility.OverlapAmount(num5, num6, num3, num4), Mathf.Lerp(GetBottomBorderSize(), GetEndUV(), t), Mathf.Lerp(GetBottomBorderSize(), GetEndUV(), t2), ref list, points[i], points[i + 1]);
			}
			if (drawMode == DrawMode.Repeat)
			{
				float num9 = (1f - GetBottomBorderSize() - GetTopBorderSize()) * tileScale;

				// 原版此处写死 20 块, 覆盖范围仅 20*num9:
				//   4 格以下零件够用, 但 6w/8w/10w/12w 等长零件后段会完全没有网格 -> 发虚/缺失。
				// 改为按实际需要覆盖的 UV 长度计算块数(仅影响网格几何, 不涉及贴图与阴影)。
				if (num9 <= 0.0001f)
				{
					num9 = 1f;
				}
				float repeatSpan = Mathf.Max(0f, Mathf.Min(num6, num4) - num3);
				int repeatCount = Mathf.Max(1, Mathf.CeilToInt(repeatSpan / num9) + 1);

				for (int j = 0; j < repeatCount; j++)
				{
					float num10 = Utility.OverlapAmount(num5, Mathf.Min(num6, num4), num3 + (float)j * num9, num3 + (float)(j + 1) * num9);
					float num11 = Mathf.Lerp(GetBottomBorderSize(), GetEndUV(), 0f);
					float num12 = Mathf.Lerp(GetBottomBorderSize(), GetEndUV(), 1f);

					CreateSegment(Mathf.Max(num5, num3 + (float)j * num9), num10, num11, num11 + num10 * (1f - GetBottomBorderSize() - GetTopBorderSize()) / num9, ref list, points[i], points[i + 1]);
				}
			}
			if (drawMode == DrawMode.Clamp)
			{
				float size = Utility.OverlapAmount(num5, num6, num3, GetBottomBorderSize());
				CreateSegment(Mathf.Max(num3, num5), size, GetBottomBorderSize(), GetBottomBorderSize(), ref list, points[i], points[i + 1]);
				float size2 = Utility.OverlapAmount(num5, num6, GetBottomBorderSize(), GetEndUV());
				CreateSegment(Mathf.Max(num5, GetBottomBorderSize()), size2, GetBottomBorderSize(), GetEndUV(), ref list, points[i], points[i + 1]);
				float size3 = Utility.OverlapAmount(num5, num6, GetEndUV(), num4);
				CreateSegment(Mathf.Max(GetEndUV(), num5), size3, GetEndUV(), GetEndUV(), ref list, points[i], points[i + 1]);
			}
			if (drawMode == DrawMode.Stretch || drawMode == DrawMode.Repeat || drawMode == DrawMode.Clamp)
			{
				float num13 = Mathf.Max(1f - (num2 - num5), GetEndUV());
				float num14 = Utility.OverlapAmount(num5, num6, num4, num2);
				CreateSegment(Mathf.Max(num5, num4), num14, num13, num13 + num14 / borderScale, ref list, points[i], points[i + 1]);
			}
		}
		return list;
	}

	private void CreateSegment(float start, float size, float startUv, float endUv, ref List<Point> points, Point pointA, Point pointB)
	{
		if (!(size <= 0f))
		{
			float t = (start - (pointA.uv - centerOffset)) / (pointB.uv - pointA.uv);
			points.Add(new Point(Vector2.Lerp(pointA.position, pointB.position, t), Mathf.Lerp(pointA.width, pointB.width, t), startUv, pointA.color));
			float t2 = (start + size - (pointA.uv - centerOffset)) / (pointB.uv - pointA.uv);
			points.Add(new Point(Vector2.Lerp(pointA.position, pointB.position, t2), Mathf.Lerp(pointA.width, pointB.width, t2), endUv, Color.clear));
		}
	}

	private void UpdateOrientation()
	{
		if (Ref.currentScene != Ref.SceneType.Game)
		{
			return;
		}
		Transform parent = base.transform.parent;
		Part component;
		while (true)
		{
			component = parent.GetComponent<Part>();
			if (component != null)
			{
				break;
			}
			parent = parent.parent;
		}
		int num = 180 * component.orientation;
		if (num < 0)
		{
			num += 360;
		}
		if (num > 360)
		{
			num -= 360;
		}
		flipX = num <= 45 || num >= 225;
	}

	[Button]
	public void Generate()
	{
		if (points.Length < 2 || sprite == null)
		{
			return;
		}
		UpdateOrientation();
		List<Point> meshPoints = GetMeshPoints();
		if (shade == null)
		{
			shade = sprite;
		}
		Vector3[] array = new Vector3[meshPoints.Count * 2];
		Vector3[] array2 = new Vector3[meshPoints.Count * 2];
		Vector3[] array3 = new Vector3[meshPoints.Count * 2];
		List<int> list = new List<int>();
		Color[] array4 = new Color[meshPoints.Count * 2];
		for (int i = 0; i < meshPoints.Count - 1; i += 2)
		{
			if (i % 1 == 0)
			{
				array[i * 2] = new Vector3(meshPoints[i].position.x - meshPoints[i].width * Mathf.Clamp(1f - cutout * 2f, -1f, 1f), meshPoints[i].position.y);
				array[i * 2 + 1] = new Vector3(meshPoints[i].position.x + meshPoints[i].width * Mathf.Clamp(1f + cutout * 2f, -1f, 1f), meshPoints[i].position.y);
				array[i * 2 + 2] = new Vector3(meshPoints[i + 1].position.x - meshPoints[i + 1].width * Mathf.Clamp(1f - cutout * 2f, -1f, 1f), meshPoints[i + 1].position.y);
				array[i * 2 + 3] = new Vector3(meshPoints[i + 1].position.x + meshPoints[i + 1].width * Mathf.Clamp(1f + cutout * 2f, -1f, 1f), meshPoints[i + 1].position.y);
				Vector2 a = array[i * 2 + 1];
				Vector2 a2 = array[i * 2];
				Vector2 a3 = array[i * 2 + 2];
				Vector2 a4 = array[i * 2 + 3];
				float num = (a3.y - a.y) / (a3.x - a.x);
				float num2 = (a2.y - a4.y) / (a2.x - a4.x);
				float num3 = (a.x * num - a4.x * num2 + (a4.y - a.y)) / (num - num2);
				Vector2 b = new Vector2(num3, num * num3 - a.x * num + a.y);
				float num4 = Vector2.Distance(a2, b);
				float num5 = Vector2.Distance(a, b);
				float num6 = Vector2.Distance(a3, b);
				float num7 = Vector2.Distance(a4, b);
				float num8 = (num4 + num7) / num7;
				float num9 = (num5 + num6) / num6;
				float num10 = (num6 + num5) / num5;
				float num11 = (num7 + num4) / num4;
				float y = Mathf.LerpUnclamped(sprite.rect.y, sprite.rect.y + sprite.rect.height, meshPoints[i].uv) / (float)sprite.texture.height;
				float y2 = Mathf.LerpUnclamped(sprite.rect.y, sprite.rect.y + sprite.rect.height, meshPoints[i + 1].uv) / (float)sprite.texture.height;
				float num12 = sprite.rect.x / (float)sprite.texture.width;
				float num13 = (sprite.rect.x + sprite.rect.width) / (float)sprite.texture.width;
				array2[i * 2] = new Vector3(Mathf.Lerp(num12, num13, Mathf.Clamp01(cutoutUV)), y, 1f) * num8;
				array2[i * 2 + 1] = new Vector3(Mathf.Lerp(num13, num12, Mathf.Clamp01(0f - cutoutUV)), y, 1f) * num9;
				array2[i * 2 + 2] = new Vector3(Mathf.Lerp(num12, num13, Mathf.Clamp01(cutoutUV)), y2, 1f) * num10;
				array2[i * 2 + 3] = new Vector3(Mathf.Lerp(num13, num12, Mathf.Clamp01(0f - cutoutUV)), y2, 1f) * num11;
				array2[i * 2].z--;
				array2[i * 2 + 1].z--;
				array2[i * 2 + 2].z--;
				array2[i * 2 + 3].z--;
				float y3 = Mathf.LerpUnclamped(shade.rect.y, shade.rect.y + shade.rect.height, meshPoints[i].uv) / (float)shade.texture.height;
				float y4 = Mathf.LerpUnclamped(shade.rect.y, shade.rect.y + shade.rect.height, meshPoints[i + 1].uv) / (float)shade.texture.height;
				float num14 = (shade.rect.x + (flipX ? shade.rect.width : 0f)) / (float)shade.texture.width;
				float num15 = (shade.rect.x + (flipX ? 0f : shade.rect.width)) / (float)shade.texture.width;
				array3[i * 2] = new Vector3(Mathf.Lerp(num14, num15, Mathf.Clamp01(cutoutUV)), y3, 1f) * num8;
				array3[i * 2 + 1] = new Vector3(Mathf.Lerp(num15, num14, Mathf.Clamp01(0f - cutoutUV)), y3, 1f) * num9;
				array3[i * 2 + 2] = new Vector3(Mathf.Lerp(num14, num15, Mathf.Clamp01(cutoutUV)), y4, 1f) * num10;
				array3[i * 2 + 3] = new Vector3(Mathf.Lerp(num15, num14, Mathf.Clamp01(0f - cutoutUV)), y4, 1f) * num11;
				array3[i * 2].z--;
				array3[i * 2 + 1].z--;
				array3[i * 2 + 2].z--;
				array3[i * 2 + 3].z--;
				list.Add(i * 2);
				list.Add(i * 2 + 2);
				list.Add(i * 2 + 1);
				list.Add(i * 2 + 1);
				list.Add(i * 2 + 2);
				list.Add(i * 2 + 3);
				array4[i * 2] = (array4[i * 2 + 1] = (array4[i * 2 + 2] = (array4[i * 2 + 3] = ((meshPoints[i].color.a == 0f) ? color : meshPoints[i].color))));
			}
		}
		Mesh mesh = new Mesh();
		mesh.triangles = new int[0];
		mesh.vertices = array;
		mesh.triangles = list.ToArray();
		mesh.SetUVs(0, new List<Vector3>(array2));
		mesh.SetUVs(1, new List<Vector3>(array3));
		mesh.colors = array4;
		base.transform.GetComponent<MeshFilter>().sharedMesh = mesh;


		mesh.RecalculateNormals();
		base.gameObject.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName;
		base.gameObject.GetComponent<MeshRenderer>().sortingOrder = SortingOrder;
	}

	private float GetEndUV()
	{
		return 1f - GetTopBorderSize();
	}

	private float GetBottomBorderSize()
	{
		return sprite.border.y / sprite.rect.height;
	}

	private float GetTopBorderSize()
	{
		return sprite.border.w / sprite.rect.height;
	}

	[Button]
	public void GenerateCollider()
	{
		if (GetComponent<PolygonCollider2D>() == null)
		{
			return;
		}
		Vector2[] array = new Vector2[points.Length * 2];
		for (int i = 0; i < points.Length; i++)
		{
			array[i] = new Vector3(points[i].position.x - points[i].width * Mathf.Clamp(1f - cutout * 2f, -1f, 1f), points[i].position.y);
			array[array.Length - 1 - i] = new Vector3(points[i].position.x + points[i].width * Mathf.Clamp(1f + cutout * 2f, -1f, 1f), points[i].position.y);
		}
		for (int j = 1; j < array.Length; j++)
		{
			if (array[j - 1] == array[j])
			{

				return;
			}
		}
		GetComponent<PolygonCollider2D>().points = array;
	}

	public Point[] CopyPoints()
	{
		Point[] array = new Point[points.Length];
		for (int i = 0; i < points.Length; i++)
		{
			array[i] = new Point(points[i].position, points[i].width, points[i].uv, points[i].color);
		}
		return array;
	}
}
