using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class LinkModule : Module
{
	public enum Type
	{
		Multiply = 0,
		Add = 1
	}

	[BoxGroup("A", false, false, 0)]
	public float In;

	[Space]
	[BoxGroup("A", false, false, 0)]
	[HorizontalGroup("A/A", 0f, 0, 0, 0)]
	public FloatValueHolder[] modifiers;

	[Space]
	[BoxGroup("A", false, false, 0)]
	[HorizontalGroup("A/A", 0f, 0, 0, 0)]
	public Type[] types = new Type[0];

	[BoxGroup("A", false, false, 0)]
	public FloatValueHolder Out;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(modifiers);
			list.Add(Out);
			return list;
		}
	}

	private void OnValidate()
	{
		if (modifiers.Length != types.Length)
		{
			Type[] array = new Type[modifiers.Length];
			for (int i = 0; i < Mathf.Min(modifiers.Length, types.Length); i++)
			{
				array[i] = types[i];
			}
			types = array;
		}
		for (int j = 0; j < modifiers.Length; j++)
		{
			if (!(modifiers[j].valuesHolder != null))
			{
				continue;
			}
			bool flag = false;
			for (int k = 0; k < modifiers[j].valuesHolder.values[modifiers[j].index].methodsHolder.Length; k++)
			{
				if (flag)
				{
					break;
				}
				if (modifiers[j].valuesHolder.values[modifiers[j].index].methodsHolder[k].component == this && modifiers[j].valuesHolder.values[modifiers[j].index].methodsHolder[k].methodName == "Calculate")
				{
					flag = true;
				}
			}
			if (!flag)
			{
				List<ValuesModule.Holder.MethodRefHolder> list = new List<ValuesModule.Holder.MethodRefHolder>(modifiers[j].valuesHolder.values[modifiers[j].index].methodsHolder);
				list.Add(new ValuesModule.Holder.MethodRefHolder(this, "Calculate"));
				modifiers[j].valuesHolder.values[modifiers[j].index].methodsHolder = list.ToArray();
			}
		}
	}

	public void Calculate(float emptyValue)
	{
		float num = In;
		for (int i = 0; i < modifiers.Length; i++)
		{
			num = ((types[i] != Type.Multiply) ? (num + modifiers[i].floatValue) : (num * modifiers[i].floatValue));
		}
		Out.floatValue = num;
	}
}
