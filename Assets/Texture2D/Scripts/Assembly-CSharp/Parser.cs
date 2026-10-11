using System.Collections.Generic;
using NewBuildSystem;

public static class Parser
{
	private static Dictionary<string, string> replacement = new Dictionary<string, string>
	{
		{ "!f", "t" },
		{ "!t", "f" },
		{ "t=t", "t" },
		{ "t=f", "f" },
		{ "f=t", "f" },
		{ "f=f", "t" },
		{ "t!=t", "f" },
		{ "t!=f", "t" },
		{ "f!=t", "t" },
		{ "f!=f", "f" },
		{ "t&t", "t" },
		{ "t&f", "f" },
		{ "f&t", "f" },
		{ "f&f", "f" },
		{ "t|t", "t" },
		{ "t|f", "t" },
		{ "f|t", "t" },
		{ "f|f", "f" },
		{ "(f)", "f" },
		{ "(t)", "t" }
	};

	private static List<char> GruppingChars
	{
		get
		{
			List<char> list = new List<char>();
			list.Add("-"[0]);
			list.Add("."[0]);
			return list;
		}
	}

	private static List<char> IfStatementChars
	{
		get
		{
			List<char> list = new List<char>();
			list.Add("t"[0]);
			list.Add("f"[0]);
			list.Add("|"[0]);
			list.Add("&"[0]);
			list.Add("!"[0]);
			list.Add("("[0]);
			list.Add(")"[0]);
			return list;
		}
	}

	public static float Process(string code, List<string> keys, Dictionary<string, float> variableKeys)
	{
		List<string> list = ToPackets(CleanCodeString(code));
		for (int i = 0; i < list.Count; i++)
		{
			if (variableKeys.ContainsKey(list[i].Replace("-", string.Empty)))
			{
				float num = variableKeys[list[i].Replace("-", string.Empty)];
				if (list[i].Replace("-", string.Empty).Length != list[i].Length)
				{
					num = 0f - num;
				}
				list[i] = ((num == 0f) ? "f" : ((num != 1f) ? num.ToString() : "t"));
			}
			else if (char.IsLetter(list[i][0]))
			{
				list[i] = ((!keys.Contains(list[i])) ? "f" : "t");
			}
		}
		while (list.Remove("?"))
		{
		}
		while (list.Remove(":"))
		{
		}
		list = CombineIfStatements(list);
		for (int j = 0; j < list.Count; j++)
		{
			if (IfStatementChars.Contains(list[j][0]))
			{
				list[j] = ((!ProcessCode(list[j])) ? "f" : "t");
			}
		}
		if (list.Count == 1)
		{
			if (list[0] == "t")
			{
				return 1f;
			}
			if (list[0] == "f")
			{
				return 0f;
			}
			return float.Parse(list[0]);
		}
		while (true)
		{
			if (list[0] == "t")
			{
				return float.Parse(list[1]);
			}
			if (!(list[0] == "f"))
			{
				break;
			}
			list.RemoveRange(0, 2);
		}
		return float.Parse(list[0]);
	}

	private static List<string> ToPackets(string code)
	{
		List<string> list = new List<string>();
		char c = " "[0];
		for (int i = 0; i < code.Length; i++)
		{
			char c2 = code[i];
			if (list.Count > 0 && (char.IsLetterOrDigit(c2) || GruppingChars.Contains(c2)) && (char.IsLetterOrDigit(c) || GruppingChars.Contains(c)))
			{
				list[list.Count - 1] += c2;
			}
			else
			{
				list.Add(c2.ToString());
			}
			c = c2;
		}
		return list;
	}

	private static List<string> CombineIfStatements(List<string> packets)
	{
		List<string> list = new List<string>();
		string text = string.Empty;
		foreach (string packet in packets)
		{
			if (list.Count > 0 && IfStatementChars.Contains(packet[0]) && text.Length > 0 && IfStatementChars.Contains(text[0]))
			{
				list[list.Count - 1] += packet;
			}
			else
			{
				list.Add(packet);
			}
			text = packet;
		}
		return list;
	}

	public static string CleanCodeString(string a)
	{
		a = a.Split("/"[0])[0];
		a = a.Replace(" ", string.Empty);
		a = a.Replace("&&", "&");
		a = a.Replace("==", "=");
		a = a.Replace("||", "|");
		return a;
	}

	public static string InsertKeys(string IN, List<string> keys)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		foreach (char c in IN)
		{
			if (!char.IsLetterOrDigit(c))
			{
				if (text2 != string.Empty)
				{
					text += ((!keys.Contains(text2)) ? "f" : "t");
					text2 = string.Empty;
				}
				text += c;
			}
			else
			{
				text2 += c;
			}
		}
		if (text2 != string.Empty)
		{
			text += ((!keys.Contains(text2)) ? "f" : "t");
		}
		return text;
	}

	public static bool ProcessCode(string a)
	{
		string text = string.Empty;
		while (a != text)
		{
			text = a;
			foreach (string key in replacement.Keys)
			{
				a = a.Replace(key, replacement[key]);
			}
		}
		return a == "t";
	}

	public static Dictionary<string, float> GetVariableKeys(PartData partData, List<string> keys)
	{
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		for (int i = 0; i < partData.switchData.variables.Length; i++)
		{
			dictionary.Add(partData.switchData.variables[i].switchName, Process(partData.switchData.variables[i].code, keys, new Dictionary<string, float>()));
		}
		return dictionary;
	}
}
