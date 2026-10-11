using System.Collections.Generic;
using Sirenix.OdinInspector;

public class WheelModule : Module
{
	public Wheel wheel;

	[InlineProperty]
	public BoolValueHolder on;

	public FlowModule[] resourceSources;

	[InlineProperty]
	public FloatValueHolder throttle;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(on);
			return list;
		}
	}

	public override void OnPartUsed()
	{
		for (int i = 0; i < resourceSources.Length; i++)
		{
			if (resourceSources[i].state != FlowModule.State.Flowing)
			{
				MenuManager.ShowMsg((resourceSources[i].state != FlowModule.State.NoSource) ? ("没有" + Resource.GetResourceName(resourceSources[i].resourceType).ToLower()) : ("No " + Resource.GetResourceName(resourceSources[i].resourceType).ToLower() + " source"));
				return;
			}
		}
		on.boolValue = !on.boolValue;
		MenuManager.ShowMsg("漫游车轮" + ((!on.boolValue) ? "关" : "开"));
	}

	public void OnSourceStateChange()
	{
		if (!FlowModule.ConfirmSources(resourceSources, Ref.mainVessel == part.vessel))
		{
			on.boolValue = false;
		}
	}
}
