using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SeparatorModule : Module
{
	public BoolValueHolder separated;

	public Vector2 separationForce;

	public bool showParametersDescription = true;

	public UnityEvent onSeparate;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(separated);
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
				list.Add("分离力:" + separationForce.magnitude + "kN");
				result = list;
			}
			else
			{
				result = new List<string>();
			}
			return result;
		}
	}

	public override void OnPartUsed()
	{
		if (!separated.boolValue)
		{
			if (Ref.timeWarping)
			{
				MenuManager.ShowMsg("Cannot use part while time warping");
				return;
			}
			separated.boolValue = true;
			onSeparate.Invoke();
		}
	}

	public void SetSeparated(bool value)
	{
		separated.boolValue = value;
	}

	public void SeparateSurface(int surfaceId)
	{
		List<Part> list = new List<Part>();
		List<float> list2 = new List<float>();
		float num = 0f;
		foreach (Part.Joint item in new List<Part.Joint>(part.joints))
		{
			int num2 = ((!(item.fromPart == part)) ? item.toSurfaceIndex : item.fromSurfaceIndex);
			if (num2 == surfaceId)
			{
				list.Add((!(item.fromPart == part)) ? item.fromPart : item.toPart);
				list2.Add((!(item.fromPart == part)) ? item.coveredAmount : item.coveredAmount);
				num += list2[list2.Count - 1];
				Part.DestroyJoint(item, true);
			}
		}
		Vector2 relativePoint = part.vessel.partsManager.rb2d.GetRelativePoint(base.transform.localPosition + part.centerOfMass * part.orientation);
		Vector2 vector = Quaternion.Euler(0f, 0f, base.transform.rotation.eulerAngles.z) * (separationForce * part.orientation);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].vessel.partsManager.rb2d != null && !float.IsNaN(list2[i] / num))
			{
				list[i].vessel.partsManager.rb2d.AddForceAtPosition(vector * (list2[i] / num), relativePoint);
			}
		}
		if (list.Count > 0 && part.vessel.partsManager.rb2d != null)
		{
			part.vessel.partsManager.rb2d.AddForceAtPosition(-vector, relativePoint);
		}
	}

	public void ApplyForce()
	{
		Vector2 position = part.vessel.partsManager.transform.TransformPoint(base.transform.localPosition + part.centerOfMass * part.orientation);
		Vector2 force = Quaternion.Euler(0f, 0f, base.transform.rotation.eulerAngles.z) * new Vector2(separationForce.x * (float)part.orientation.x, separationForce.y * (float)part.orientation.y);
		part.vessel.partsManager.rb2d.AddForceAtPosition(force, position);
	}

	public static void TransferJoints(Part fromPart, int transferSurfaceId, Part toPart, int toSurfaceId)
	{
		for (int i = 0; i < fromPart.joints.Count; i++)
		{
			Part.Joint joint = fromPart.joints[i];
			bool flag = joint.fromPart == fromPart;
			if (((!flag) ? joint.toSurfaceIndex : joint.fromSurfaceIndex) == transferSurfaceId)
			{
				if (flag)
				{
					joint.fromPart = toPart;
					joint.fromSurfaceIndex = toSurfaceId;
				}
				else
				{
					joint.toPart = toPart;
					joint.toSurfaceIndex = toSurfaceId;
				}
				toPart.joints.Add(joint);
				fromPart.joints.RemoveAt(i);
				i--;
			}
		}
	}
}
