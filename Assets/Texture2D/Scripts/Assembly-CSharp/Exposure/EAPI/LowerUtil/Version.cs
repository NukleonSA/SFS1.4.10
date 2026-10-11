using System.Collections.Generic;

namespace Exposure.EAPI.LowerUtil
{
	public class Version
	{
		private List<int> VersionList = new List<int>();

		private List<string> VersionListString
		{
			get
			{
				List<string> list = new List<string>();
				foreach (int version in VersionList)
				{
					list.Add(version.ToString());
				}
				return list;
			}
		}

		public string VersionString
		{
			get
			{
				return "V" + string.Join(".", VersionListString.ToArray());
			}
		}

		public Version(string version)
		{
			string[] array = version.Split('.');
			foreach (string s in array)
			{
				int result = -1;
				int.TryParse(s, out result);
				VersionList.Add(result);
			}
		}

		public static bool operator >(Version one, Version two)
		{
			List<int> versionList = one.VersionList;
			List<int> versionList2 = two.VersionList;
			for (int i = 0; i < versionList.Count && i < versionList2.Count; i++)
			{
				if (versionList[i] > versionList2[i])
				{
					return true;
				}
				if (versionList[i] < versionList2[i])
				{
					return false;
				}
			}
			return versionList.Count > versionList2.Count;
		}

		public static bool operator <(Version one, Version two)
		{
			List<int> versionList = two.VersionList;
			List<int> versionList2 = one.VersionList;
			for (int i = 0; i < versionList.Count && i < versionList2.Count; i++)
			{
				if (versionList[i] > versionList2[i])
				{
					return true;
				}
				if (versionList[i] < versionList2[i])
				{
					return false;
				}
			}
			return versionList.Count > versionList2.Count;
		}
	}
}
