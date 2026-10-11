using System.Collections.Generic;
using NewBuildSystem;
using UnityEngine;

public class DockingPortModule : Module
{
	public float pullDistance;

	public float pullForce;

	public float dockDistance;

	private static List<DockingPortModule> dockingPorts = new List<DockingPortModule>();

	private void OnEnable()
	{
		dockingPorts.Add(this);
	}

	private void OnDisable()
	{
		dockingPorts.Remove(this);
	}

	public override void OnModuleLoaded()
	{
		for (int i = 0; i < base.part.joints.Count; i++)
		{
			Part part = ((!(base.part.joints[i].fromPart == base.part)) ? base.part.joints[i].fromPart : base.part.joints[i].toPart);
			if (part.HasDockingPortModule())
			{
				base.enabled = false;
			}
		}
	}

	private void Update()
	{
		for (int i = 0; i < dockingPorts.Count; i++)
		{
			if (part.vessel != dockingPorts[i].part.vessel && this != dockingPorts[i] && dockingPorts[i].part.vessel != Ref.mainVessel && !((base.transform.position - dockingPorts[i].transform.position).sqrMagnitude > pullDistance * pullDistance))
			{
				part.vessel.partsManager.rb2d.AddForceAtPosition(-(base.transform.position - dockingPorts[i].transform.position).normalized * pullForce * Time.fixedDeltaTime, part.vessel.partsManager.rb2d.GetRelativePoint(base.transform.localPosition));
				if ((base.transform.position - dockingPorts[i].transform.position).sqrMagnitude < dockDistance * dockDistance)
				{
					Dock(dockingPorts[i]);
				}
			}
		}
	}

	private void Dock(DockingPortModule other)
	{
		new Part.Joint(Vector2.zero, part, other.part, 0, 0, false);
		Vector2 vector = Vector2.up * part.orientation;
		Vector2 vector2 = Vector2.up * other.part.orientation;
		Orientation orientation = new Orientation(1, 1, (int)(Mathf.Atan2(vector.y, vector.x) * 57.29578f - Mathf.Atan2(vector2.y, vector2.x) * 57.29578f) + 180);
		orientation.z = Mathf.RoundToInt((float)orientation.z / 90f) * 90;
		for (int i = 0; i < other.part.vessel.partsManager.parts.Count; i++)
		{
			other.part.vessel.partsManager.parts[i].orientation += orientation;
			for (int j = 0; j < other.part.vessel.partsManager.parts[i].joints.Count; j++)
			{
				if (other.part.vessel.partsManager.parts[i].joints[j].fromPart == other.part.vessel.partsManager.parts[i])
				{
					other.part.vessel.partsManager.parts[i].joints[j].anchor *= orientation;
				}
			}
		}
		part.vessel.MergeVessel(other.part.vessel);
		part.UpdateDragSurfaces();
		other.part.UpdateDragSurfaces();
		base.enabled = false;
		other.enabled = false;
	}

	public void EnableOther()
	{
		for (int i = 0; i < base.part.joints.Count; i++)
		{
			Part part = ((!(base.part.joints[i].fromPart == base.part)) ? base.part.joints[i].fromPart : base.part.joints[i].toPart);
			if (part.HasDockingPortModule())
			{
				part.GetComponent<DockingPortModule>().enabled = true;
			}
		}
	}

	public void Enable()
	{
		base.enabled = true;
	}
}
