using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ResourceGroup : MonoBehaviour
{
	[BoxGroup]
	public Resource.Type resourceType;

	[BoxGroup]
	public double resourceAmount;

	[BoxGroup]
	public double resourceSpace;

	[BoxGroup]
	public double resourcePercent;

	[BoxGroup]
	public bool empty;

	[BoxGroup]
	public bool full;

	[BoxGroup]
	public Transform fuelIcon;

	[BoxGroup]
	public TextMesh counter;

	public List<ResourceModule> resourceModules;

	public List<FlowModule> flowModules;

	public void Initialize(List<ResourceModule> resourceModulesAll, int currentIndex, Resource.Type newResourceType)
	{
		resourceType = newResourceType;
		resourceModules = new List<ResourceModule>();
		flowModules = new List<FlowModule>();
		if (Resource.GrupsGlobally(resourceType))
		{
			for (int i = currentIndex; i < resourceModulesAll.Count; i++)
			{
				if (resourceModulesAll[i].resourceType == resourceType)
				{
					AddResourceModule(resourceModulesAll[i]);
				}
			}
		}
		else
		{
			GetConnectedResourceModules(resourceModulesAll[currentIndex]);
		}
		SetTanks();
		empty = resourcePercent == 0.0;
		full = resourcePercent >= 1.0;
	}

	private void AddResourceModule(ResourceModule newModule)
	{
		resourceModules.Add(newModule);
		resourceSpace += newModule.resourceSpace;
		resourceAmount += newModule.resourceAmount.floatValue;
		newModule.resourceGroup = this;
	}

	private void GetConnectedResourceModules(ResourceModule start)
	{
		AddResourceModule(start);
		Ref.connectionCheckId++;
		start.part.connectionCheckId = Ref.connectionCheckId;
		List<Part> list = new List<Part>(1);
		list.Add(start.part);
		List<Part> list2 = list;
		while (list2.Count > 0)
		{
			for (int i = 0; i < list2[0].joints.Count; i++)
			{
				if (!list2[0].joints[i].resourceFlow)
				{
					continue;
				}
				Part part = ((!(list2[0].joints[i].fromPart == list2[0])) ? list2[0].joints[i].fromPart : list2[0].joints[i].toPart);
				if (part.connectionCheckId != Ref.connectionCheckId)
				{
					part.connectionCheckId = Ref.connectionCheckId;
					ResourceModule resourceModule = part.GetResourceModule();
					if (!(resourceModule == null) && start.resourceType == resourceModule.resourceType)
					{
						list2.Add(part);
						AddResourceModule(resourceModule);
					}
				}
			}
			list2.RemoveAt(0);
		}
	}

	public void TakeResource(double amount)
	{
		if (resourceAmount > amount)
		{
			resourceAmount -= amount;
		}
		else
		{
			resourceAmount = 0.0;
			empty = true;
		}
		SetTanks();
		if (!full)
		{
			return;
		}
		full = false;
		for (int i = 0; i < flowModules.Count; i++)
		{
			if (flowModules[i].state == FlowModule.State.NoSpace && flowModules[i].flowType == FlowModule.FlowType.Positive)
			{
				flowModules[i].withSpace.Add(this);
				flowModules[i].UpdateState();
			}
		}
	}

	public void AddResource(double amount)
	{
		if (resourceSpace - resourceAmount > amount)
		{
			resourceAmount += amount;
		}
		else
		{
			resourceAmount = resourceSpace;
			full = true;
		}
		SetTanks();
		if (!empty)
		{
			return;
		}
		empty = false;
		for (int i = 0; i < flowModules.Count; i++)
		{
			if (flowModules[i].state == FlowModule.State.NoResource && flowModules[i].flowType == FlowModule.FlowType.Negative)
			{
				flowModules[i].withResource.Add(this);
				flowModules[i].UpdateState();
			}
		}
	}

	private void SetTanks()
	{
		resourceAmount = Math.Min(resourceAmount, resourceSpace);
		resourcePercent = resourceAmount / resourceSpace;
		resourcePercent = Math.Min(resourcePercent, 1.0);
		for (int i = 0; i < resourceModules.Count; i++)
		{
			resourceModules[i].resourceAmount.floatValue = resourceModules[i].resourceSpace * (float)resourcePercent;
		}
		if (fuelIcon != null)
		{
			fuelIcon.GetChild(0).localScale = new Vector3((float)resourcePercent, fuelIcon.GetChild(0).localScale.y, 1f);
		}
	}

	public void DestroyGroup()
	{
		UnityEngine.Object.Destroy(this);
		for (int i = 0; i < resourceModules.Count; i++)
		{
			resourceModules[i].resourceGroup = null;
		}
	}
}
