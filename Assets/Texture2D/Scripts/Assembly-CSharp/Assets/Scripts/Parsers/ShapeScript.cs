using System;
using System.Collections.Generic;
using PE.Utility;

namespace Assets.Scripts.Parsers
{
	public class ShapeScript
	{
		public enum ParserStage
		{
			EMPTY = 0,
			READER_STARTED = 1,
			VAR_ASSIGNMENT = 2,
			FUNCTION = 3,
			IN_STRING = 4
		}

		private const string Apply_Curve = "ApplyCurve";

		private const string Add_Height_Map = "AddHeightMap";

		private const string Add = "Add";

		private const string Multiply = "Multiply";

		private const string ClampMinMax = "ClampMinMax";

		public static object JsonConver { get; private set; }

		public static float[] GetTerrainSamples(double start, double delta, int pointCount, string[] terrainFormula)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < terrainFormula.Length; i++)
			{
				if (!terrainFormula[i].StartsWith("//"))
				{
					list.Add(terrainFormula[i]);
				}
			}
			string text = string.Join(";", list.ToArray()) + ";";
			float[] value = new float[pointCount];
			Dictionary<string, object> storage = new Dictionary<string, object> { { "OUTPUT", value } };
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			List<string> list2 = new List<string>();
			list2.Add("ApplyCurve");
			list2.Add("Add");
			list2.Add("Multiply");
			list2.Add("AddHeightMap");
			list2.Add("ClampMinMax");
			List<string> list3 = list2;
			Func<string, int> func = (string r) =>
			{
				if (r.isInt())
				{
					return r.toInt();
				}
				if (!storage.ContainsKey(r))
				{
					throw new Exception("Argument could not be parsed as int");
				}
				if (storage[r].ToString().isInt())
				{
					return storage[r].ToString().toInt();
				}
				throw new Exception("Argument could not be parsed as int");
			};
			Func<string, float> func2 = (string r) =>
			{
				if (r.isFloat())
				{
					return r.toFloat();
				}
				if (!storage.ContainsKey(r))
				{
					throw new Exception("Argument could not be parsed as float");
				}
				if (storage[r].ToString().isFloat())
				{
					return storage[r].ToString().toFloat();
				}
				throw new Exception("Argument could not be parsed as float");
			};
			Func<string, Type, object> func3 = (string name, Type type) =>
			{
				if (!storage.ContainsKey(name))
				{
					if (type == typeof(float[]))
					{
						return new float[pointCount];
					}
					return Activator.CreateInstance(type);
				}
				object obj2 = storage[name];
				if (obj2.GetType() != type)
				{
					throw new Exception("Type mismatch");
				}
				return Convert.ChangeType(obj2, type);
			};
			string text2 = string.Empty;
			List<ParserStage> list4 = new List<ParserStage>();
			string text3 = string.Empty;
			for (int num = 0; num < text.Length; num++)
			{
				string text4 = text[num].ToString();
				bool flag = false;
				try
				{
					flag = list4.Contains(ParserStage.IN_STRING);
					if (text4 == " " && !flag)
					{
						continue;
					}
					if (text4 == "\"")
					{
						if (list4.Contains(ParserStage.IN_STRING))
						{
							list4.Remove(ParserStage.IN_STRING);
						}
						else
						{
							list4.Add(ParserStage.IN_STRING);
						}
						continue;
					}
					if (text4 == "=" && !flag)
					{
						list4.Add(ParserStage.VAR_ASSIGNMENT);
						dictionary["EXIT_VAR"] = text2;
						text2 = string.Empty;
						text3 = string.Empty;
						continue;
					}
					if (text4 == "(" && !flag && list3.Contains(text2))
					{
						list4.Add(ParserStage.FUNCTION);
						text3 = text2;
						text2 = string.Empty;
						continue;
					}
					if (text4 == "," && !flag)
					{
						if (!dictionary.ContainsKey("AGN"))
						{
							dictionary["AG1"] = text2;
							dictionary["AGN"] = 2;
						}
						else
						{
							int num2 = (int)dictionary["AGN"];
							dictionary["AG" + num2] = text2;
							num2++;
							dictionary["AGN"] = num2;
						}
						text2 = string.Empty;
						continue;
					}
					if (text4 == ")" && !flag)
					{
						int num3 = 1;
						if (dictionary.ContainsKey("AGN"))
						{
							num3 = (int)dictionary["AGN"];
						}
						dictionary["AG" + num3] = text2;
						num3++;
						dictionary["AGN"] = num3;
						text2 = string.Empty;
						if (list4.Contains(ParserStage.FUNCTION))
						{
							switch (text3)
							{
							case "ApplyCurve":
							{
								float[] array8 = new float[pointCount];
								if (storage.ContainsKey(dictionary["EXIT_VAR"] as string))
								{
									array8 = (float[])storage[dictionary["EXIT_VAR"] as string];
								}
								HeightMap heightMap3 = (dictionary["AG1"] as string).ToHeightmap();
								for (int num17 = 0; num17 < pointCount; num17++)
								{
									array8[num17] = heightMap3.EvaluateClamped(array8[num17]);
								}
								dictionary["CURRENT_RESULT"] = array8;
								list4.Remove(ParserStage.FUNCTION);
								text2 = string.Empty;
								text3 = string.Empty;
								goto end_IL_0138;
							}
							case "AddHeightMap":
							{
								float[] array2 = ((!storage.ContainsKey(dictionary["EXIT_VAR"] as string)) ? new float[pointCount] : ((float[])storage[dictionary["EXIT_VAR"] as string]));
								HeightMap heightMap = (dictionary["AG1"] as string).ToHeightmap();
								double num5 = func2(dictionary["AG2"] as string);
								float num6 = func2(dictionary["AG3"] as string);
								HeightMap heightMap2 = ((!dictionary.ContainsKey("AG4")) ? null : (dictionary["AG4"] as string).ToHeightmap());
								float[] array3 = ((!dictionary.ContainsKey("AG5") || !storage.ContainsKey(dictionary["AG5"] as string)) ? null : (storage[dictionary["AG5"] as string] as float[]));
								if (heightMap2 != null)
								{
									if (array3 != null)
									{
										for (int num7 = 0; num7 < pointCount; num7++)
										{
											array2[num7] += (float)heightMap2.EvaluateDoubleOut(heightMap.EvaluateDoubleOut((start + delta * (double)num7) * num5)) * num6 * array3[num7];
										}
									}
									else
									{
										for (int num8 = 0; num8 < pointCount; num8++)
										{
											array2[num8] += (float)heightMap2.EvaluateDoubleOut(heightMap.EvaluateDoubleOut((start + delta * (double)num8) * num5)) * num6;
										}
									}
								}
								else if (array3 != null)
								{
									for (int num9 = 0; num9 < pointCount; num9++)
									{
										array2[num9] += (float)heightMap.EvaluateDoubleOut((start + delta * (double)num9) * num5) * num6 * array3[num9];
									}
								}
								else
								{
									for (int num10 = 0; num10 < pointCount; num10++)
									{
										array2[num10] += (float)heightMap.EvaluateDoubleOut((start + delta * (double)num10) * num5) * num6;
									}
								}
								dictionary["CURRENT_RESULT"] = array2;
								list4.Remove(ParserStage.FUNCTION);
								text2 = string.Empty;
								text3 = string.Empty;
								goto end_IL_0138;
							}
							case "Add":
							case "Multiply":
							{
								string text5 = dictionary["AG1"] as string;
								float result = 0f;
								if (float.TryParse(text5, out result))
								{
									float[] array4 = func3(dictionary["EXIT_VAR"] as string, typeof(float[])) as float[];
									if (text3 == "Add")
									{
										for (int num11 = 0; num11 < array4.Length; num11++)
										{
											array4[num11] += result;
										}
									}
									else
									{
										for (int num12 = 0; num12 < array4.Length; num12++)
										{
											array4[num12] *= result;
										}
									}
								}
								else
								{
									string s = storage[text5].ToString();
									if (float.TryParse(s, out result))
									{
										float[] array5 = (float[])storage[dictionary["EXIT_VAR"] as string];
										if (text3 == "Add")
										{
											for (int num13 = 0; num13 < array5.Length; num13++)
											{
												array5[num13] += result;
											}
										}
										else
										{
											for (int num14 = 0; num14 < array5.Length; num14++)
											{
												array5[num14] *= result;
											}
										}
										dictionary["CURRENT_RESULT"] = array5;
									}
									else
									{
										float[] array6 = (float[])storage[text5];
										float[] array7 = (float[])storage[dictionary["EXIT_VAR"] as string];
										if (text3 == "Add")
										{
											for (int num15 = 0; num15 < array7.Length; num15++)
											{
												array7[num15] += array6[num15];
											}
										}
										else
										{
											for (int num16 = 0; num16 < array7.Length; num16++)
											{
												array7[num16] *= array6[num16];
											}
										}
										dictionary["CURRENT_RESULT"] = array7;
									}
								}
								list4.Remove(ParserStage.FUNCTION);
								text2 = string.Empty;
								text3 = string.Empty;
								goto end_IL_0138;
							}
							case "ClampMinMax":
							{
								object obj = storage[dictionary["EXIT_VAR"] as string];
								float val = (dictionary["AG1"] as string).toFloat();
								float val2 = (dictionary["AG2"] as string).toFloat();
								if (obj is float[])
								{
									float[] array = (float[])obj;
									for (int num4 = 0; num4 < array.Length; num4++)
									{
										array[num4] = Math.Min(Math.Max(array[num4], val), val2);
									}
									dictionary["CURRENT_RESULT"] = array;
									list4.Remove(ParserStage.FUNCTION);
									text2 = string.Empty;
									text3 = string.Empty;
									continue;
								}
								if (!(obj is string))
								{
									break;
								}
								float val3 = obj.ToString().toFloat();
								val3 = Math.Min(Math.Max(val3, val), val2);
								dictionary["CURRENT_RESULT"] = val3;
								list4.Remove(ParserStage.FUNCTION);
								text2 = string.Empty;
								text3 = string.Empty;
								goto end_IL_0138;
							}
							}
						}
					}
					if (text4 == ";" && !flag)
					{
						if (list4.Contains(ParserStage.VAR_ASSIGNMENT))
						{
							string key = dictionary["EXIT_VAR"] as string;
							if (dictionary.ContainsKey("RESULT"))
							{
								storage[key] = dictionary["RESULT"];
							}
							else if (dictionary.ContainsKey("CURRENT_RESULT"))
							{
								storage[key] = dictionary["CURRENT_RESULT"];
							}
							else
							{
								float result2 = 0f;
								if (float.TryParse(text2, out result2))
								{
									storage[key] = result2;
								}
								else
								{
									storage[key] = storage[text2];
								}
							}
							list4.Remove(ParserStage.VAR_ASSIGNMENT);
						}
						text3 = string.Empty;
						text2 = string.Empty;
						dictionary.Clear();
						list4 = new List<ParserStage>();
					}
					else
					{
						text2 += text4;
					}
					end_IL_0138:;
				}
				catch (Exception)
				{
				}
			}
			return (float[])storage["OUTPUT"];
		}
	}
}
