using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Analytics;

public class Vessel : MonoBehaviour
{
	[Serializable]
	public class Throttle
	{
		public bool throttleOn;

		public float throttle;

		public float throttleRaw;

		public Throttle(bool throttleOn, float throttleRaw)
		{
			this.throttleOn = throttleOn;
			throttle = Mathf.Pow(throttleRaw, 1.6f);
			this.throttleRaw = throttleRaw;
		}
	}

	[Serializable]
	public class StationaryData
	{
		public Double3 posToPlane;

		public Planet planet;
	}

	public enum State
	{
		RealTime = 0,
		OnRails = 1,
		Stationary = 2,
		OnRailsUnloaded = 3,
		StationaryUnloaded = 4
	}

	public enum ToState
	{
		ToRealTime = 0,
		ToTimewarping = 1,
		ToUnloaded = 2
	}

	[BoxGroup]
	public bool controlAuthority;

	[BoxGroup]
	public float horizontalAxis;

	[BoxGroup]
	public bool RCS;

	public PartsManager partsManager;

	[FoldoutGroup("State", 0)]
	[Space]
	public State state;

	[FoldoutGroup("State", 0)]
	[Space]
	public List<Orbit> orbits;

	[FoldoutGroup("State", 0)]
	public StationaryData stationaryData;

	[Space]
	public Transform mapIcon;

	[Space]
	public Throttle throttle;

	[Space]
	public List<string> vesselAchievements;

	private float archivmentCheckTime;

	public Double3 GetGlobalPosition
	{
		get
		{
			if (state == State.RealTime)
			{
				return Ref.positionOffset + partsManager.rb2d.worldCenterOfMass;
			}
			if ((state == State.OnRails || state == State.OnRailsUnloaded) && orbits.Count > 0)
			{
				return orbits[0].GetPosOut(Ref.controller.globalTime);
			}
			return stationaryData.posToPlane;
		}
	}

	public Double3 GetGlobalVelocity
	{
		get
		{
			if (state == State.RealTime)
			{
				return Ref.velocityOffset + partsManager.rb2d.velocity;
			}
			if ((state == State.OnRails || state == State.OnRailsUnloaded) && orbits.Count > 0)
			{
				return orbits[0].GetVelOut(Ref.controller.globalTime);
			}
			return Double3.zero;
		}
	}

	public Planet GetVesselPlanet
	{
		get
		{
			if ((state == State.OnRails || state == State.OnRailsUnloaded) && orbits.Count > 0)
			{
				return orbits[0].planet;
			}
			if (state == State.RealTime)
			{
				return Ref.controller.loadedPlanet;
			}
			if (state == State.Stationary || state == State.StationaryUnloaded)
			{
				return stationaryData.planet;
			}
			return Ref.controller.loadedPlanet;
		}
	}

	public bool OnSurface
	{
		get
		{
			if (state == State.Stationary || state == State.StationaryUnloaded)
			{
				return true;
			}
			if (partsManager.rb2d == null)
			{
				return false;
			}
			Collider2D[] array = new Collider2D[5];
			partsManager.rb2d.GetContacts(array);
			Collider2D[] array2 = array;
			foreach (Collider2D collider2D in array2)
			{
				if (collider2D != null && collider2D.gameObject.layer == LayerMask.NameToLayer("Celestial Body"))
				{
					return true;
				}
			}
			return false;
		}
	}

	public void SetThrottle(Throttle newThrottle)
	{
		throttle = newThrottle;
		for (int i = 0; i < partsManager.engineModules.Count; i++)
		{
			partsManager.engineModules[i].UpdateEngineThrottle(newThrottle);
		}
		if (Ref.mainVessel == this)
		{
			Ref.controller.throttleOnUI.text = ((!throttle.throttleOn) ? "关" : "开");
			Ref.controller.throttleBar.fillAmount = throttle.throttleRaw;
			Ref.controller.throttleColorMove.SetTargetTime(throttle.throttleOn ? 1 : 0);
			float num = Mathf.Pow(throttle.throttleRaw, 1.6f) * 100f;
			if (num > 10f)
			{
				Ref.controller.throttlePercentUI.text = (int)num/*cast due to constrained. prefix*/ + "%";
			}
			else if (num >= 0.1f)
			{
				Ref.controller.throttlePercentUI.text = (int)num/*cast due to constrained. prefix*/ + "." + (int)(num % 1f * 10f)/*cast due to constrained. prefix*/ + "%";
			}
			else
			{
				Ref.controller.throttlePercentUI.text = ((!(num > 0f)) ? "0%" : "0.1%");
			}
		}
	}

	public static Vessel CreateVessel(Part startPart, Vector2 velocity, float angularVelocity, Throttle throttle, List<string> vesselArchivments, Transform mapIconHolder)
	{
		Vessel component = UnityEngine.Object.Instantiate(Ref.controller.vesselPrefab).GetComponent<Vessel>();
		Ref.controller.vessels.Add(component);
		component.partsManager.GetConnectedParts(startPart, component);
		component.partsManager.rb2d.velocity = velocity;
		component.partsManager.rb2d.angularVelocity = angularVelocity;
		component.mapIcon = UnityEngine.Object.Instantiate(Ref.map.mapVesselIconPrefab, mapIconHolder);
		Ref.map.UpdateVesselsMapIcons();
		component.SetThrottle(throttle);
		component.vesselAchievements = new List<string>(vesselArchivments);
		return component;
	}

	private void FixedUpdate()
	{
		if (state == State.RealTime)
		{
			partsManager.UpdateCenterOfMass();
			ApplyPhysics();
		}
	}

	private void Update()
	{
		CheckLoadDistance();
		Double3 getGlobalPosition = GetGlobalPosition;
		if (state == State.RealTime)
		{
			getGlobalPosition = Ref.positionOffset + partsManager.rb2d.worldCenterOfMass;
			if (Time.time > archivmentCheckTime)
			{
				archivmentCheckTime = Time.time + 1f;
				CheckForArchivments();
				CheckOrbitArchivments(new Orbit(getGlobalPosition, GetGlobalVelocity, Ref.controller.loadedPlanet));
			}
			if (getGlobalPosition.sqrMagnitude2d > Ref.controller.loadedPlanet.SOI * Ref.controller.loadedPlanet.SOI)
			{
				AddArchivment("到达" + Ref.controller.loadedPlanet.data.BASE_DATA.name + "影响范围");
				Planet parentBody = Ref.controller.loadedPlanet.parentBody;
				mapIcon.parent = parentBody.mapHolder;
				if (Ref.mainVessel == this)
				{
					Ref.controller.EnterTimeWarpMode();
					EnterNextOrbit();
					Ref.controller.ExitTimeWarpMode();
					getGlobalPosition = GetGlobalPosition;
				}
				else
				{
					partsManager.ReCenter();
					Double3 posIn = Ref.controller.loadedPlanet.GetPosOut(Ref.controller.globalTime) + getGlobalPosition;
					Double3 velIn = Ref.controller.loadedPlanet.GetVelOut(Ref.controller.globalTime) + GetGlobalVelocity;
					partsManager.rb2d.bodyType = RigidbodyType2D.Static;
					partsManager.rb2d.gameObject.SetActive(false);
					orbits = Orbit.CalculateOrbits(posIn, velIn, parentBody);
					state = State.OnRailsUnloaded;
				}
			}
			Planet[] satellites = Ref.controller.loadedPlanet.satellites;
			foreach (Planet planet in satellites)
			{
				Double3 @double = getGlobalPosition - planet.GetPosOut(Ref.controller.globalTime);
				if (@double.sqrMagnitude2d < planet.SOI * planet.SOI)
				{
					AddArchivment("进入" + planet.data.BASE_DATA.name + "影响范围");
					mapIcon.parent = planet.mapHolder;
					Double3 posIn2 = @double;
					Double3 velIn2 = GetGlobalVelocity - planet.GetVelOut(Ref.controller.globalTime);
					if (Ref.mainVessel == this)
					{
						Ref.controller.EnterTimeWarpMode();
						EnterNextOrbit();
						Ref.controller.ExitTimeWarpMode();
						getGlobalPosition = GetGlobalPosition;
					}
					else
					{
						partsManager.ReCenter();
						partsManager.rb2d.bodyType = RigidbodyType2D.Static;
						partsManager.rb2d.gameObject.SetActive(false);
						orbits = Orbit.CalculateOrbits(posIn2, velIn2, planet);
						state = State.OnRailsUnloaded;
					}
				}
			}
			if (Ref.mapView)
			{
				mapIcon.localPosition = (getGlobalPosition / 10000.0).toVector3;
				mapIcon.localRotation = partsManager.parts[0].transform.rotation;
			}
		}
		else
		{
			if (state != State.OnRails && state != State.OnRailsUnloaded)
			{
				return;
			}
			if (orbits[0].orbitEndTime < Ref.controller.globalTime)
			{
				EnterNextOrbit();
				getGlobalPosition = GetGlobalPosition;
			}
			if (orbits[0].calculatePassesTime != double.PositiveInfinity && Ref.controller.globalTime > orbits[0].calculatePassesTime - orbits[0].period * 0.9 && orbits[0].UpdatePass())
			{
				orbits = Orbit.CalculateOrbits(orbits);
			}
			if (Ref.mapView)
			{
				mapIcon.localPosition = (getGlobalPosition / 10000.0).toVector3;
			}
			else if (state == State.OnRails)
			{
				base.transform.position = (getGlobalPosition - Ref.positionOffset).toVector3;
			}
			if (Ref.mainVessel != this)
			{
				double sqrMagnitude2d = getGlobalPosition.sqrMagnitude2d;
				double num = orbits[0].planet.data.BASE_DATA.radius - 25.0 + PlanetData.TerrainModule.GetTerrainSampleAtAngle(Math.Atan2(getGlobalPosition.y, getGlobalPosition.x) / (Math.PI * 2.0) * 360.0, orbits[0].planet);
				if (sqrMagnitude2d < num * num)
				{
					DestroyVessel();
				}
			}
		}
	}

	private void EnterNextOrbit()
	{
		if (orbits[0].orbitType == Orbit.Type.Escape)
		{
			AddArchivment("到达" + orbits[0].planet.data.BASE_DATA.name + "影响范围.");
		}
		else if (orbits[0].orbitType == Orbit.Type.Encounter)
		{
			AddArchivment("进入" + orbits[0].nextPlanet.data.BASE_DATA.name + "影响范围.");
		}
		orbits.RemoveAt(0);
		orbits = Orbit.CalculateOrbits(orbits);
		mapIcon.parent = orbits[0].planet.mapHolder;
		if (Ref.mainVessel == this)
		{
			Ref.planetManager.SwitchLocation(orbits[0].planet, GetGlobalPosition, true, 0.0);
		}
	}

	private void CheckLoadDistance()
	{
		if (Ref.mainVessel == null)
		{
			return;
		}
		if (Ref.mainVessel == this)
		{
			if (!Ref.timeWarping && state == State.OnRails)
			{
				SetVesselState(ToState.ToRealTime);
				MonoBehaviour.print("Corrected");
			}
			if (Ref.timeWarping && state == State.OnRailsUnloaded)
			{
				SetVesselState(ToState.ToTimewarping);
				MonoBehaviour.print("Corrected");
			}
		}
		else if (Ref.controller.loadedPlanet == GetVesselPlanet && (GetGlobalPosition - Ref.mainVessel.GetGlobalPosition).sqrMagnitude2d < 25000000.0)
		{
			if (state == State.OnRailsUnloaded || state == State.StationaryUnloaded)
			{
				SetVesselState(Ref.timeWarping ? ToState.ToTimewarping : ToState.ToRealTime);
			}
		}
		else if (state == State.RealTime || state == State.OnRails || state == State.Stationary)
		{
			SetVesselState(ToState.ToUnloaded);
		}
	}

	private void CheckForArchivments()
	{
		if (Ref.controller.loadedPlanet.data.BASE_DATA.name != Ref.controller.startAdress)
		{
			if (OnSurface)
			{
				AddArchivment("着陆于" + Ref.controller.loadedPlanet.data.BASE_DATA.name + "表面.");
			}
			return;
		}
		Double3 @double = Ref.positionOffset + partsManager.rb2d.worldCenterOfMass;
		if (@double.magnitude2d > Ref.controller.loadedPlanet.data.BASE_DATA.radius + Ref.controller.loadedPlanet.data.ATMOSPHERE_DATA.PHYSICS.height)
		{
			AddArchivment("越过卡门线,离开大气层,进入太空.");
		}
		else if (@double.magnitude2d > Ref.controller.loadedPlanet.data.BASE_DATA.radius + 15000.0)
		{
			AddArchivment("达到15公里高度.");
		}
		else if (@double.magnitude2d > Ref.controller.loadedPlanet.data.BASE_DATA.radius + 10000.0)
		{
			AddArchivment("达到10公里高度.");
		}
		else if (@double.magnitude2d > Ref.controller.loadedPlanet.data.BASE_DATA.radius + 5000.0)
		{
			AddArchivment("达到5公里高度.");
		}
	}

	private void ApplyPhysics()
	{
		Planet loadedPlanet = Ref.controller.loadedPlanet;
		Double3 getGlobalPosition = GetGlobalPosition;
		if (!Ref.noGravity)
		{
			double num = loadedPlanet.mass / (getGlobalPosition.x * getGlobalPosition.x + getGlobalPosition.y * getGlobalPosition.y) * (double)Time.fixedDeltaTime;
			partsManager.rb2d.velocity -= (getGlobalPosition.normalized2d * num).toVector2;
		}
		if (throttle.throttleOn && throttle.throttleRaw > 0f)
		{
			partsManager.ApplyThrustForce(horizontalAxis, throttle, this);
		}
		if (!Ref.noDrag)
		{
			partsManager.ApplyDragForce(loadedPlanet, getGlobalPosition);
		}
		partsManager.ApplyTorqueForce(controlAuthority, ref horizontalAxis, this);
		Vector2 vector = ((!(this == Ref.mainVessel)) ? Vector2.zero : GetRcsInput());
		bool flag = vector.sqrMagnitude > 0f;
		if (RCS)
		{
			partsManager.ApplyRcs(this, horizontalAxis, (!flag || !controlAuthority) ? 0f : (Mathf.Atan2(vector.y, vector.x) * 57.29578f + Ref.main.Camera.transform.eulerAngles.z), flag);
		}
	}

	private Vector2 GetRcsInput()
	{
		return Ref.inputController.rcsInput;
	}

	public void SetVesselState(ToState toState)
	{
		if (toState == ToState.ToTimewarping)
		{
			if (state == State.RealTime)
			{
				state = GoOnRails(Ref.controller.loadedPlanet, true);
				base.name = "Vessel (" + state.ToString() + ")";
				return;
			}
			if (state == State.OnRailsUnloaded)
			{
				state = State.OnRails;
			}
			if (state == State.StationaryUnloaded)
			{
				state = State.Stationary;
			}
			partsManager.rb2d.gameObject.SetActive(true);
		}
		if (toState == ToState.ToUnloaded)
		{
			if (state == State.RealTime)
			{
				state = GoOnRails(Ref.controller.loadedPlanet, true);
			}
			if (state == State.OnRails)
			{
				state = State.OnRailsUnloaded;
			}
			if (state == State.Stationary)
			{
				state = State.StationaryUnloaded;
			}
			partsManager.rb2d.gameObject.SetActive(false);
			partsManager.rb2d.position = Vector3.zero;
		}
		if (toState == ToState.ToRealTime)
		{
			if (state == State.RealTime)
			{
				return;
			}
			Ref.planetManager.totalDistanceMoved += 5000.0;
			partsManager.rb2d.gameObject.SetActive(true);
			GoOffRails();
			state = State.RealTime;
		}
		base.name = "Vessel (" + state.ToString() + ")";
	}

	public State GoOnRails(Planet initialPlanet, bool reCenter)
	{
		if (reCenter)
		{
			partsManager.ReCenter();
		}
		Double3 getGlobalPosition = GetGlobalPosition;
		Double3 getGlobalVelocity = GetGlobalVelocity;
		partsManager.rb2d.bodyType = RigidbodyType2D.Static;
		foreach (EngineModule engineModule in partsManager.engineModules)
		{
			if (engineModule.nozzleMove != null)
			{
				engineModule.nozzleMove.SetTargetTime(0f);
			}
			engineModule.UpdateEngineThrottle(throttle);
		}
		stationaryData.posToPlane = getGlobalPosition;
		stationaryData.planet = initialPlanet;
		if (Math.Abs(getGlobalVelocity.x) < 0.3 && Math.Abs(getGlobalVelocity.y) < 0.3)
		{
			MonoBehaviour.print("low vel");
			orbits.Clear();
			mapIcon.localPosition = (stationaryData.posToPlane / 10000.0).toVector3;
			return State.Stationary;
		}
		orbits = Orbit.CalculateOrbits(getGlobalPosition, getGlobalVelocity, initialPlanet);
		if (double.IsNaN(orbits[0].meanMotion))
		{
			MonoBehaviour.print("Cannot orbit NaN, went stationary instead");
			orbits.Clear();
			mapIcon.localPosition = (stationaryData.posToPlane / 10000.0).toVector3;
			return State.Stationary;
		}
		CheckOrbitArchivments(orbits[0]);
		return State.OnRails;
	}

	public void GoOffRails()
	{
		Double3 getGlobalPosition = GetGlobalPosition;
		Vector3 toVector = (GetGlobalPosition - Ref.positionOffset).toVector3;
		double num = Math.Pow(Ref.controller.loadedPlanet.data.BASE_DATA.radius + Ref.controller.loadedPlanet.maxTerrainHeight + 10.0, 2.0);
		if (getGlobalPosition.sqrMagnitude2d < num && Ref.velocityOffset.sqrMagnitude2d > 0.0)
		{
			Ref.controller.OffsetSceneVelocity(-Ref.velocityOffset.toVector2);
		}
		Vector2 toVector2 = (GetGlobalVelocity - Ref.velocityOffset).toVector2;
		base.transform.position = toVector;
		partsManager.rb2d.bodyType = RigidbodyType2D.Dynamic;
		partsManager.rb2d.velocity = toVector2;
		foreach (EngineModule engineModule in partsManager.engineModules)
		{
			engineModule.UpdateEngineThrottle(throttle);
		}
		orbits.Clear();
		stationaryData.posToPlane = Double3.zero;
		stationaryData.planet = null;
	}

	public void DestroyVessel()
	{
		Ref.controller.vessels.Remove(this);
		partsManager.parts.Clear();
		if (Ref.mainVessel == this)
		{
			Ref.mainVessel = null;
			Ref.controller.RepositionFuelIcons();
		}
		if (mapIcon != null)
		{
			UnityEngine.Object.Destroy(mapIcon.gameObject);
		}
		if (base.gameObject != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void MergeVessel(Vessel otherVessel)
	{
		if (!(otherVessel == this))
		{
			if (otherVessel == Ref.mainVessel)
			{
				Ref.mainVessel = this;
			}
			partsManager.parts.AddRange(otherVessel.partsManager.parts);
			partsManager.GetConnectedParts(partsManager.parts[0], this);
			for (int i = 0; i < otherVessel.vesselAchievements.Count; i++)
			{
				AddArchivment(otherVessel.vesselAchievements[i]);
			}
			otherVessel.DestroyVessel();
		}
	}

	public void DisasembleVessel()
	{
		Ref.controller.vessels.Remove(this);
		partsManager.ResetsParts();
		List<Vessel> list = Ref.controller.CreateVesselsFromParts(partsManager.parts, partsManager.rb2d.velocity, partsManager.rb2d.angularVelocity, throttle, vesselAchievements);
		if (Ref.mainVessel == this && list.Count > 0)
		{
			Ref.controller.SwitchVessel(list[0]);
		}
		DestroyVessel();
	}

	public void AddArchivment(string newArchivment)
	{
		for (int i = 0; i < vesselAchievements.Count; i++)
		{
			if (vesselAchievements[i] == newArchivment)
			{
				return;
			}
		}
		Analytics.CustomEvent(newArchivment);
		vesselAchievements.Add(newArchivment);
		if (Ref.mainVessel != this)
		{
			return;
		}
		switch (newArchivment)
		{
		case "达到10公里高度.":
			return;
		case "达到15公里高度.":
			return;
		case "越过卡门线,离开大气层,进入太空.":
			newArchivment = "越过卡门线,离开大气层,进入太空.";
			if (!Saving.LoadSetting(Saving.SettingKey.seenMapInstructions))
			{
				if (!Ref.mapView)
				{
					Ref.inputController.instructionsMap.SetActive(true);
					Ref.inputController.CloseDropdownMenu();
				}
				Saving.SaveSetting(Saving.SettingKey.seenMapInstructions, true);
				return;
			}
			break;
		}
		if (newArchivment == "到达近地轨道." && !Saving.LoadSetting(Saving.SettingKey.seenTimewarpInstructions))
		{
			Ref.inputController.instructionsTimewarp.SetActive(true);
			Saving.SaveSetting(Saving.SettingKey.seenTimewarpInstructions, true);
		}
		newArchivment = newArchivment.Replace(".", "!");
		MenuManager.ShowMsg(Utility.SplitLines(newArchivment), 6f);
	}

	private void CheckOrbitArchivments(Orbit orbit)
	{
		if (orbit.orbitType == Orbit.Type.Eternal && orbit.periapsis > orbit.planet.data.BASE_DATA.radius + Math.Max((!orbit.planet.data.hasAtmosphere) ? 0.0 : orbit.planet.data.ATMOSPHERE_DATA.PHYSICS.height, orbit.planet.data.BASE_DATA.timewarpHeight))
		{
			AddArchivment(((!(orbit.planet.data.BASE_DATA.name == Ref.controller.startAdress)) ? "进入" : "达到最低点") + Ref.controller.loadedPlanet.data.BASE_DATA.name + " orbit.");
		}
	}
}
