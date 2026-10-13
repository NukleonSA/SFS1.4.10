using System;
using System.Collections.Generic;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;

public class PartsManager : MonoBehaviour
{
	public List<Part> parts;

	public bool partToPartDamage = true;

	public Rigidbody2D rb2d;

	[BoxGroup]
	public float torque;

	[FoldoutGroup("Modules", 0)]
	public List<ControlModule> controlModules;

	[FoldoutGroup("Modules", 0)]
	public List<EngineModule> engineModules;

	[FoldoutGroup("Modules", 0)]
	public List<TorqueModule> torqueModules;

	[FoldoutGroup("Modules", 0)]
	public List<RcsModule> rcsModules;

	[FoldoutGroup("Modules", 0)]
	public List<SplitModule> fairingModules;

	[FoldoutGroup("Resources", 0)]
	public List<FlowModule> flowModules;

	[FoldoutGroup("Resources", 0)]
	public List<ResourceGroup> resourceGrups;

	[FoldoutGroup("Resources", 0)]
	public List<ResourceGroup> rcsFuel;

	public void ApplyThrustForce(float engineGimbal, Vessel.Throttle throttle, Vessel vessel)
	{
		if (Ref.mainVessel == vessel && Ref.mainVesselTerrainHeight < 20.0 && (!throttle.throttleOn || throttle.throttleRaw == 0f))
		{
			engineGimbal = 0f;
		}
		float totalThrust = 0f;
		for (int i = 0; i < engineModules.Count; i++)
		{
			engineModules[i].ApplyThrust(ref totalThrust, rb2d, (!engineModules[i].engineOn.boolValue) ? 0f : engineGimbal, throttle, Ref.mainVessel == vessel);
		}
		if (totalThrust != 0f && vessel == Ref.mainVessel)
		{
			Ref.controller.cameraOffset = totalThrust * 3E-05f * UnityEngine.Random.insideUnitCircle;
		}
	}

	public void ApplyDragForce(Planet planet, Double3 posToPlanet)
	{
		if (!planet.data.hasAtmosphere || planet.data.ATMOSPHERE_DATA.PHYSICS.density == 0.0)
		{
			return;
		}
		Double3 @double = Ref.velocityOffset + rb2d.velocity;
		if (1.0 > @double.sqrMagnitude)
		{
			return;
		}
		double num = posToPlanet.magnitude2d - planet.data.BASE_DATA.radius;
		if (!(num > planet.data.ATMOSPHERE_DATA.PHYSICS.height))
		{
			double num2 = Math.Exp((0.0 - num / planet.data.ATMOSPHERE_DATA.PHYSICS.height) * planet.data.ATMOSPHERE_DATA.PHYSICS.curve) * planet.data.ATMOSPHERE_DATA.PHYSICS.density;
			float sumedDrag = 0f;
			Vector2 sumedCenterOfDrag = Vector2.zero;
			float velocityAngle = Mathf.Atan2((float)@double.y, (float)@double.x);
			float rot = (rb2d.rotation + 90f) * ((float)Math.PI / 180f);
			for (int i = 0; i < parts.Count; i++)
			{
				parts[i].GetDrag(velocityAngle, rot, ref sumedDrag, ref sumedCenterOfDrag);
			}
			sumedCenterOfDrag /= sumedDrag;
			if (!float.IsNaN(sumedCenterOfDrag.x))
			{
				rb2d.AddForceAtPosition(-(@double.normalized2d * @double.sqrMagnitude * num2).toVector2 * sumedDrag, rb2d.GetRelativePoint(sumedCenterOfDrag));
			}
		}
	}

	public void ApplyTorqueForce(bool controlAuthority, ref float horizontalAxis, Vessel vessel)
	{
		if (torque <= 0f)
		{
			return;
		}
		if (!controlAuthority)
		{
			if (Ref.mainVessel == vessel && Ref.inputController.horizontalAxis != 0f && MenuManager.main.msgText.color.a < 0.6f)
			{
				MenuManager.ShowMsg("失去控制");
			}
			horizontalAxis = 0f;
			return;
		}
		float num = torque / rb2d.mass / Time.fixedDeltaTime;
		if (Ref.mainVessel == vessel && Ref.inputController.horizontalAxis != 0f)
		{
			horizontalAxis = Ref.inputController.horizontalAxis;
			rb2d.angularVelocity -= num * Ref.inputController.horizontalAxis * Time.fixedDeltaTime;
		}
		else
		{
			horizontalAxis = Mathf.Clamp(rb2d.angularVelocity / (num * Time.fixedDeltaTime), -1f, 1f);
			rb2d.angularVelocity -= num * horizontalAxis * Time.fixedDeltaTime;
		}
	}

	public void ApplyRcs(Vessel vessel, float horizontalAxis, float rcsInputDirection, bool directionInput)
	{
		if (rcsModules.Count == 0)
		{
			vessel.RCS = false;
			return;
		}
		double fuelUsed = 0.0;
		for (int i = 0; i < rcsModules.Count; i++)
		{
			rcsModules[i].UseRcs(rb2d, vessel, horizontalAxis, rcsInputDirection, directionInput, ref fuelUsed);
		}
		if (!TakeFromAllFuelGrups(fuelUsed))
		{
			vessel.RCS = false;
			for (int j = 0; j < rcsModules.Count; j++)
			{
				rcsModules[j].effect.SetTargetTime(0f);
			}
		}
	}

	public void ToggleRCS(Vessel vessel, bool showMsg)
	{
		if (!vessel.RCS)
		{
			if (rcsFuel.Count == 0)
			{
				MenuManager.ShowMsg("没有燃料来源", showMsg);
				return;
			}
			if (!TakeFromAllFuelGrups(0.0))
			{
				MenuManager.ShowMsg("没有燃料", 2f, showMsg);
				return;
			}
		}
		vessel.RCS = !vessel.RCS;
		if (!vessel.RCS)
		{
			for (int i = 0; i < rcsModules.Count; i++)
			{
				rcsModules[i].effect.SetTargetTime(0f);
			}
		}
		MenuManager.ShowMsg("RCS " + ((!vessel.RCS) ? "关" : "开"), showMsg);
	}

	private bool TakeFromAllFuelGrups(double removeAmount)
	{
		if (Ref.infiniteFuel)
		{
			return true;
		}
		int num = 0;
		for (int i = 0; i < resourceGrups.Count; i++)
		{
			if (!resourceGrups[i].empty)
			{
				num++;
			}
		}
		if (num == 0)
		{
			return false;
		}
		for (int j = 0; j < resourceGrups.Count; j++)
		{
			if (!resourceGrups[j].empty)
			{
				resourceGrups[j].TakeResource(removeAmount / (double)num);
			}
		}
		return true;
	}

	public void ReCenter()
	{
		rb2d.transform.parent = null;
		base.transform.position = rb2d.worldCenterOfMass;
		rb2d.transform.parent = base.transform;
	}

	public void UpdatePartsGruping(bool disablePartToPartDamage, Vessel vessel)
	{
		if (parts.Count > 0 && parts[0] != null && parts[0].gameObject.activeSelf)
		{
			ResetsParts();
			List<Part> list = new List<Part>(parts);
			GetConnectedParts(parts[0], vessel);
			List<Part> list2 = new List<Part>();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].vessel == null)
				{
					list2.Add(list[i]);
				}
			}
			List<Vessel> list3 = Ref.controller.CreateVesselsFromParts(list2, rb2d.velocity, rb2d.angularVelocity, vessel.throttle, vessel.vesselAchievements);
			if (Ref.mainVessel == vessel)
			{
				Ref.controller.RepositionFuelIcons();
			}
			if (disablePartToPartDamage)
			{
				DisablePartToPartDamage(0.1f);
				for (int j = 0; j < list3.Count; j++)
				{
					list3[j].partsManager.DisablePartToPartDamage(0.1f);
				}
			}
		}
		else
		{
			vessel.DisasembleVessel();
		}
	}

	private void DisablePartToPartDamage(float disableTime)
	{
		partToPartDamage = false;
		StopCoroutine("EnablePartToPartDamage");
		Invoke("EnablePartToPartDamage", disableTime);
	}

	private void EnablePartToPartDamage()
	{
		partToPartDamage = true;
	}

	public void GetConnectedParts(Part startPart, Vessel vessel)
	{
		for (int i = 0; i < parts.Count; i++)
		{
			parts[i].vessel = null;
		}
		parts.Clear();
		rb2d.transform.SetPositionAndRotation(startPart.transform.position, Quaternion.Euler(new Vector3(0f, 0f, startPart.transform.eulerAngles.z - (float)startPart.orientation.z)));
		startPart.transform.parent = rb2d.transform;
		startPart.transform.localPosition = Vector3.zero;
		startPart.vessel = vessel;
		parts.Add(startPart);
		List<Part> list = new List<Part>(1);
		list.Add(startPart);
		List<Part> list2 = list;
		while (list2.Count > 0)
		{
			Orientation.ApplyOrientation(list2[0].transform, list2[0].orientation);
			for (int j = 0; j < list2[0].joints.Count; j++)
			{
				Part part = ((!(list2[0].joints[j].fromPart == list2[0])) ? list2[0].joints[j].fromPart : list2[0].joints[j].toPart);
				if (part.vessel == null)
				{
					list2.Add(part);
					part.vessel = vessel;
					parts.Add(part);
					part.transform.parent = rb2d.transform;
					part.transform.localPosition = list2[0].transform.localPosition + (Vector3)list2[0].joints[j].RelativeAnchor(list2[0]);
				}
			}
			list2.RemoveAt(0);
		}
		SortModules(GetModules(), vessel);
		UpdateCenterOfMass();
	}
	private List<Module> GetModules()
	{
		List<Module> list = new List<Module>();
		for (int i = 0; i < parts.Count; i++)
		{
			list.AddRange(parts[i].modules);
		}
		return list;
	}

	private void SortModules(List<Module> modules, Vessel vessel)
	{
		controlModules.Clear();
		torqueModules.Clear();
		engineModules.Clear();
		fairingModules.Clear();
		flowModules.Clear();
		rcsModules.Clear();
		rcsFuel.Clear();
		while (resourceGrups.Count > 0)
		{
			resourceGrups[0].DestroyGroup();
			resourceGrups.RemoveAt(0);
		}
		List<ResourceModule> list = new List<ResourceModule>();
		for (int i = 0; i < modules.Count; i++)
		{
			if (modules[i].IsSorted())
			{
				if (modules[i] is ResourceModule)
				{
					list.Add(modules[i] as ResourceModule);
				}
				else if (modules[i] is FlowModule)
				{
					flowModules.Add(modules[i] as FlowModule);
				}
				else if (modules[i] is TorqueModule)
				{
					torqueModules.Add(modules[i] as TorqueModule);
				}
				else if (modules[i] is EngineModule)
				{
					engineModules.Add(modules[i] as EngineModule);
				}
				else if (modules[i] is ControlModule)
				{
					controlModules.Add(modules[i] as ControlModule);
				}
				else if (modules[i] is RcsModule)
				{
					rcsModules.Add(modules[i] as RcsModule);
				}
				else if (modules[i] is SplitModule)
				{
					fairingModules.Add(modules[i] as SplitModule);
				}
			}
		}
		UpdateTorque();
		vessel.controlAuthority = controlModules.Count > 0;
		SortResourceGrups(list);
		for (int j = 0; j < flowModules.Count; j++)
		{
			flowModules[j].GetSource();
		}
		for (int k = 0; k < resourceGrups.Count; k++)
		{
			if (resourceGrups[k].resourceType == Resource.Type.Fuel)
			{
				rcsFuel.Add(resourceGrups[k]);
			}
		}
	}

	private void SortResourceGrups(List<ResourceModule> resourceModules)
	{
		for (int i = 0; i < resourceModules.Count; i++)
		{
			if (!(resourceModules[i].resourceGroup != null))
			{
				ResourceGroup resourceGroup = base.gameObject.AddComponent<ResourceGroup>();
				resourceGroup.Initialize(resourceModules, i, resourceModules[i].resourceType);
				if (resourceGroup.resourceModules.Count != 0)
				{
					resourceGrups.Add(resourceGroup);
				}
			}
		}
		if (Ref.mainVessel != null && Ref.mainVessel.partsManager == this)
		{
			Ref.controller.RepositionFuelIcons();
		}
	}

	public void ResetsParts()
	{
		for (int i = 0; i < rcsModules.Count; i++)
		{
			rcsModules[i].effect.SetTargetTime(0f);
		}
		foreach (ResourceGroup resourceGrup in resourceGrups)
		{
			resourceGrup.DestroyGroup();
		}
		resourceGrups = new List<ResourceGroup>();
		foreach (Part part in parts)
		{
			if (part.gameObject.activeSelf)
			{
				part.vessel = null;
			}
		}
	}

	public void UpdateCenterOfMass()
	{
		float num = 0f;
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < parts.Count; i++)
		{
			num += parts[i].mass;
			zero += (parts[i].transform.localPosition + parts[i].centerOfMass * parts[i].orientation) * parts[i].mass;
		}
		rb2d.mass = num;
		rb2d.centerOfMass = zero / num;
	}

	public float GetPhysicalRadius()
	{
		Vector3 vector = rb2d.centerOfMass;
		float num = 0f;
		for (int i = 0; i < parts.Count; i++)
		{
			float sqrMagnitude = (parts[i].transform.localPosition + parts[i].centerOfMass * parts[i].orientation - vector).sqrMagnitude;
			num = ((!(sqrMagnitude > num)) ? num : sqrMagnitude);
		}
		return Mathf.Sqrt(num) + 2f;
	}

	public void UpdateTorque()
	{
		torque = 0f;
		for (int i = 0; i < torqueModules.Count; i++)
		{
			torque += torqueModules[i].torque.floatValue;
		}
	}
}
