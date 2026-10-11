using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JMtech.Translations
{
	public class TranslationSerializer
	{
		public string Result;

		public string serialize<T>(T obj)
		{
			object obj2 = obj;
			FieldInfo[] fields = obj2.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo[] array = fields;
			foreach (FieldInfo fieldInfo in array)
			{
				string name = fieldInfo.Name;
				string text = fieldInfo.GetValue(obj).ToString();
				Result = Result + name + "=" + text + "\n";
			}
			return Result;
		}

		public T revert<T>(string txt)
		{
			string[] array = Regex.Split(txt, "\n");
			T val = Activator.CreateInstance<T>();
			Type type = val.GetType();
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (text.Contains("="))
				{
					int num = text.IndexOf("=");
					string name = text.Substring(0, num);
					string value = text.Substring(num + 1, text.Length - num - 1);
					FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(val, value);
					}
				}
			}
			return val;
		}
	}
}
