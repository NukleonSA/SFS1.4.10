using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ServerTransferProgram
{
	public static class JsonUtils
	{
		public static void Restart(this Stopwatch ob)
		{
			ob.Stop();
			ob.Reset();
			ob.Start();
		}

		public static string ToJson(this object input)
		{
			return JsonConvert.SerializeObject(input, Formatting.Indented, new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				ObjectCreationHandling = ObjectCreationHandling.Auto,
				Error = (object s, Newtonsoft.Json.Serialization.ErrorEventArgs e) =>
				{
					e.ErrorContext.Handled = true;
				}
			});
		}

		public static void Store(this object input, string path)
		{
			File.WriteAllText(path, input.ToJson());
		}

		public static T Load<T>(this string path)
		{
			return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
		}

		public static void Load<T>(this string path, out T toStore)
		{
			toStore = path.Load<T>();
		}

		public static T Load<T>(this T input, string path)
		{
			return path.Load<T>();
		}

		public static T FromJson<T>(this string input)
		{
			return JsonConvert.DeserializeObject<T>(input);
		}

		public static object FromJson(this string input, Type castTo)
		{
			return JsonConvert.DeserializeObject(input, castTo);
		}

		public static string FormatTo(this string input, object form)
		{
			return input.Replace("{0}", form.ToString());
		}

		public static string FormatTo(this string input, params object[] form)
		{
			for (int i = 0; i < form.Length; i++)
			{
				input = input.Replace("{" + i + "}", form[i].ToString());
			}
			return input;
		}

		public static List<T> UToList<T>(this IEnumerable<T> input)
		{
			return new List<T>(input);
		}
	}
}
