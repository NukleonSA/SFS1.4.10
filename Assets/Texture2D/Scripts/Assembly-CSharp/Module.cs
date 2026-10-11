using System;
using System.Collections.Generic;
using UnityEngine;

public class Module : MonoBehaviour
{
	[Serializable]
	public class Save
	{
		public string moduleName;

		public string[] valuesList = new string[0];

		public Save(string moduleName, List<object> variablesToSave)
		{
			this.moduleName = moduleName;
			List<string> list = new List<string>();
			for (int i = 0; i < variablesToSave.Count; i++)
			{
				if (variablesToSave[i] is FloatValueHolder)
				{
					list.Add((variablesToSave[i] as FloatValueHolder).floatValue.ToString());
				}
				else if (variablesToSave[i] is FloatValueHolder[])
				{
					FloatValueHolder[] array = variablesToSave[i] as FloatValueHolder[];
					for (int j = 0; j < array.Length; j++)
					{
						list.Add(array[j].floatValue.ToString());
					}
				}
				else if (variablesToSave[i] is BoolValueHolder)
				{
					list.Add((variablesToSave[i] as BoolValueHolder).boolValue.ToString());
				}
			}
			valuesList = list.ToArray();
		}
	}

	[HideInInspector]
	public Part part;

	public virtual List<object> SaveVariables
	{
		get
		{
			return new List<object>();
		}
	}

	public virtual List<string> DescriptionVariables
	{
		get
		{
			return new List<string>();
		}
	}

	private void Awake()
	{
		part = GetComponent<Part>();
	}

	private void OnValidate()
	{
		part = GetComponent<Part>();
	}

	public virtual bool IsSorted()
	{
		return false;
	}

	public virtual void OnPartUsed()
	{
	}

	public virtual void OnModuleLoaded()
	{
	}

	public void Load(Save loadedSave)
	{
		float result = 0f;
		List<object> saveVariables = SaveVariables;
		int num = 0;
		for (int i = 0; i < saveVariables.Count; i++)
		{
			if (saveVariables[i] is FloatValueHolder)
			{
				if (saveVariables.Count > i && loadedSave.valuesList.Length > num && float.TryParse(loadedSave.valuesList[num], out result))
				{
					(saveVariables[i] as FloatValueHolder).floatValue = float.Parse(loadedSave.valuesList[num]);
				}
				num++;
			}
			else if (saveVariables[i] is FloatValueHolder[])
			{
				if (saveVariables.Count <= i)
				{
					continue;
				}
				FloatValueHolder[] array = saveVariables[i] as FloatValueHolder[];
				for (int j = 0; j < array.Length; j++)
				{
					if (loadedSave.valuesList.Length > num && float.TryParse(loadedSave.valuesList[num], out result))
					{
						array[j].floatValue = float.Parse(loadedSave.valuesList[num]);
					}
					num++;
				}
			}
			else if (saveVariables[i] is BoolValueHolder)
			{
				if (saveVariables.Count > i && loadedSave.valuesList.Length > num)
				{
					(saveVariables[i] as BoolValueHolder).boolValue = loadedSave.valuesList[num] == "True";
				}
				num++;
			}
		}
	}
}
