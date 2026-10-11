using System;
using System.Collections.Generic;
using NewBuildSystem;
using UnityEngine;

public class GameSaver : MonoBehaviour
{
	[Serializable]
	public class Quicksave
	{
		public string saveName;

		public double globalTime;

		public int timeWarpPhase;

		public double startedTimewapTime;

		public float camDistance;

		public bool mapView;

		public Double3 mapPosition;

		public string mapFollowingAdress;

		public int mainVesselId;

		public List<VesselSave> vessels;

		public Double3 positionOffset;

		public Double3 velocityOffset;

		public Quicksave(string saveName, int mainVesselId, List<VesselSave> vessels, Double3 positionOffset, Double3 velocityOffset, double globalTime, int timeWarpPhase, double startedTimewapTime, float camDistance, bool mapView, Double3 mapPosition, string mapFollowingAdress)
		{
			this.saveName = saveName;
			this.globalTime = globalTime;
			this.timeWarpPhase = timeWarpPhase;
			this.startedTimewapTime = startedTimewapTime;
			this.camDistance = camDistance;
			this.mapView = mapView;
			this.mapPosition = mapPosition;
			this.mapFollowingAdress = mapFollowingAdress;
			this.mainVesselId = mainVesselId;
			this.vessels = vessels;
			this.positionOffset = positionOffset;
			this.velocityOffset = velocityOffset;
		}

		public static Quicksave CreateQuicksave(string saveName)
		{
			return new Quicksave(saveName, Ref.controller.vessels.IndexOf(Ref.mainVessel), GetVesselSaveData(Ref.controller.vessels), Ref.positionOffset, Ref.velocityOffset, Ref.controller.globalTime, Ref.controller.timewarpPhase, Ref.controller.startedTimewarpTime, Ref.controller.cameraDistanceGame, Ref.mapView, Ref.map.mapPosition, Ref.map.followingPlanet.data.BASE_DATA.name);
		}
	}

	[Serializable]
	public class VesselSave
	{
		public string adress;

		public Double3 globalPosition;

		public Double3 globalVelocity;

		public float rotation;

		public float angularVelocity;

		public Vessel.State state;

		public Vessel.Throttle throttle;

		public Part.Save[] parts;

		public Part.Joint.Save[] joints;

		public List<string> vesselArchivments;

		public bool RCS;

		public VesselSave(Vessel.State state, string adress, Double3 globalPosition, Double3 globalVelocity, float rotation, float angularVelocity, Vessel.Throttle throttle, Part.Save[] parts, Part.Joint.Save[] joints, List<string> vesselArchivments, bool RCS)
		{
			this.globalPosition = globalPosition;
			this.globalVelocity = globalVelocity;
			this.rotation = rotation;
			this.angularVelocity = angularVelocity;
			this.adress = adress;
			this.state = state;
			this.throttle = throttle;
			this.parts = parts;
			this.joints = joints;
			this.vesselArchivments = vesselArchivments;
			this.RCS = RCS;
		}
	}

	private static List<VesselSave> GetVesselSaveData(List<Vessel> vesselsToSave)
	{
		List<VesselSave> list = new List<VesselSave>();
		for (int i = 0; i < vesselsToSave.Count; i++)
		{
			list.Add(new VesselSave(vesselsToSave[i].state, vesselsToSave[i].GetVesselPlanet.data.BASE_DATA.name, vesselsToSave[i].GetGlobalPosition, vesselsToSave[i].GetGlobalVelocity, vesselsToSave[i].partsManager.rb2d.rotation, vesselsToSave[i].partsManager.rb2d.angularVelocity, vesselsToSave[i].throttle, GetPartsSaveData(vesselsToSave[i].partsManager.parts), GetPartsJointsSave(vesselsToSave[i].partsManager.parts), vesselsToSave[i].vesselAchievements, vesselsToSave[i].RCS));
		}
		return list;
	}

	private static Part.Save[] GetPartsSaveData(List<Part> partsToSave)
	{
		int count = partsToSave.Count;
		Part.Save[] array = new Part.Save[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = new Part.Save(partsToSave[i], partsToSave);
		}
		return array;
	}

	private static Part.Joint.Save[] GetPartsJointsSave(List<Part> parts)
	{
		List<Part.Joint.Save> list = new List<Part.Joint.Save>();
		foreach (Part part in parts)
		{
			foreach (Part.Joint joint in part.joints)
			{
				if (joint.fromPart == part)
				{
					list.Add(new Part.Joint.Save(Part.GetPartListIndex(joint.fromPart, parts), Part.GetPartListIndex(joint.toPart, parts), joint.anchor, joint.fromSurfaceIndex, joint.toSurfaceIndex, joint.resourceFlow));
				}
			}
		}
		return list.ToArray();
	}

	public static void LoadGame(Quicksave loadedData)
	{
		ClearScene();
		if (loadedData.mainVesselId == -1)
		{
			LoadForLaunch(loadedData, new Double3(-400.0, Ref.controller.startAdress.Planet().data.BASE_DATA.radius, 0.0));
			return;
		}
		Planet planet = ((loadedData.mainVesselId == -1) ? Ref.controller.startAdress.Planet() : loadedData.vessels[loadedData.mainVesselId].adress.Planet());
		Ref.planetManager.SwitchLocation(planet, (loadedData.mainVesselId == -1) ? new Double3(0.0, planet.data.BASE_DATA.radius, 0.0) : loadedData.vessels[loadedData.mainVesselId].globalPosition, false, 0.0);
		Ref.velocityOffset = loadedData.velocityOffset;
		Ref.controller.globalTime = loadedData.globalTime;
		Ref.controller.timewarpPhase = loadedData.timeWarpPhase;
		Ref.controller.startedTimewarpTime = loadedData.startedTimewapTime;
		Ref.timeWarping = Ref.controller.timewarpPhase != 0;
		Ref.controller.SetCameraDistance(loadedData.camDistance);
		foreach (VesselSave vessel in loadedData.vessels)
		{
			LoadVessel(vessel);
		}
		if (loadedData.mainVesselId != -1)
		{
			Ref.mainVessel = Ref.controller.vessels[loadedData.mainVesselId];
		}
		Ref.mainVessel.partsManager.UpdateCenterOfMass();
		Ref.planetManager.totalDistanceMoved += 100000.0;
		Ref.planetManager.UpdateChuncksLoading((loadedData.mainVesselId != -1) ? loadedData.vessels[loadedData.mainVesselId].adress.Planet() : Ref.controller.startAdress.Planet());
		Ref.map.UpdateVesselsMapIcons();
		Ref.map.followingPlanet = loadedData.mapFollowingAdress.Planet();
		Ref.map.UpdateMapPosition(loadedData.mapPosition);
		Ref.map.UpdateMapZoom(loadedData.mapPosition.z);
		Ref.map.ToggleMap();
		if (Ref.mapView != loadedData.mapView)
		{
			Ref.map.ToggleMap();
		}
		Ref.mainVessel.SetThrottle(Ref.mainVessel.throttle);
		Ref.controller.RepositionFuelIcons();
		Ref.planetManager.UpdateFog(loadedData.camDistance);
		Ref.mainVesselHeight = loadedData.vessels[loadedData.mainVesselId].globalPosition.magnitude2d - loadedData.vessels[loadedData.mainVesselId].adress.Planet().data.BASE_DATA.radius;
		Ref.mainVesselAngleToPlanet = (float)Math.Atan2(loadedData.vessels[loadedData.mainVesselId].globalPosition.y, loadedData.vessels[loadedData.mainVesselId].globalPosition.x) * 57.29578f;
		if (Ref.mainVesselHeight < loadedData.vessels[loadedData.mainVesselId].adress.Planet().data.BASE_DATA.timewarpHeight)
		{
			Ref.controller.camTargetAngle = Ref.mainVesselAngleToPlanet - 90f;
		}
		else
		{
			Ref.controller.camTargetAngle = 0f;
		}
		Ref.main.Camera.transform.eulerAngles = new Vector3(0f, 0f, Ref.controller.camTargetAngle);
		Ref.controller.camAngularVelocity = 0f;
	}

	public static void LoadForLaunch(Quicksave loadedData, Double3 launchPadPosition)
	{
		loadedData.velocityOffset = new Double3(0.0, 0.0, 0.0);
		for (int i = 0; i < loadedData.vessels.Count; i++)
		{
			if (loadedData.vessels[i].adress == Ref.controller.startAdress && Math.Abs(loadedData.vessels[i].globalPosition.x - launchPadPosition.x) < 12.0 && Math.Abs(loadedData.vessels[i].globalPosition.y - launchPadPosition.y) < 150.0)
			{
				loadedData.vessels.RemoveAt(i);
				i--;
				continue;
			}
			switch (loadedData.vessels[i].state)
			{
			case Vessel.State.RealTime:
				if ((loadedData.vessels[i].globalPosition - launchPadPosition).magnitude2d > 1000.0 || loadedData.vessels[i].adress != Ref.controller.startAdress)
				{
					loadedData.vessels[i].state = ((!(Math.Abs(loadedData.vessels[i].globalVelocity.x) > 1.0) && !(Math.Abs(loadedData.vessels[i].globalVelocity.y) > 1.0)) ? Vessel.State.StationaryUnloaded : Vessel.State.OnRailsUnloaded);
				}
				break;
			case Vessel.State.OnRails:
				loadedData.vessels[i].state = Vessel.State.OnRailsUnloaded;
				break;
			case Vessel.State.Stationary:
				loadedData.vessels[i].state = Vessel.State.StationaryUnloaded;
				break;
			}
		}
		Ref.planetManager.SwitchLocation(Ref.controller.startAdress.Planet(), launchPadPosition, false, 0.0);
		Ref.planetManager.UpdatePositionOffset(new Double3(0.0, 315000.0));
		Ref.velocityOffset = Double3.zero;
		Ref.controller.globalTime = loadedData.globalTime;
		Ref.controller.timewarpPhase = 0;
		Ref.timeWarping = false;
		foreach (VesselSave vessel in loadedData.vessels)
		{
			LoadVessel(vessel);
		}
		Ref.map.followingPlanet = Ref.controller.startAdress.Planet();
		Ref.map.UpdateMapPosition(new Double3(0.0, launchPadPosition.y, launchPadPosition.y / 10.0));
		Ref.planetManager.UpdateFog(loadedData.camDistance);
	}

	private static Vessel LoadVessel(VesselSave vesselToLoad)
	{
		List<Part> list = LoadParts(vesselToLoad);
		LoadPartsData(vesselToLoad, list.ToArray());
		list.Remove(null);
		Orientation.ApplyOrientation(list[0].transform, list[0].orientation);
		Vessel vessel = Vessel.CreateVessel(list[0].GetComponent<Part>(), Vector2.zero, vesselToLoad.angularVelocity, vesselToLoad.throttle, vesselToLoad.vesselArchivments, vesselToLoad.adress.Planet().mapHolder);
		foreach (Part part in vessel.partsManager.parts)
		{
			part.UpdateIcon();
			part.UpdateDragSurfaces();
		}
		vessel.partsManager.UpdateCenterOfMass();
		vessel.partsManager.ReCenter();
		vessel.transform.position = (vesselToLoad.globalPosition - Ref.positionOffset).toVector2;
		vessel.transform.eulerAngles = new Vector3(0f, 0f, vesselToLoad.rotation);
		if (vesselToLoad.state == Vessel.State.RealTime)
		{
			vessel.partsManager.rb2d.velocity = (vesselToLoad.globalVelocity - Ref.velocityOffset).toVector2;
		}
		else if (vesselToLoad.state == Vessel.State.OnRails || vesselToLoad.state == Vessel.State.OnRailsUnloaded)
		{
			vessel.orbits = Orbit.CalculateOrbits(vesselToLoad.globalPosition, vesselToLoad.globalVelocity, vesselToLoad.adress.Planet());
			vessel.partsManager.rb2d.bodyType = RigidbodyType2D.Static;
			vessel.state = Vessel.State.OnRails;
			if (vessel.state == Vessel.State.OnRailsUnloaded)
			{
				vessel.SetVesselState(Vessel.ToState.ToUnloaded);
			}
			if (double.IsNaN(vessel.orbits[0].meanMotion))
			{
				MonoBehaviour.print("Cannot orbit NaN, went stationary instead");
				vessel.orbits.Clear();
				vessel.stationaryData.posToPlane = vesselToLoad.globalPosition;
				vessel.stationaryData.planet = vesselToLoad.adress.Planet();
				vessel.state = Vessel.State.Stationary;
				vessel.SetVesselState(Vessel.ToState.ToUnloaded);
				vessel.mapIcon.localPosition = (vessel.stationaryData.posToPlane / 10000.0).toVector3;
			}
		}
		else
		{
			vessel.stationaryData.posToPlane = vesselToLoad.globalPosition;
			vessel.stationaryData.planet = vesselToLoad.adress.Planet();
			vessel.state = Vessel.State.Stationary;
			if (vesselToLoad.state == Vessel.State.StationaryUnloaded)
			{
				vessel.SetVesselState(Vessel.ToState.ToUnloaded);
			}
			vessel.mapIcon.localPosition = (vessel.stationaryData.posToPlane / 10000.0).toVector3;
		}
		vessel.mapIcon.rotation = vessel.partsManager.parts[0].transform.rotation;
		vessel.RCS = vesselToLoad.RCS;
		return vessel;
	}

	private static List<Part> LoadParts(VesselSave vesselToLoad)
	{
		int num = vesselToLoad.parts.Length;
		Part[] array = new Part[num];
		for (int i = 0; i < num; i++)
		{
			PartData partData = vesselToLoad.parts[i].partName.PartData();
			if (partData == null)
			{
				partData = Ref.controller.emptyPlaceholderPart;
			}
			array[i] = CreateRocket.CreatePart(partData.prefab, vesselToLoad.parts[i].orientation, null, Vector3.zero, vesselToLoad.parts[i].skinId, vesselToLoad.parts[i].interactionKeys);
		}
		for (int j = 0; j < vesselToLoad.joints.Length; j++)
		{
			Part.Joint.Save save = vesselToLoad.joints[j];
			if (save.fromPartId != -1 && save.toPartId != -1 && !(array[save.fromPartId] == null) && !(array[save.toPartId] == null))
			{
				new Part.Joint(save.anchor, array[save.fromPartId], array[save.toPartId], save.fromSurfaceIndex, save.toSurfaceIndex, save.fuelFlow);
			}
		}
		return new List<Part>(array);
	}

	private static void LoadPartsData(VesselSave vesselToLoad, Part[] loadedParts)
	{
		for (int i = 0; i < vesselToLoad.parts.Length; i++)
		{
			Part part = loadedParts[i];
			if (!(part == null))
			{
				Part.Save save = vesselToLoad.parts[i];
				Module[] components = part.GetComponents<Module>();
				Array.Reverse(components);
				Array.Reverse(save.moduleSaves);
				for (int j = 0; j < Mathf.Min(part.modules.Length, save.moduleSaves.Length); j++)
				{
					components[j].Load(save.moduleSaves[j]);
				}
			}
		}
		for (int k = 0; k < vesselToLoad.parts.Length; k++)
		{
			if (!(loadedParts[k] == null))
			{
				Module[] components2 = loadedParts[k].GetComponents<Module>();
				for (int l = 0; l < Mathf.Min(loadedParts[k].modules.Length, vesselToLoad.parts[k].moduleSaves.Length); l++)
				{
					components2[l].OnModuleLoaded();
				}
			}
		}
	}

	private static void ClearScene()
	{
		if (Ref.controller.vessels != null)
		{
			while (Ref.controller.vessels.Count > 0)
			{
				Ref.controller.vessels[0].DestroyVessel();
			}
		}
		Ref.controller.vessels.Clear();
	}
}
