using UnityEngine;

namespace Assets
{
	public static class VersionHolder
	{
		public static string SharingVersion = "0.5";

		public static string ProtocolVersion = "1.0";

		public static string ClientVersion = "1.35";

		public static int CompareVersion(this string subject, string toCompareTo)
		{
			string[] array = subject.Split('.');
			string[] array2 = toCompareTo.Split('.');
			for (int i = 0; i < array2.Length && i < array.Length; i++)
			{
				int num = int.Parse(array[i]);
				int num2 = int.Parse(array2[i]);
				if (num > num2)
				{
					Debug.Log(num + ">" + num2 + " on " + subject + " with " + toCompareTo);
					return 1;
				}
				if (num < num2)
				{
					Debug.Log(num + "<" + num2 + " on " + subject + " with " + toCompareTo);
					return -1;
				}
			}
			return 0;
		}
	}
}
