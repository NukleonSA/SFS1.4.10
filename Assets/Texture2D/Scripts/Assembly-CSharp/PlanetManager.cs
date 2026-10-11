using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlanetManager : MonoBehaviour
{
	[Serializable]
	public class LowRezPlanet
	{
		public Planet planet;

		public Transform holder;

		public LowRezPlanet(Planet planet)
		{
			this.planet = planet;
			holder = ((!planet.data.hasTerrain) ? new GameObject().transform : new Chunck(0.0, 1.0, 0, planet.data.BASE_DATA.mapResolution, planet, null, false, false).transform);
			holder.localScale = Vector3.one / 10000f;
			holder.gameObject.layer = LayerMask.NameToLayer("Scaled Space");
			holder.name = planet.data.BASE_DATA.name + " (Scaled)";
			holder.parent = Ref.planetManager.scaledHolder;
			if (planet.data.hasAtmosphere)
			{
				CreateAtmosphere(planet, holder, "Scaled Space", "Back");
			}
		}
	}

	[Serializable]
	public class TerrainPoints
	{
		public Vector3[] points;

		public Vector2[] uvOthers;

		public TerrainPoints(int pointCount)
		{
			points = new Vector3[pointCount];
			uvOthers = new Vector2[pointCount];
		}
	}

	[Serializable]
	public class Chunck
	{
		public Transform transform;

		[TableColumnWidth(60, true)]
		public double from;

		[TableColumnWidth(50, true)]
		public int LODId;

		public double updateSplit;

		public double updateMerge;

		public double topSizeHalf;

		public Double3 topPosition;

		public Vector3 chunckPosOffset;

		public Chunck secondHalf;

		public Chunck parentChunck;

		public Chunck(double from, double size, int LODId, int LOD, Planet planet, Transform parent, bool forMap, bool offset)
		{
			this.from = from;
			this.LODId = LODId;
			topSizeHalf = size * Math.PI * (planet.data.BASE_DATA.radius + planet.maxTerrainHeight);
			double num = (from + size * 0.5) * Math.PI * 2.0;
			topPosition = new Double3(Math.Cos(num), Math.Sin(num)) * (planet.data.BASE_DATA.radius + planet.maxTerrainHeight);
			transform = UnityEngine.Object.Instantiate(Ref.planetManager.chunckPrefab).transform;
			transform.parent = parent;
			transform.localPosition = Vector3.zero;
			int num2 = (int)((double)LOD * size) + 1;
			double deltaAngle = size / (double)(num2 - 1) * Math.PI * 2.0;
			TerrainPoints terrainPoints = planet.data.TERRAIN_DATA.GetTerrainPoints(from, deltaAngle, num2, LOD, offset, planet);
			GenerateMesh(transform, terrainPoints, GenerateIndices(terrainPoints.points.Length), from - 0.5, size / (double)(num2 - 1), "Default", 10, planet, forMap);
			chunckPosOffset = terrainPoints.points[0];
		}

		private void GenerateMesh(Transform chunckTrans, TerrainPoints terrainPoints, int[] indices, double from, double delta, string sortingLayer, int orderInLayer, Planet planet, bool map)
		{
			Mesh mesh = chunckTrans.GetComponent<MeshFilter>().mesh;
			mesh.Clear();
			mesh.vertices = terrainPoints.points;
			mesh.triangles = indices;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			MeshRenderer component = chunckTrans.GetComponent<MeshRenderer>();
			component.sortingLayerName = sortingLayer;
			component.sortingOrder = orderInLayer;
			component.material = ((!map) ? planet.terrainMaterial : Ref.map.mapMaterial);
			Vector2[] array = new Vector2[terrainPoints.points.Length];
			for (int i = 0; i < terrainPoints.points.Length - 1; i++)
			{
				array[i + 1] = new Vector2((float)(from + delta * (double)i), 0f);
			}
			array[0] = Vector3.up;
			mesh.uv = array;
			Vector2[] array2 = new Vector2[terrainPoints.points.Length];
			for (int j = 0; j < terrainPoints.points.Length; j++)
			{
				array2[j] = (terrainPoints.points[j] - terrainPoints.points[0]).normalized / 2f;
			}
			mesh.uv2 = array2;
			mesh.uv3 = terrainPoints.uvOthers;
		}

		private int[] GenerateIndices(int lenght)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < lenght - 2; i++)
			{
				list.Add(0);
				list.Add(i + 2);
				list.Add(i + 1);
			}
			return list.ToArray();
		}

		public void GenerateCollider()
		{
			PolygonCollider2D polygonCollider2D = transform.gameObject.AddComponent<PolygonCollider2D>();
			Vector3[] vertices = transform.GetComponent<MeshFilter>().mesh.vertices;
			Vector2[] array = new Vector2[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				array[i] = vertices[i];
			}
			polygonCollider2D.points = array;
			transform.parent = null;
		}
	}

	public const float launchPadPositionX = 365f;

	public const float launchPadPositionY = 25.5f;

	[TableList]
	public List<Chunck> activeChuncks;

	[TableList]
	public List<Chunck> colliderChuncks;

	public MeshRenderer atmosphere;

	public MeshRenderer atmospherePrefab;

	public Transform chuncksHolder;

	public Transform launchPad;

	public Transform scaledHolder;

	public Transform chunckPrefab;

	public double totalDistanceMoved;

	public List<LowRezPlanet> lowRezPlanets;

	public const int baseChuncksCount = 50;

	public void SwitchLocation(Planet newPlanet, Double3 newFocusPosition, bool showSwitchMsg, double unloadDistance)
	{
		Debug.Log("LOADING NEW LOCATION");
		Planet loadedPlanet = Ref.controller.loadedPlanet;
		foreach (Vessel vessel in Ref.controller.vessels)
		{
			if (loadedPlanet != newPlanet || (newFocusPosition - vessel.GetGlobalPosition).sqrMagnitude2d > unloadDistance * unloadDistance)
			{
				vessel.SetVesselState(Vessel.ToState.ToUnloaded);
			}
		}
		Ref.controller.loadedPlanet = newPlanet;
		Ref.controller.OffsetScenePosition((newFocusPosition.roundTo1000 - Ref.positionOffset).toVector2);
		LoadTerrain(newPlanet);
		LoadAtmosphere(newPlanet);
		UpdateFog(Ref.controller.cameraDistanceGame);
		launchPad.gameObject.SetActive(newPlanet.data.BASE_DATA.name == Ref.controller.startAdress);
		if (showSwitchMsg)
		{
			ShowSwitchedPlanetMsg(newPlanet, loadedPlanet);
		}
	}

	private void LoadAtmosphere(Planet newPlanet)
	{
		if (atmosphere != null)
		{
			UnityEngine.Object.Destroy(atmosphere.gameObject);
		}
		if (newPlanet.data.hasAtmosphere)
		{
			atmosphere = CreateAtmosphere(newPlanet, base.transform, "Default", "Back").GetComponent<MeshRenderer>();
		}
	}

	private void LoadTerrain(Planet newPlanet)
	{
		while (chuncksHolder.childCount > 0)
		{
			UnityEngine.Object.DestroyImmediate(chuncksHolder.GetChild(0).gameObject);
		}
		foreach (Chunck colliderChunck in colliderChuncks)
		{
			if (colliderChunck.transform != null)
			{
				UnityEngine.Object.DestroyImmediate(colliderChunck.transform.gameObject);
			}
		}
		activeChuncks.Clear();
		colliderChuncks.Clear();
		if (newPlanet.data.hasTerrain)
		{
			for (int i = 0; i < 50; i++)
			{
				int lOD = (int)(newPlanet.data.BASE_DATA.radius * Math.PI * 2.0 / (double)newPlanet.data.TERRAIN_DATA.DETAIL_LEVELS[0].verticeSize);
				activeChuncks.Add(new Chunck((float)i / 50f, 0.02, 0, lOD, newPlanet, chuncksHolder, false, false)
				{
					updateMerge = double.PositiveInfinity
				});
			}
			UpdateChuncksLoading(newPlanet);
		}
	}

	public static Transform CreateAtmosphere(Planet planet, Transform parent, string layer, string sortingLayerName)
	{
		Transform transform = UnityEngine.Object.Instantiate(Ref.planetManager.atmospherePrefab.transform, parent);
		transform.gameObject.layer = LayerMask.NameToLayer(layer);
		transform.localPosition = Vector3.forward * planet.data.ATMOSPHERE_DATA.GRADIENT.positionZ;
		transform.transform.localScale = Vector3.one * (float)(planet.data.ATMOSPHERE_DATA.GRADIENT.gradientHeight + planet.data.BASE_DATA.radius);
		MeshRenderer component = transform.GetComponent<MeshRenderer>();
		component.sortingLayerName = sortingLayerName;
		component.material = planet.atmosphereMaterial;
		Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
		Vector2[] uv = mesh.uv;
		for (int i = 1; i < uv.Length; i++)
		{
			uv[i] = new Vector2(i, 1f);
		}
		uv[0] = Vector2.zero;
		mesh.uv = uv;
		return transform;
	}

	private static void ShowSwitchedPlanetMsg(Planet newPlanet, Planet oldPlanet)
	{
		if (oldPlanet.parentBody == newPlanet && oldPlanet != null)
		{
			MenuManager.ShowMsg("Left " + oldPlanet.data.BASE_DATA.name + "影响范围");
		}
		if (newPlanet.parentBody == oldPlanet)
		{
			MenuManager.ShowMsg("进入" + newPlanet.data.BASE_DATA.name + "影响范围");
		}
	}

	private void Start()
	{
		lowRezPlanets = new List<LowRezPlanet>();
		foreach (Planet value in AssetLoader.main.planets.Values)
		{
			lowRezPlanets.Add(new LowRezPlanet(value));
		}
	}

	private void Update()
	{
		UpdateChuncksLoading(Ref.controller.loadedPlanet);
		if (Ref.positionOffset.magnitude2d < 500000.0 && Ref.positionOffset.toVector3 != -base.transform.position)
		{
			UpdatePositionOffset(-Ref.positionOffset);
			UpdatePositionOffset(-Ref.positionOffset);
		}
		if (Ref.controller.loadedPlanet.data.hasAtmosphere)
		{
			Ref.controller.loadedPlanet.atmosphereMaterial.SetFloat("_PosX", (float)(Ref.controller.globalTime * 10.0 / Ref.controller.loadedPlanet.data.BASE_DATA.radius * (double)Ref.controller.loadedPlanet.data.ATMOSPHERE_DATA.CLOUDS.cloudVelocity % 1.0));
		}
	}

	public void UpdateChuncksLoading(Planet planet)
	{
		// 原版每帧固定 +0.1, 属于帧率相关: 高帧率下里程表增长过快, 会提前触发分块合并,
		// 导致地形退回最粗 LOD(地面消失/只剩大块扁平地形). 这里换算成与帧率无关的每秒 6.0(即 60fps 下的原行为)
		totalDistanceMoved += GetMaxVesselsVelocity() * (double)Time.deltaTime + 6.0 * (double)Time.deltaTime;
		for (int i = 0; i < activeChuncks.Count; i++)
		{
			Chunck chunck = activeChuncks[i];
			if (totalDistanceMoved > chunck.updateSplit && TrySplitChunck(chunck, planet))
			{
				i--;
			}
			if (totalDistanceMoved > chunck.updateMerge)
			{
				TryMergeChunck(chunck, planet);
			}
		}
	}

	private bool TrySplitChunck(Chunck chunck, Planet planet)
	{
		double loadDistance = planet.data.TERRAIN_DATA.DETAIL_LEVELS[chunck.LODId + 1].loadDistance;
		double minDistanceToVessels = GetMinDistanceToVessels(chunck);
		if (minDistanceToVessels < loadDistance + 10.0)
		{
			SplitChunck(chunck);
			return true;
		}
		if (minDistanceToVessels != double.PositiveInfinity)
		{
			chunck.updateSplit = totalDistanceMoved + Math.Min(minDistanceToVessels - loadDistance, 100000.0);
		}
		return false;
	}

	private void TryMergeChunck(Chunck chunck, Planet planet)
	{
		double loadDistance = planet.data.TERRAIN_DATA.DETAIL_LEVELS[chunck.LODId].loadDistance;
		double minDistanceToVessels = GetMinDistanceToVessels(chunck.parentChunck);
		if (minDistanceToVessels > loadDistance + 10.0)
		{
			MergeChunck(chunck);
		}
		if (minDistanceToVessels != double.PositiveInfinity)
		{
			chunck.updateMerge = totalDistanceMoved + Math.Min(loadDistance - minDistanceToVessels, 100000.0) + 30.0;
		}
	}

	private void SplitChunck(Chunck chunckToSplit)
	{
		chunckToSplit.transform.gameObject.SetActive(false);
		activeChuncks.Remove(chunckToSplit);
		bool flag = chunckToSplit.LODId + 1 == Ref.controller.loadedPlanet.data.TERRAIN_DATA.DETAIL_LEVELS.Length - 1;
		int num = chunckToSplit.LODId + 1;
		int lOD = (int)(Ref.controller.loadedPlanet.data.BASE_DATA.radius * Math.PI * 2.0 / (double)Ref.controller.loadedPlanet.data.TERRAIN_DATA.DETAIL_LEVELS[num].verticeSize);
		double num2 = 1.0 / (Math.Pow(2.0, num) * 50.0);
		Chunck chunck = new Chunck(chunckToSplit.from, num2, num, lOD, Ref.controller.loadedPlanet, chuncksHolder, false, flag);
		Chunck chunck2 = new Chunck(chunckToSplit.from + num2, num2, num, lOD, Ref.controller.loadedPlanet, chuncksHolder, false, flag);
		activeChuncks.Add(chunck);
		activeChuncks.Add(chunck2);
		chunck.transform.localPosition = ((!flag) ? Vector3.zero : (-chunck.chunckPosOffset));
		chunck2.transform.localPosition = ((!flag) ? Vector3.zero : (-chunck2.chunckPosOffset));
		chunck.secondHalf = chunck2;
		chunck.parentChunck = chunckToSplit;
		chunck2.updateMerge = double.PositiveInfinity;
		chunck.transform.name = "Chunck - Lod: " + (chunckToSplit.LODId + 1);
		chunck2.transform.name = "Chunck - Lod: " + (chunckToSplit.LODId + 1);
		if (flag)
		{
			chunck.GenerateCollider();
			chunck2.GenerateCollider();
			colliderChuncks.Add(chunck);
			colliderChuncks.Add(chunck2);
			chunck.updateSplit = double.PositiveInfinity;
			chunck2.updateSplit = double.PositiveInfinity;
		}
		else
		{
			TrySplitChunck(chunck, Ref.controller.loadedPlanet);
			TrySplitChunck(chunck2, Ref.controller.loadedPlanet);
		}
	}

	private void MergeChunck(Chunck firstHalf)
	{
		Chunck secondHalf = firstHalf.secondHalf;
		if (secondHalf.transform.gameObject.activeSelf)
		{
			firstHalf.parentChunck.transform.gameObject.SetActive(true);
			activeChuncks.Add(firstHalf.parentChunck);
			activeChuncks.Remove(firstHalf);
			activeChuncks.Remove(secondHalf);
			colliderChuncks.Remove(firstHalf);
			colliderChuncks.Remove(secondHalf);
			UnityEngine.Object.Destroy(firstHalf.transform.gameObject);
			UnityEngine.Object.Destroy(firstHalf.secondHalf.transform.gameObject);
		}
	}

	private double GetMaxVesselsVelocity()
	{
		double num = 0.0;
		for (int i = 0; i < Ref.controller.vessels.Count; i++)
		{
			if (Ref.controller.vessels[i].state == Vessel.State.RealTime)
			{
				double sqrMagnitude2d = Ref.controller.vessels[i].GetGlobalVelocity.sqrMagnitude2d;
				if (sqrMagnitude2d > num)
				{
					num = sqrMagnitude2d;
				}
			}
		}
		return Math.Sqrt(num);
	}

	private double GetMinDistanceToVessels(Chunck chunck)
	{
		double num = double.PositiveInfinity;
		for (int i = 0; i < Ref.controller.vessels.Count; i++)
		{
			if (Ref.controller.vessels[i].state == Vessel.State.RealTime || Ref.controller.vessels[i].state == Vessel.State.Stationary)
			{
				Double3 @double = ((Ref.controller.vessels[i].state != Vessel.State.RealTime) ? Ref.controller.vessels[i].GetGlobalPosition : (Ref.positionOffset + Ref.controller.vessels[i].partsManager.rb2d.position));
				double closestPointOnLine = Double3.GetClosestPointOnLine(chunck.topPosition, @double);
				Double3 double2 = chunck.topPosition * closestPointOnLine;
				double num2 = (double2 - @double).magnitude2d - chunck.topSizeHalf * closestPointOnLine;
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	public void UpdatePositionOffset(Double3 newPlanetOffset)
	{
		if (newPlanetOffset.x != Ref.positionOffset.x || newPlanetOffset.y != Ref.positionOffset.y)
		{
			Ref.positionOffset = newPlanetOffset;
			Double3 @double = new Double3(365.0, Ref.controller.loadedPlanet.data.BASE_DATA.radius + 25.5);
			launchPad.transform.position = (@double - Ref.positionOffset).toVector3;
			base.transform.position = -Ref.positionOffset.toVector3;
			for (int i = 0; i < colliderChuncks.Count; i++)
			{
				colliderChuncks[i].transform.position = -Ref.positionOffset.toVector3 - colliderChuncks[i].chunckPosOffset;
			}
		}
	}

	public void UpdateFog(float newDistance)
	{
		foreach (Planet value in AssetLoader.main.planets.Values)
		{
			if (value.data.hasAtmosphere && value.data.hasTerrain)
			{
				value.terrainMaterial.SetColor("_Fog", value.data.ATMOSPHERE_DATA.FOG.Evaluate(newDistance));
			}
		}
	}

	public void PositionLowRezPlanets(double time)
	{
		Double3 @double = Ref.controller.loadedPlanet.GetSolarSystemPosition(time) + Ref.positionOffset;
		foreach (LowRezPlanet lowRezPlanet in lowRezPlanets)
		{
			lowRezPlanet.holder.localPosition = ((lowRezPlanet.planet.GetSolarSystemPosition(time) - @double) * 0.0001).toVector3;
		}
	}
}
