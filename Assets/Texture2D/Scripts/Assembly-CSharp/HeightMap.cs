using System;

[Serializable]
public class HeightMap
{
	public float[] points = new float[2] { 0f, 1f };

	public float Evaluate(float a)
	{
		int num = (int)(a * (float)(points.Length - 1)) % (points.Length - 1);
		float num2 = a * (float)(points.Length - 1) % 1f;
		return points[num] * (1f - num2) + points[num + 1] * num2;
	}

	public double EvaluateDoubleOut(double a)
	{
		a *= (double)(points.Length - 1);
		int num = (int)a % (points.Length - 1);
		float num2 = (float)(a % 1.0);
		return points[num] * (1f - num2) + points[num + 1] * num2;
	}

	public float EvaluateClamped(float a)
	{
		if (a >= 1f)
		{
			return points[points.Length - 1];
		}
		if (a <= 0f)
		{
			return points[0];
		}
		int num = (int)(a * (float)(points.Length - 1));
		float num2 = a * (float)(points.Length - 1) % 1f;
		return points[num] * (1f - num2) + points[num + 1] * num2;
	}
}
