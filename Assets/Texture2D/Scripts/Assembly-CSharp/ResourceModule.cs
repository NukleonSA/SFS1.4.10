using System.Collections.Generic;
using Sirenix.OdinInspector;

public class ResourceModule : Module
{
	[BoxGroup]
	public Resource.Type resourceType;

	[BoxGroup]
	[InlineProperty]
	public FloatValueHolder resourceAmount;

	[BoxGroup]
	public float resourceSpace;

	[BoxGroup("B", false, false, 0)]
	public ResourceGroup resourceGroup;

	public bool showParametersDescription = true;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(resourceAmount);
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
				list.Add(Resource.GetResourceName(resourceType) + ": " + resourceSpace * 1f + Resource.GetResourceUnit(resourceType));
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

	public override void OnPartUsed()
	{
		if (part.vessel != Ref.mainVessel || resourceType != Resource.Type.Fuel)
		{
			return;
		}
		bool flag = Ref.controller.fromTank != null;
		bool flag2 = Ref.controller.toTank != null;
		if (flag && Ref.controller.fromTank.resourceGroup == resourceGroup)
		{
			Ref.controller.fromTank = null;
		}
		else if (flag2 && Ref.controller.toTank.resourceGroup == resourceGroup)
		{
			Ref.controller.toTank = null;
		}
		else if (flag)
		{
			if (flag2)
			{
				Ref.controller.fromTank = Ref.controller.toTank;
				Ref.controller.toTank = this;
			}
			else
			{
				Ref.controller.toTank = this;
			}
		}
		else
		{
			Ref.controller.fromTank = this;
		}
	}
}
