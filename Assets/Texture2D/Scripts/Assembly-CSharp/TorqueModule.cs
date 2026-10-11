using System.Collections.Generic;
using Sirenix.OdinInspector;

public class TorqueModule : Module
{
	[InlineProperty]
	public FloatValueHolder torque;

	public bool showParametersDescription = true;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(torque);
			return list;
		}
	}

	public override List<string> DescriptionVariables
	{
		get
		{
			List<string> result;
			if (showParametersDescription)
			{
				List<string> list = new List<string>();
				list.Add("扭矩:" + torque.floatValue + " kN");
				result = list;
			}
			else
			{
				result = new List<string>();
			}
			return result;
		}
	}

	public override bool IsSorted()
	{
		return true;
	}

	public void Torque(float newTorque)
	{
		base.transform.root.GetComponent<Vessel>().partsManager.UpdateTorque();
	}
}
