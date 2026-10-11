using System.Diagnostics;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets
{
	public class MasterInfo
	{
		public string REDIRECT;

		public VersionInfo Version;

		public static MasterInfo Fetch()
		{
			WWW wWW = new WWW("https://pastebin.com/raw/pZi4airN");
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			while (!wWW.isDone)
			{
				if (stopwatch.ElapsedMilliseconds >= 1000)
				{
					return null;
				}
			}
			string text = wWW.text;
			return JsonConvert.DeserializeObject<MasterInfo>(text);
		}
	}
}
