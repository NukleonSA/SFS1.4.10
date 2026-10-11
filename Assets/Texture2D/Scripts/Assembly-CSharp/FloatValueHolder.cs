using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class FloatValueHolder
{
	[HideInInspector]
	public bool useExternalReference;

	[HideInInspector]
	public float localFloat;

	[HorizontalGroup(0f, 0, 0, 0)]
	[CustomValueDrawer("DrawColor")]
	public int index = -1;

	[HorizontalGroup(0f, 0, 0, 0)]
	[HideLabel]
	[ShowIf("ShowValueModule", true)]
	public ValuesModule valuesHolder;

	public float floatValue
	{
		get
		{
			return (!useExternalReference) ? localFloat : valuesHolder.values[index].floatValue;
		}
		set
		{
			if (((!useExternalReference) ? localFloat : valuesHolder.values[index].floatValue) == value)
			{
				return;
			}
			if (useExternalReference)
			{
				valuesHolder.values[index].floatValue = value;
				if (valuesHolder.values[index].hasValidDelegate)
				{
					valuesHolder.values[index].updateDelegate(value);
				}
			}
			else
			{
				localFloat = value;
			}
		}
	}
}
