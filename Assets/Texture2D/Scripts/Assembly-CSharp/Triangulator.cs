using UnityEngine;

public static class Triangulator
{
	public static bool InsideTriangle(Vector2 point, Vector2 A, Vector2 B, Vector2 C, float edge)
	{
		if (A.y == C.y)
		{
			float num = C.y - B.y;
			float num2 = C.x - B.x;
			float num3 = A.y - B.y;
			float num4 = point.y - B.y;
			float num5 = (B.x * num + num4 * num2 - point.x * num) / (num3 * num2 - (A.x - B.x) * num);
			float num6 = (num4 - num5 * num3) / num;
			if (num5 >= 0f && num6 >= 0f && 1f >= num5 + num6)
			{
				return true;
			}
		}
		else
		{
			float num7 = C.y - A.y;
			float num8 = C.x - A.x;
			float num9 = B.y - A.y;
			float num10 = point.y - A.y;
			float num11 = (A.x * num7 + num10 * num8 - point.x * num7) / (num9 * num8 - (B.x - A.x) * num7);
			float num12 = (num10 - num11 * num9) / num7;
			if (num11 >= 0f && num12 >= 0f && 1f >= num11 + num12)
			{
				return true;
			}
		}
		if (edge == 0f)
		{
			return false;
		}
		float num13 = edge * edge;
		if (Utility.DistanceSqrtToLine(point, A, B) < num13)
		{
			return true;
		}
		if (Utility.DistanceSqrtToLine(point, B, C) < num13)
		{
			return true;
		}
		if (Utility.DistanceSqrtToLine(point, C, A) < num13)
		{
			return true;
		}
		return false;
	}

	public static bool Intersect(Vector2 a1, Vector2 a2, Vector2 a3, Vector2 b1, Vector2 b2, Vector2 b3)
	{
		return Utility.LineLineIntersect(a1, a2, b1, b2) || Utility.LineLineIntersect(a1, a2, b2, b3) || Utility.LineLineIntersect(a1, a2, b3, b1) || Utility.LineLineIntersect(a2, a3, b1, b2) || Utility.LineLineIntersect(a2, a3, b2, b3) || Utility.LineLineIntersect(a2, a3, b3, b1) || Utility.LineLineIntersect(a3, a1, b1, b2) || Utility.LineLineIntersect(a3, a1, b2, b3) || Utility.LineLineIntersect(a3, a1, b3, b1) || InsideTriangle(a1, b1, b2, b3, 0f) || InsideTriangle(b1, a1, a2, a3, 0f);
	}
}
