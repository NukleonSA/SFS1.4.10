using System.Collections.Generic;
using NewBuildSystem;
using UnityEngine;

public class CreateRocket
{
	public static List<Part> CreateBuildParts(Vector3 createPos, Build.Blueprint toLaunch)
	{
		List<PlacedPart> list = Build.Blueprint.PlacedPartSave.FromSave(toLaunch.parts);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].partData == null || list[i].partData.prefab == null)
			{
				list.RemoveAt(i);
				i--;
			}
		}
		foreach (PlacedPart item in list)
		{
			item.UpdateInteractionKeys(list, Utility.Location.game);
			item.UpdateData();
		}
		List<Part> list2 = new List<Part>();
		foreach (PlacedPart item2 in list)
		{
			Part component = Object.Instantiate(item2.partData.prefab, createPos + (Vector3)item2.position, Quaternion.identity).GetComponent<Part>();
			component.orientation = item2.orientation.DeepCopy();
			Orientation.ApplyOrientation(component.transform, item2.orientation);
			component.skinId = item2.skinId;
			Utility.ApplySkin(component.transform, component.partData, component.skinId, component.orientation);
			list2.Add(component);
		}
		for (int j = 0; j < list.Count; j++)
		{
			for (int k = 0; k < list[j].appliedData.surfaces.Length; k++)
			{
				for (int l = j + 1; l < list.Count; l++)
				{
					for (int m = 0; m < list[l].appliedData.surfaces.Length; m++)
					{
						if (PlacedPart.SurfacesConnect(list[j], list[j].appliedData.surfaces[k], list[l], list[l].appliedData.surfaces[m]) > 0f)
						{
							bool resourceFlow = list[j].appliedData.surfaces[k].fuelFlow && list[l].appliedData.surfaces[m].fuelFlow;
							new Part.Joint(list[l].position - list[j].position, list2[j], list2[l], k, m, resourceFlow);
						}
					}
				}
			}
		}
		list2.Sort(SortByControl);
		return list2;
	}

	private static int SortByControl(Part a, Part b)
	{
		return ((b.GetComponent<ControlModule>() != null) ? 1 : 0) - ((a.GetComponent<ControlModule>() != null) ? 1 : 0);
	}

	public static Part CreatePart(Transform prefab, Orientation orientation, Transform parent, Vector3 localPosition, int skinId, List<string> interactionKeys)
	{
		Part component = Object.Instantiate(prefab).GetComponent<Part>();
		component.transform.parent = parent;
		component.transform.localPosition = localPosition;
		component.orientation = orientation.DeepCopy();
		Orientation.ApplyOrientation(component.transform, orientation);
		component.skinId = skinId;
		Utility.ApplySkin(component.transform, component.partData, component.skinId, component.orientation);
		component.interactionKeys = interactionKeys;
		return component;
	}
}
