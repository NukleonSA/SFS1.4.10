using System;
using System.Collections.Generic;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public static class Utility
{
	[Serializable]
	public class Specefier<T>
	{
		[BoxGroup]
		public T base_value;

		[BoxGroup]
		public T full_value;

		[BoxGroup]
		[HideInInspector]
		public T pc_value;

		public virtual T Value
		{
			get
			{
				return full_value;
			}
		}
	}

	[Serializable]
	public class Vector2Specifier : Specefier<Vector2>
	{
	}

	[Serializable]
	public class PickListSpecifier : Specefier<PickList[]>
	{
	}

	public enum Location
	{
		game = 0,
		build = 1,
		pick = 2,
		held = 3
	}
	public static bool IsInsideRange(float value, float a, float b, bool canBeInsideEdge = false)
	{
		if (canBeInsideEdge)
		{
			return value >= Mathf.Min(a, b) && value <= Mathf.Max(a, b);
		}
		return value > Mathf.Min(a, b) && value < Mathf.Max(a, b);
	}

	public static bool IsInsideRange(double value, double a, double b)
	{
		return value >= Math.Min(a, b) && value <= Math.Max(a, b);
	}

	public static bool IsInsideRange(float value, float a, float b, bool canBeInsideEdge, float edge)
	{
		if (canBeInsideEdge)
		{
			return value >= Mathf.Min(a, b) - edge && value <= Mathf.Max(a, b) + edge;
		}
		return value > Mathf.Min(a, b) - edge && value < Mathf.Max(a, b) + edge;
	}

	public static double Overlap(double a1, double a2, double b1, double b2)
	{
		return Math.Min(Math.Max(a1, a2), Math.Max(b1, b2)) - Math.Max(Math.Min(a1, a2), Math.Min(b1, b2));
	}
	public static float Overlap(float a1, float a2, float b1, float b2)
	{
		return Mathf.Min(Mathf.Max(a1, a2), Mathf.Max(b1, b2)) - Mathf.Max(Mathf.Min(a1, a2), Mathf.Min(b1, b2));
	}

	public static float OverlapAmount(float a1, float a2, float b1, float b2)
	{
		return Mathf.Min(Mathf.Max(a1, a2), Mathf.Max(b1, b2)) - Mathf.Max(Mathf.Min(a1, a2), Mathf.Min(b1, b2));
	}

	public static bool LineLineIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
	{
		Debug.DrawLine(p1, p2);
		Debug.DrawLine(p3, p4);
		float num = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
		if (num == 0f)
		{
			return false;
		}
		float num2 = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / num;
		float num3 = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / num;
		return num2 >= 0f && num2 <= 1f && num3 >= 0f && num3 <= 1f;
	}

	public static bool IsInside(float a1, float a2, float b1, float b2)
	{
		return IsInsideRange(a1, b1, b2, true) && IsInsideRange(a2, b1, b2, true);
	}

	public static Vector2 RoundToHalf(Vector2 a)
	{
		return new Vector2(Mathf.Round(a.x * 10f) / 10f, Mathf.Round(a.y * 10f) / 10f);
	}

	public static float RoundToHalf(float a)
	{
		return Mathf.Round(a * 10f) / 10f;
	}
	public static Transform GetByPath(Transform root, int[] path, bool safe = false)
	{
		for (int i = 0; i < path.Length; i++)
		{
			if (!safe || root.childCount > path[i])
			{
				root = root.GetChild(path[i]);
				continue;
			}
			Debug.Log("out of range");
			for (int j = 0; j < path.Length; j++)
			{
				Debug.Log(path[j]);
			}
			return root;
		}
		return root;
	}
	public static Vector2 ToDepth(Vector3 worldPos, float targetDepth)
	{
		Vector2 vector = Camera.main.WorldToScreenPoint(worldPos);
		return Camera.main.ScreenToWorldPoint(new Vector3(vector.x, vector.y, targetDepth));
	}
	public static string SplitLines(string textToSplit)
	{
		string text = string.Empty;
		for (int i = 0; i < textToSplit.Length; i++)
		{
			text += ((!(textToSplit[i].ToString() == "/")) ? ((!(textToSplit[i].ToString() == "&")) ? textToSplit[i].ToString() : "/") : "\r\n");
		}
		return text;
	}

	public static int GetLinesCount(string descriptionRaw)
	{
		int num = 0;
		for (int i = 0; i < descriptionRaw.Length; i++)
		{
			if (descriptionRaw[i].ToString() == "/")
			{
				num++;
			}
		}
		return num;
	}

	public static Vector3 CameraRelativePosition(float fieldOfView, Vector3 posPixel)
	{
		Vector2 vector = new Vector2((posPixel.x - (float)(Screen.width / 2)) / (float)Screen.height, (posPixel.y - (float)(Screen.height / 2)) / (float)Screen.height);
		float f = fieldOfView * ((float)Math.PI / 180f);
		float num = Mathf.Sin(f) / ((1f + Mathf.Cos(f)) * 0.5f);
		return new Vector3(vector.x * posPixel.z * num, vector.y * posPixel.z * num, posPixel.z);
	}

	public static Vector2 RotateZ(Vector2 a, float b)
	{
		float num = Mathf.Cos(b);
		float num2 = Mathf.Sin(b);
		return new Vector2(a.x * num - a.y * num2, a.x * num2 + a.y * num);
	}
	public static float DistanceSqrtToLine(Vector2 point, Vector2 p1, Vector2 p2)
	{
		float num = p2.x - p1.x;
		float num2 = p2.y - p1.y;
		float t = Mathf.Clamp01(((point.x - p1.x) * num + (point.y - p1.y) * num2) / (num * num + num2 * num2));
		return (point - Vector2.Lerp(p1, p2, t)).sqrMagnitude;
	}

	public static void ApplySwitchesToIcon(Transform root, PartData partData, List<string> keys)
	{
		Dictionary<string, float> variableKeys = Parser.GetVariableKeys(partData, keys);
		if (partData.switchData == null || partData.switchData.outputs == null)
		{
			return;
		}
		for (int i = 0; i < partData.switchData.outputs.Length; i++)
		{
			float num = Parser.Process(partData.switchData.outputs[i].code, keys, variableKeys);
			Transform byPath = GetByPath(root, partData.switchData.outputs[i].path);
			if (partData.switchData.outputs[i].type == PartData.Switch.Output.Type.Active)
			{
				if (byPath.gameObject.activeSelf != num > 0f)
				{
					byPath.gameObject.SetActive(num > 0f);
				}
			}
			else if (partData.switchData.outputs[i].type == PartData.Switch.Output.Type.SpriteColor)
			{
				if (num > 0f)
				{
					byPath.GetComponent<SpriteRenderer>().color = partData.switchData.outputs[i].color;
				}
			}
			else if (partData.switchData.outputs[i].type == PartData.Switch.Output.Type.SetMesh)
			{
				SpriteMesh component = GetByPath(partData.prefab, partData.switchData.outputs[i].path).GetComponent<SpriteMesh>();
				SpriteMesh component2 = byPath.GetComponent<SpriteMesh>();
				int index = partData.switchData.outputs[i].index;
				partData.switchData.outputs[i].setType.Exec(component2, component, index, num);
			}
		}
	}

	public static List<string> GetKeys(PartData partData, Location location, Orientation orientation, bool[] surfaces, bool[] switches)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < partData.attachmentSurfaces.Length; i++)
		{
			if (partData.attachmentSurfaces[i].switchName != string.Empty && i < surfaces.Length && surfaces[i])
			{
				list.Add(partData.attachmentSurfaces[i].switchName);
			}
		}
		for (int j = 0; j < partData.switchPoints.Length; j++)
		{
			if (partData.switchPoints[j].switchName != string.Empty && j < switches.Length && switches[j])
			{
				list.Add(partData.switchPoints[j].switchName);
			}
		}
		list.Add(location.ToString());
		if (orientation.x < 0)
		{
			list.Add("x");
		}
		if (orientation.y < 0)
		{
			list.Add("y");
		}
		return list;
	}

	public static void ApplySkin(Transform root, PartData partData, int skinId, Orientation orientation)
	{
		Skin[] skins = partData.skins.skins;
		if (skins != null && skinId <= skins.Length - 1 && !(skins[skinId] == null))
		{
			Skin skin = skins[skinId];
			for (int i = 0; i < partData.skins.paths.Length; i++)
			{
				SpriteMesh component = GetByPath(root, partData.skins.paths[i].path).GetComponent<SpriteMesh>();
				component.sprite = skin.texture;
				component.shade = skin.shading;
				component.shade = skin.shading;
				component.borderScale = skin.scale;
				component.tileScale = skin.tileScale;
				component.drawMode = skin.drawMode;
				component.color = skin.color;
				component.Generate();
			}
		}
	}
}
