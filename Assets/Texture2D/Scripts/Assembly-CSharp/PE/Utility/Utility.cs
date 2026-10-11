using System;
using System.Collections.Generic;
using System.Globalization;

namespace PE.Utility
{
	public static class Utility
	{
		public static bool isFloat(this string toCheck)
		{
			try
			{
				toCheck = toCheck.Replace(".", ",");
				float.Parse(toCheck);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool isDouble(this string toCheck)
		{
			try
			{
				toCheck = toCheck.Replace(".", ",");
				double.Parse(toCheck);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool isInt(this string toCheck)
		{
			try
			{
				int.Parse(toCheck, CultureInfo.InvariantCulture);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static float toFloat(this string toCheck)
		{
			return float.Parse(toCheck, CultureInfo.InvariantCulture);
		}

		public static double toDouble(this string toCheck)
		{
			return double.Parse(toCheck, CultureInfo.InvariantCulture);
		}

		public static int toInt(this string toCheck)
		{
			return int.Parse(toCheck);
		}

		public static float Evaluate(this IEnumerable<float> input, double a)
		{
			List<float> list = new List<float>(input);
			int num = (int)(a * (double)(list.Count - 1)) % (list.Count - 1);
			float num2 = (float)a * (float)(list.Count - 1) % 1f;
			return list[num] * (1f - num2) + list[num + 1] * num2;
		}
	}
}
