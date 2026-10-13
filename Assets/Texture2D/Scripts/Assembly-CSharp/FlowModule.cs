using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class FlowModule : Module
{
	public enum FlowType
	{
		Negative = 0,
		Positive = 1
	}

	public enum State
	{
		NoSource = 0,
		NoResource = 1,
		NoSpace = 2,
		Flowing = 3,
		NoFlow = 4
	}

	[BoxGroup("Data", true, false, 0)]
	[EnableIf("IsActive")]
	public Resource.Type resourceType;

	[BoxGroup("Data", true, false, 0)]
	public FlowType flowType;

	[BoxGroup("Data", true, false, 0)]
	[InlineProperty]
	public FloatValueHolder flowPerSecond;

	[BoxGroup("Data", true, false, 0)]
	[InlineProperty]
	[ShowIf("showParametersDescription", true)]
	public FloatValueHolder maxFlow;

	[BoxGroup("Data", true, false, 0)]
	[EnableIf("IsActive")]
	[HideIf("GlobalResource", true)]
	public bool global;

	[EnableIf("IsActive")]
	public State state;

	[FoldoutGroup("State", 0)]
	[Space]
	[EnableIf("IsActive")]
	public List<ResourceGroup> sources;

	[FoldoutGroup("State", 0)]
	[EnableIf("IsActive")]
	public List<ResourceGroup> withResource;

	[FoldoutGroup("State", 0)]
	[EnableIf("IsActive")]
	public List<ResourceGroup> withSpace;

	public UnityEvent onStateChange;

	public bool showParametersDescription = true;

	public override List<string> DescriptionVariables
	{
		get
		{
			List<string> result;
			if (showParametersDescription)
			{
				List<string> list = new List<string>();
				list.Add(Resource.GetResourceName(resourceType) + ((flowType != FlowType.Negative) ? "产生:" : "消耗:") + maxFlow.floatValue * 2f + Resource.GetResourceUnit(resourceType));
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
	public void OnFlowChange(float empty)
	{
		GetSource();
		UpdateState();
	}

	public void FlowPerSecond(float empty)
	{
		UpdateState();
	}

	private void FixedUpdate()
	{
		if (flowType == FlowType.Negative)
		{
			if (!Ref.infiniteFuel)
			{
				FlowNegative();
			}
		}
		else
		{
			FlowPositive();
		}
	}

	private void FlowNegative()
	{
		double amount = (double)flowPerSecond.floatValue * (double)Time.fixedDeltaTime / (double)withResource.Count;
		for (int i = 0; i < withResource.Count; i++)
		{
			withResource[i].TakeResource(amount);
			if (withResource[i].empty)
			{
				withResource.RemoveAt(i);
				i--;
				UpdateState();
			}
		}
	}

	private void FlowPositive()
	{
		double amount = (double)flowPerSecond.floatValue * (double)Time.fixedDeltaTime / (double)withSpace.Count;
		for (int i = 0; i < withSpace.Count; i++)
		{
			withSpace[i].AddResource(amount);
			if (withSpace[i].full)
			{
				withSpace.RemoveAt(i);
				i--;
				UpdateState();
			}
		}
	}

	public void UpdateState()
	{
		if (sources.Count == 0)
		{
			base.enabled = false;
			state = State.NoSource;
			onStateChange.Invoke();
			return;
		}
		if (flowType == FlowType.Negative)
		{
			if (withResource.Count == 0)
			{
				base.enabled = false;
				state = State.NoResource;
				onStateChange.Invoke();
				return;
			}
		}
		else if (withSpace.Count == 0)
		{
			base.enabled = false;
			state = State.NoSpace;
			onStateChange.Invoke();
			return;
		}
		base.enabled = flowPerSecond.floatValue > 0f;
		state = State.Flowing;
		onStateChange.Invoke();
	}

	public void GetSource()
	{
		sources = new List<ResourceGroup>();
		withResource = new List<ResourceGroup>();
		withSpace = new List<ResourceGroup>();
		if (global || Resource.GrupsGlobally(resourceType))
		{
			for (int i = 0; i < part.vessel.partsManager.resourceGrups.Count; i++)
			{
				if (part.vessel.partsManager.resourceGrups[i].resourceType == resourceType)
				{
					AddSource(part.vessel.partsManager.resourceGrups[i]);
				}
			}
		}
		else
		{
			for (int j = 0; j < part.joints.Count; j++)
			{
				if (part.joints[j].resourceFlow)
				{
					ResourceModule resourceModule = ((!(part.joints[j].fromPart == part)) ? part.joints[j].fromPart : part.joints[j].toPart).GetResourceModule();
					if (!(resourceModule == null) && resourceModule.resourceType == resourceType)
					{
						AddSource(resourceModule.resourceGroup);
					}
				}
			}
		}
		UpdateState();
	}

	private void AddSource(ResourceGroup newSource)
	{
		sources.Add(newSource);
		if (newSource.resourcePercent > 0.0)
		{
			withResource.Add(newSource);
		}
		if (newSource.resourcePercent < 1.0)
		{
			withSpace.Add(newSource);
		}
		newSource.flowModules.Add(this);
	}

	public static bool ConfirmSources(FlowModule[] resourceSources, bool showMsg)
	{
		State state = State.Flowing;
		for (int i = 0; i < resourceSources.Length; i++)
		{
			if (resourceSources[i].state == State.Flowing)
			{
				continue;
			}
			if (resourceSources[i].state == State.NoSource)
			{
				state = State.NoSource;
				if (showMsg)
				{
					MenuManager.ShowMsg((resourceSources[i].state != State.NoSource) ? ("没有" + Resource.GetResourceName(resourceSources[i].resourceType).ToLower()) : ("No " + Resource.GetResourceName(resourceSources[i].resourceType).ToLower() + " source"));
				}
				return false;
			}
			if (resourceSources[i].state == State.NoResource)
			{
				state = State.NoResource;
				if (showMsg)
				{
					MenuManager.ShowMsg((resourceSources[i].state != State.NoSource) ? ("没有" + Resource.GetResourceName(resourceSources[i].resourceType).ToLower()) : ("No " + Resource.GetResourceName(resourceSources[i].resourceType).ToLower() + " source"));
				}
			}
		}
		return state == State.Flowing;
	}
}
