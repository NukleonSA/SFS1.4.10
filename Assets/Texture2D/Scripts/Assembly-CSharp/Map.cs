using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Map : MonoBehaviour
{
	[Serializable]
	public class SelectManager
	{
		public float selectIconSize;

		[Space]
		public Transform selectIcon;

		public Transform targetIcon;

		[Space]
		public Transform selectMarkerVessel;

		public Transform targetMarkerVessel;

		[Space]
		public Transform targetButton;

		public Transform focusButton;

		public Transform switchButton;

		public Transform followButton;

		public Transform recoverButton;

		public Transform destroyButton;

		[Space]
		public BoxCollider2D[] buttons;

		[Space]
		public TextMesh targetButtonText;

		[Space]
		public TextMesh followButtonText;

		public Vector2 menuSize;

		public float hitboxVerticalSize;
	}

	[Serializable]
	public class ElementManager
	{
		[Serializable]
		public class Element
		{
			public int priority;

			public Vector2 size;

			public Vector2 position;

			public TextMesh textMesh;
		}

		[Serializable]
		public class TextElement
		{
			public TextMesh textMesh;

			public Renderer renderer;
		}

		public List<Element> elements;

		public List<TextElement> textElements = new List<TextElement>();

		public int listIndex;

		public Transform prefab;

		public void DrawTextElement(string text, int fontSize, float alpha, int priority, Vector3 position)
		{
			if (!(alpha <= 0f))
			{
				if (listIndex > textElements.Count - 1)
				{
					CreateNewElement();
				}
				TextMesh textMesh = textElements[listIndex].textMesh;
				textMesh.text = text;
				textMesh.fontSize = fontSize;
				textMesh.color = new Color(1f, 1f, 1f, alpha);
				textMesh.transform.position = position;
				if (!textMesh.gameObject.activeSelf)
				{
					textMesh.gameObject.SetActive(true);
				}
				textMesh.transform.rotation = Quaternion.identity;
				RegisterElement(priority, textElements[listIndex].renderer.bounds.extents * 0.85f, position, textMesh);
				textMesh.transform.localEulerAngles = Ref.main.Camera.transform.localEulerAngles;
				listIndex++;
			}
		}

		public void RegisterElement(int priority, Vector2 size, Vector2 position, TextMesh textMesh)
		{
			Element element = new Element();
			element.priority = priority;
			element.size = size;
			element.position = position;
			element.textMesh = textMesh;
			Element element2 = element;
			float num = (float)(Ref.map.mapPosition.z / 10000.0) * 0.01f;
			foreach (Element element3 in elements)
			{
				Vector2 a = element2.position - element3.position;
				a = Utility.RotateZ(a, (0f - Ref.main.Camera.transform.eulerAngles.z) * ((float)Math.PI / 180f));
				float num2 = Mathf.Abs(a.x) - element2.size.x - element3.size.x;
				if (num2 > num)
				{
					continue;
				}
				float num3 = Mathf.Abs(a.y) - element2.size.y - element3.size.y;
				if (num3 > num)
				{
					continue;
				}
				float num4 = Mathf.Max(num2, num3);
				float num5 = num4 / num;
				if (element2.priority > element3.priority)
				{
					if (element3.textMesh != null)
					{
						if (num5 > 0f)
						{
							element3.textMesh.color = new Color(1f, 1f, 1f, num5);
						}
						else if (element3.textMesh.gameObject.activeSelf)
						{
							element3.textMesh.gameObject.SetActive(false);
						}
					}
				}
				else if (element2.textMesh != null)
				{
					if (num5 > 0f)
					{
						element2.textMesh.color = new Color(1f, 1f, 1f, num5);
					}
					else if (element2.textMesh.gameObject.activeSelf)
					{
						element2.textMesh.gameObject.SetActive(false);
					}
				}
			}
			elements.Add(element2);
		}

		public void Reset()
		{
			elements.Clear();
			listIndex = 0;
			foreach (TextElement textElement in textElements)
			{
				if (textElement.textMesh.gameObject.activeSelf)
				{
					textElement.textMesh.gameObject.SetActive(false);
				}
			}
		}

		public void UpdateTextSize(float newSize)
		{
			foreach (TextElement textElement in textElements)
			{
				textElement.textMesh.transform.localScale = Vector3.one * newSize;
			}
		}

		private void CreateNewElement()
		{
			TextMesh component = UnityEngine.Object.Instantiate(prefab).GetComponent<TextMesh>();
			textElements.Add(new TextElement
			{
				textMesh = component,
				renderer = component.GetComponent<Renderer>()
			});
			Ref.map.syncRotation.Add(component.transform);
			component.transform.localScale = Vector3.one * (float)(Ref.map.mapPosition.z * 0.004000000189989805 / 10000.0);
		}
	}

	public const double mapScale = 10000.0;

	public const double maxZoom = 200000000000.0;

	public const double minZoom = 500.0;

	public const int maxPatchConics = 3;

	public Material mapMaterial;

	[BoxGroup("a", false, false, 0)]
	public Double3 mapPosition;

	[BoxGroup("a", false, false, 0)]
	public Planet followingPlanet;

	public object selected;

	public object target;

	[BoxGroup("a", false, false, 0)]
	public Vessel followVessel;

	[Space]
	public static float orbitLineWidth;

	[BoxGroup("b", false, false, 0)]
	public OrbitLines.OrbitLinesPack mainLines;

	[BoxGroup("c", false, false, 0)]
	public OrbitLines.OrbitLinesPack selectedLines;

	[BoxGroup("d", false, false, 0)]
	public Material dottedLineMaterial;

	[BoxGroup("Transfer Windows", true, false, 0)]
	public MeshFilter transferWindowPlanet;

	[BoxGroup("Transfer Windows", true, false, 0)]
	public MeshFilter transferWindowOrbit;

	[BoxGroup("Transfer Windows", true, false, 0)]
	public TransferWindow.Data transferWindowData;

	public Shader spriteDefault;

	[BoxGroup("d", false, false, 0)]
	public List<Material> orbitLineMaterials;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform mapVesselIconPrefab;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform planetHolderPrefab;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform SOIPrefab;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform planetOrbitLinePrefab;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform vesselOrbitLinePrefab;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform textPrefab;

	[BoxGroup("Prefab References", true, false, 0)]
	public Transform dottedLinePrefab;

	public Transform sunGlow;

	[HideInInspector]
	public Vector3[] ellipsePoints;

	[DrawWithUnity]
	public Sprite spriteSOI;

	public List<Transform> syncRotation;

	[Space]
	public SelectManager selectManager;

	[Space]
	public ElementManager elementManager;

	public void InitializeMap()
	{
		GetEllipsePoints();
		for (int i = 0; i < AssetLoader.main.maxOrbitalDepth + 1; i++)
		{
			orbitLineMaterials.Add(new Material(spriteDefault));
		}
		mainLines = new OrbitLines.OrbitLinesPack(vesselOrbitLinePrefab, textPrefab);
		selectedLines = new OrbitLines.OrbitLinesPack(vesselOrbitLinePrefab, textPrefab);
		CreateMapPlanets();
		followingPlanet = Ref.controller.startAdress.Planet();
		UpdateMapZoom(mapPosition.z);
		TransferWindow.GenerateMeshPlanetTW(ref transferWindowPlanet, 0.05f, 0.006f, 1f, null);
		TransferWindow.GenerateMeshPlanetTW(ref transferWindowOrbit, 0.05f, 0f, 1f, null);
		CreateNewTransferWindow();
		while (OrbitLines.meshesPool.Count < 10)
		{
			OrbitLines.meshesPool.Add(new Mesh());
		}
	}

	public void ToggleMap()
	{
		Ref.mapView = !Ref.mapView;
		if (Ref.mapView)
		{
			Ref.main.Camera.cullingMask = Ref.controller.mapView;
		}
		base.gameObject.SetActive(Ref.mapView);
		if (Ref.mapView)
		{
			foreach (Vessel vessel in Ref.controller.vessels)
			{
				vessel.mapIcon.transform.parent = vessel.GetVesselPlanet.mapHolder;
			}
		}
		else
		{
			Ref.main.Camera.transform.position = new Vector3(Ref.controller.mainVesselLocalPosition.x, Ref.controller.mainVesselLocalPosition.y, 0f - Ref.controller.cameraDistanceGame);
		}
		Ref.inputController.instructionsMap.SetActive(false);
	}

	public void Select(object newSelect)
	{
		selected = newSelect;
		UpdateSelectButtons();
	}

	public void Target()
	{
		if (selected != null && (!(selected is Vessel) || !(selected as Vessel == Ref.mainVessel)))
		{
			if (target == selected)
			{
				SetTarget(null);
				Select(null);
			}
			else
			{
				SetTarget(selected);
				Select(null);
			}
		}
	}

	public void SetTarget(object newTarget)
	{
		target = newTarget;
		bool flag = target != null && target is Planet;
		bool flag2 = target != null && target is Vessel;
		if (selectManager.targetMarkerVessel.gameObject.activeSelf != flag2)
		{
			selectManager.targetMarkerVessel.gameObject.SetActive(flag2);
		}
		if (flag)
		{
			CreateNewTransferWindow();
		}
	}

	public void Focus()
	{
		if (selected is Planet)
		{
			followingPlanet = selected as Planet;
			followVessel = null;
			UpdateMapPosition(new Double3(0.0, 0.0, mapPosition.z));
			if ((selected as Planet).SOI != double.PositiveInfinity)
			{
				UpdateMapZoom(followingPlanet.SOI * 2.5);
				selected = null;
			}
		}
	}

	private void UpdateSelectButtons()
	{
		if (selected != null)
		{
			bool flag = !(selected is Vessel) || !(selected as Vessel == Ref.mainVessel);
			bool flag2 = (selected is Vessel) & (selected as Vessel != Ref.mainVessel);
			bool flag3 = selected is Vessel && (selected as Vessel).GetGlobalVelocity.magnitude2d > 10.0;
			bool flag4 = selected is Vessel && (selected as Vessel).OnSurface && (selected as Vessel).GetVesselPlanet.data.BASE_DATA.name == Ref.controller.startAdress;
			bool flag5 = selected is Planet;
			Vector2 vector = new Vector2(0f, -0.75f);
			selectManager.targetButton.gameObject.SetActive(flag);
			selectManager.targetButton.localPosition = vector;
			vector.y += ((!flag) ? 0f : (-0.7f));
			selectManager.focusButton.gameObject.SetActive(flag5);
			selectManager.focusButton.localPosition = vector;
			vector.y += ((!flag5) ? 0f : (-0.7f));
			selectManager.switchButton.gameObject.SetActive(flag2);
			selectManager.switchButton.localPosition = vector;
			vector.y += ((!flag2) ? 0f : (-0.7f));
			selectManager.followButton.gameObject.SetActive(flag3);
			selectManager.followButton.localPosition = vector;
			vector.y += ((!flag3) ? 0f : (-0.7f));
			selectManager.recoverButton.gameObject.SetActive(flag4);
			selectManager.recoverButton.localPosition = vector;
			vector.y += ((!flag4) ? 0f : (-0.7f));
			selectManager.destroyButton.gameObject.SetActive(flag2);
			selectManager.destroyButton.localPosition = vector;
			vector.y += ((!flag2) ? 0f : (-0.7f));
			vector.y += 0.35f;
			selectManager.menuSize = new Vector2(selectManager.menuSize.x, (0f - vector.y) * selectManager.hitboxVerticalSize);
		}
	}

	public void ToggleFollow()
	{
		Vessel vessel = ((!(selected is Vessel)) ? null : (selected as Vessel));
		if (!(vessel == null))
		{
			if (vessel == followVessel)
			{
				StopFollowingVessel();
				return;
			}
			mapPosition = new Double3(0.0, 0.0, mapPosition.z);
			followVessel = vessel;
			vessel = null;
		}
	}

	private void StopFollowingVessel()
	{
		if (!(followVessel == null))
		{
			Double3 newMapPosition = followVessel.GetGlobalPosition + mapPosition;
			followingPlanet = followVessel.GetVesselPlanet;
			followVessel = null;
			selected = ((!(selected is Vessel)) ? selected : null);
			UpdateMapPosition(newMapPosition);
		}
	}

	public Vessel GetOtherVessel()
	{
		return ((!(target is Vessel)) ? null : (target as Vessel)) ?? ((!(selected is Vessel)) ? null : (selected as Vessel)) ?? followVessel;
	}

	private void CreateNewTransferWindow()
	{
		transferWindowData = TransferWindow.Data.GetTransferType((!(Ref.mainVessel != null)) ? null : Ref.mainVessel.GetVesselPlanet, (!(target is Planet)) ? null : (target as Planet));
		TransferWindow.SetParents(transferWindowData, transferWindowPlanet.transform, transferWindowOrbit.transform, transferWindowPlanet.GetComponent<MeshRenderer>().sharedMaterial);
	}

	private void PositionTransferWindowMarkers(List<Orbit> orbits)
	{
		bool show = false;
		TransferWindow.PositionPlanetMarker(ref show, ref transferWindowPlanet, transferWindowData);
		if (transferWindowPlanet.gameObject.activeSelf != show)
		{
			transferWindowPlanet.gameObject.SetActive(show);
		}
		bool show2 = false;
		TransferWindow.PositionOrbitMarker(ref show2, ref transferWindowOrbit, orbits, transferWindowData);
		if (transferWindowOrbit.gameObject.activeSelf != show2)
		{
			transferWindowOrbit.gameObject.SetActive(show2);
		}
	}

	public void UpdateVesselsMapIcons()
	{
		// 【地图火箭标记固定屏占比】地图的世界高度与 mapPosition.z 成正比(见本文件 10000.0 的约定),
		// 因此需要"固定屏幕大小"的元素都要乘 mapPosition.z / 10000:
		//   selectIcon / targetIcon(518/534 行)、名称文本(189 行)、轨道线宽(688 行) 都是这么做的。
		// 唯独火箭标记原来是固定值 0.01/0.015 -> 会随地图一起放大缩小, 近距离时大得异常。
		// 这里补上同样的缩放补偿 => 三角形在任何缩放下都保持同样大小。
		float mapIconZoom = (float)(mapPosition.z / 10000.0);
		foreach (Vessel vessel in Ref.controller.vessels)
		{
			vessel.mapIcon.localScale = ((!(vessel == Ref.mainVessel)) ? (Vector2.one * (0.01f * mapIconZoom)) : (Vector2.one * (0.015f * mapIconZoom)));
		}
	}

	private void LateUpdate()
	{
		if (!Ref.mapView)
		{
			return;
		}
		elementManager.Reset();
		Ref.main.Camera.transform.position = Vector3.zero;
		PositionPlanets();
		List<Orbit> vesselOrbits = GetVesselOrbits(Ref.mainVessel);
		List<Orbit> list = ((!(GetOtherVessel() != Ref.mainVessel)) ? new List<Orbit>() : GetVesselOrbits(GetOtherVessel()));
		OrbitLines.DrawOrbitLines(mainLines, GetVesselOrbits(Ref.mainVessel));
		OrbitLines.DrawOrbitLines(selectedLines, list);
		DrawDropdownMenu();
		PositionTransferWindowMarkers((vesselOrbits.Count <= 0) ? list : vesselOrbits);
		if (IsNotNull(target))
		{
			if (target is Planet)
			{
				ClosestApproach.DrawClosestApproachPlanet(vesselOrbits, target as Planet);
			}
			else if (target is Vessel)
			{
				ClosestApproach.DrawClosestApproachVessel(vesselOrbits, (list.Count <= 0) ? null : list[0]);
			}
		}
	}

	private void DrawDropdownMenu()
	{
		bool flag = IsNotNull(selected);
		bool flag2 = IsNotNull(target) && target != selected;
		if (selectManager.selectIcon.gameObject.activeSelf != flag)
		{
			selectManager.selectIcon.gameObject.SetActive(flag);
		}
		if (selectManager.targetIcon.gameObject.activeSelf != flag2)
		{
			selectManager.targetIcon.gameObject.SetActive(flag2);
		}
		if (flag)
		{
			if (selectManager.selectMarkerVessel.gameObject.activeSelf != selected is Vessel)
			{
				selectManager.selectMarkerVessel.gameObject.SetActive(selected is Vessel);
			}
			selectManager.selectIcon.position = ((!(selected is Vessel)) ? (selected as Planet).mapHolder.transform.position : (selected as Vessel).mapIcon.transform.position);
			selectManager.selectIcon.localScale = Vector2.one * (selectManager.selectIconSize * (float)(mapPosition.z / 10000.0));
			selectManager.targetButtonText.text = ((selected == target) ? "取消目标" : "设为目标");
			selectManager.followButtonText.text = ((!(selected is Vessel) || !(selected as Vessel == followVessel)) ? "跟随" : "取消跟随");
			selectManager.targetButtonText.transform.GetChild(0).GetComponent<TextMesh>().text = selectManager.targetButtonText.text;
			selectManager.followButtonText.transform.GetChild(0).GetComponent<TextMesh>().text = selectManager.followButtonText.text;
			Vector2 size = selectManager.menuSize * (float)(Ref.map.mapPosition.z / 10000.0);
			Vector2 position = (Vector2)selectManager.selectIcon.position - Utility.RotateZ(new Vector2(0f, size.y), Ref.main.Camera.transform.eulerAngles.z * ((float)Math.PI / 180f));
			elementManager.RegisterElement(100, size, position, null);
			if (selected is Planet)
			{
				elementManager.DrawTextElement((selected as Planet).data.BASE_DATA.name, 48, 1f, 101, selectManager.selectIcon.position);
			}
		}
		if (flag2)
		{
			selectManager.targetIcon.position = ((!(target is Vessel)) ? (target as Planet).mapHolder.transform.position : (target as Vessel).mapIcon.transform.position);
			selectManager.targetIcon.localScale = Vector2.one * (selectManager.selectIconSize * (float)(mapPosition.z / 10000.0));
			if (target is Planet)
			{
				elementManager.DrawTextElement("►" + (target as Planet).data.BASE_DATA.name + "◄", 42, 1f, 50, selectManager.targetIcon.position);
			}
		}
	}

	private bool IsNotNull(object a)
	{
		return a != null && ((!(a is Planet)) ? (a as Vessel != null) : (a as Planet != null));
	}

	public void DrawOrbitLines()
	{
		List<Orbit> vesselOrbits = GetVesselOrbits(Ref.mainVessel);
		List<Orbit> orbits = ((!(GetOtherVessel() != Ref.mainVessel)) ? new List<Orbit>() : GetVesselOrbits(GetOtherVessel()));
		OrbitLines.DrawOrbitLines(mainLines, GetVesselOrbits(Ref.mainVessel));
		OrbitLines.DrawOrbitLines(selectedLines, orbits);
	}

	private void PositionPlanets()
	{
		Double3 @double = ((!(followVessel == null)) ? (mapPosition + followVessel.GetGlobalPosition + followVessel.GetVesselPlanet.GetSolarSystemPosition(Ref.controller.globalTime)) : (mapPosition + followingPlanet.GetSolarSystemPosition(Ref.controller.globalTime)));
		foreach (Planet value in AssetLoader.main.planets.Values)
		{
			Double3 double2 = value.GetSolarSystemPosition(Ref.controller.globalTime) - @double;
			double2.z = 0.0 - double2.z;
			value.mapHolder.transform.localPosition = (double2 / 10000.0).toVector3;
			if (value.data.hasOrbitData)
			{
				value.orbitLine.transform.eulerAngles = new Vector3(0f, 0f, (float)(value.meanMotion * Ref.controller.globalTime) * 57.29578f);
			}
			int priority = (value.orbitalDepth + 1) * -100 + value.satelliteIndex;
			double num = mapPosition.z / (value.data.BASE_DATA.radius * value.data.BASE_DATA.hideNameMultiplier) - 3.0;
			elementManager.DrawTextElement(value.data.BASE_DATA.name, 42, (float)num, priority, value.mapHolder.position);
		}
	}

	private List<Orbit> GetVesselOrbits(Vessel vessel)
	{
		if (vessel == null)
		{
			return new List<Orbit>();
		}
		if (vessel.state == Vessel.State.RealTime)
		{
			Double3 velIn = Ref.velocityOffset + vessel.partsManager.rb2d.velocity;
			if (velIn.sqrMagnitude2d > 4.0)
			{
				return Orbit.CalculateOrbits(vessel.GetGlobalPosition, velIn, Ref.controller.loadedPlanet);
			}
		}
		return vessel.orbits;
	}

	public void OnClickEmpty(Vector2 clickPositionWorld)
	{
		GameObject gameObject = Ref.inputController.PointCastButtons(clickPositionWorld, selectManager.buttons);
		if (gameObject != null)
		{
			gameObject.GetComponent<CustomEvent>().InvokeEvenets();
			return;
		}
		Planet planet = PointCastMapPlanets(clickPositionWorld);
		if (planet != null)
		{
			Select(planet);
			Ref.inputController.PlayClickSound(0.6f);
			return;
		}
		Vessel vessel = PointCastVessels(clickPositionWorld);
		if (vessel != null)
		{
			Select(vessel);
			Ref.inputController.PlayClickSound(0.6f);
		}
		else
		{
			Select(null);
		}
	}

	private Planet PointCastMapPlanets(Vector2 clickPos)
	{
		float num = Mathf.Pow((float)Ref.map.mapPosition.z * 0.04f / 10000f, 2f);
		Planet result = null;
		foreach (Planet value in AssetLoader.main.planets.Values)
		{
			float sqrMagnitude = (clickPos - (Vector2)value.mapHolder.transform.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = value;
			}
		}
		return result;
	}

	private Vessel PointCastVessels(Vector2 clickPos)
	{
		float num = Mathf.Pow((float)(Ref.map.mapPosition.z * 0.04 / 10000.0), 2f);
		Vessel result = null;
		for (int i = 0; i < Ref.controller.vessels.Count; i++)
		{
			float sqrMagnitude = ((Vector2)Ref.controller.vessels[i].mapIcon.position - clickPos).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = Ref.controller.vessels[i];
			}
		}
		return result;
	}

	public void UpdateMapPosition(Double3 newMapPosition)
	{
		mapPosition = newMapPosition;
		TryFollowSattelites();
		TryFollowParent();
		TryStopFollow();
		UpdateOrbitLinesVisibility();
	}

	public void UpdateMapZoom(double newZoom)
	{
		if (newZoom > 200000000000.0)
		{
			newZoom = 200000000000.0;
		}
		if (newZoom < 500.0)
		{
			newZoom = 500.0;
		}
		mapPosition.z = newZoom;
		// 缩放变化后必须刷新火箭标记尺寸, 否则它不会随缩放重新计算
		// (Start 阶段就会调用本方法, 此时 controller 可能尚未就绪, 故加保护)
		if (Ref.controller != null && Ref.controller.vessels != null)
		{
			UpdateVesselsMapIcons();
		}
		if (mapPosition.z > followingPlanet.SOI * 6.0)
		{
			SwitchFollowingBody(followingPlanet.parentBody);
		}
		TryFollowSattelites();
		UpdateOrbitLinesVisibility();
		if (transferWindowData.transferType == TransferWindow.TransferType.ToNeighbour)
		{
			TransferWindow.UpdateTransferWindowAlpha(transferWindowPlanet.GetComponent<MeshRenderer>().sharedMaterial, transferWindowData);
		}
	}

	private void UpdateOrbitLinesVisibility()
	{
		for (int i = 0; i < orbitLineMaterials.Count; i++)
		{
			float a = ((i + 1 != followingPlanet.orbitalDepth) ? ((float)((i + 1 > followingPlanet.orbitalDepth) ? 1 : 0)) : GetOrbitLineAlpha(followingPlanet.SOI));
			orbitLineMaterials[i].color = new Color(1f, 1f, 1f, a);
		}
		orbitLineWidth = (float)(Ref.map.mapPosition.z * 0.004000000189989805 / 10000.0);
		Vector3 vector = Vector3.one * orbitLineWidth;
		foreach (Planet value in AssetLoader.main.planets.Values)
		{
			if (value.data.hasOrbitData)
			{
				value.orbitLine.widthMultiplier = orbitLineWidth;
			}
		}
		mainLines.UpdateLinesWidth(orbitLineWidth);
		selectedLines.UpdateLinesWidth(orbitLineWidth);
		elementManager.UpdateTextSize(orbitLineWidth);
	}

	private float GetOrbitLineAlpha(double followingSOI)
	{
		float val = Mathf.Clamp01((float)(mapPosition.magnitude2d / (followingSOI * 2.0)) * 3f - 2f);
		float val2 = Mathf.Clamp01((float)(mapPosition.z / (followingSOI * 3.5) - 1.0) * 1.5f);
		return Mathf.Pow(Math.Max(val, val2), 2f);
	}

	private void TryFollowParent()
	{
		if (!(followVessel != null) && mapPosition.magnitude2d > followingPlanet.SOI * 2.0)
		{
			SwitchFollowingBody(followingPlanet.parentBody);
		}
	}

	private void TryFollowSattelites()
	{
		if (followVessel != null)
		{
			return;
		}
		Planet[] satellites = followingPlanet.satellites;
		foreach (Planet planet in satellites)
		{
			if (!(planet.SOI * 6.0 < mapPosition.z) && !(Math.Pow(planet.SOI * 2.0, 2.0) < (planet.GetPosOut(Ref.controller.globalTime) - mapPosition).sqrMagnitude2d))
			{
				SwitchFollowingBody(planet);
			}
		}
	}

	private void SwitchFollowingBody(Planet newFollow)
	{
		StopFollowingVessel();
		if (newFollow.parentBody == followingPlanet)
		{
			mapPosition -= newFollow.GetPosOut(Ref.controller.globalTime);
		}
		if (followingPlanet.parentBody == newFollow)
		{
			mapPosition += followingPlanet.GetPosOut(Ref.controller.globalTime);
		}
		followingPlanet = newFollow;
	}

	private void TryStopFollow()
	{
		if (!(followVessel == null) && mapPosition.magnitude2d > mapPosition.z * 1.25)
		{
			StopFollowingVessel();
		}
	}

	private void GetEllipsePoints()
	{
		ellipsePoints = new Vector3[vesselOrbitLinePrefab.GetChild(0).GetComponent<LineRenderer>().positionCount];
		for (int i = 0; i < ellipsePoints.Length; i++)
		{
			ellipsePoints[i] = vesselOrbitLinePrefab.GetChild(0).GetComponent<LineRenderer>().GetPosition(i);
			ellipsePoints[i].y *= 0.5f;
			ellipsePoints[i] = ellipsePoints[i].normalized;
		}
	}

	private void CreateMapPlanets()
	{
		foreach (Planet value in AssetLoader.main.planets.Values)
		{
			CreateMapCelestialBody(value);
		}
		foreach (Planet value2 in AssetLoader.main.planets.Values)
		{
			if (value2.data.hasOrbitData)
			{
				CreateCircle(value2.mapHolder, (float)(value2.SOI / 10000.0), GetMapElementColor(Color.white, 0.05f), 0, "SOI");
				value2.orbitLine = CreatePlanetOrbitLine(value2.parentBody.mapHolder, value2);
			}
		}
	}

	private void CreateMapCelestialBody(Planet planet)
	{
		planet.mapHolder = UnityEngine.Object.Instantiate(planetHolderPrefab, base.transform);
		planet.mapHolder.gameObject.layer = base.gameObject.layer;
		planet.mapHolder.name = planet.data.BASE_DATA.name + " holder ( Map)";
		if (planet.data.hasTerrain)
		{
			CreateMapTerrain(planet, planet.mapHolder);
		}
		else
		{
			CreateCircle(planet.mapHolder, (float)(planet.data.BASE_DATA.radius / 10000.0), planet.data.BASE_DATA.mapColor.GetColor(), 10, planet.data.BASE_DATA.name);
		}
		if (planet.data.hasAtmosphere && planet.data.ATMOSPHERE_DATA.PHYSICS.density > 0.0)
		{
			CreateCircle(planet.mapHolder, (float)((planet.data.BASE_DATA.radius + planet.data.ATMOSPHERE_DATA.PHYSICS.height) / 10000.0), GetMapElementColor(Color.white, 0.1f), 1, "Atmosphere");
		}
	}

	private TextMesh CreateNameIcon(string name, Transform holder)
	{
		Transform transform = UnityEngine.Object.Instantiate(textPrefab, holder);
		transform.name = name + " name icon";
		transform.GetComponent<TextMesh>().text = name;
		transform.GetComponent<MeshRenderer>().sortingLayerName = "Map";
		transform.GetComponent<MeshRenderer>().sortingOrder = 101;
		return transform.GetComponent<TextMesh>();
	}

	private Transform CreateMapTerrain(Planet planet, Transform parent)
	{
		Transform transform = new PlanetManager.Chunck(0.0, 1.0, 0, planet.data.BASE_DATA.mapResolution, planet, base.transform, true, false).transform;
		transform.gameObject.layer = base.gameObject.layer;
		transform.localScale = Vector3.one * 1.0001f / 10000f;
		transform.parent = parent;
		transform.localPosition = Vector3.zero;
		MeshRenderer component = transform.GetComponent<MeshRenderer>();
		component.sortingOrder = 10;
		component.sortingLayerName = "Map";
		Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
		Color[] array = new Color[mesh.vertices.Length];
		Color color = planet.data.BASE_DATA.mapColor.GetColor();
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		mesh.colors = array;
		transform.name = "Mesh ( " + planet.data.BASE_DATA.name + ")";
		return transform;
	}

	private LineRenderer CreatePlanetOrbitLine(Transform parent, Planet planet)
	{
		Transform transform = UnityEngine.Object.Instantiate(planetOrbitLinePrefab, Vector3.zero, Quaternion.identity, parent);
		transform.GetComponent<Ellipse>().resolution = planet.data.ORBIT_DATA.orbitLineResolution;
		transform.name = "Orbit line";
		transform.localPosition = Vector3.zero;
		transform.localScale = Vector3.one * (float)(planet.data.ORBIT_DATA.orbitHeight / 10000.0);
		LineRenderer component = transform.GetComponent<LineRenderer>();
		component.sortingOrder = 0;
		component.sortingLayerName = "Map";
		component.startColor = GetMapElementColor(Color.white, 0.15f);
		component.endColor = GetMapElementColor(Color.white, 0.06f);
		component.material = orbitLineMaterials[planet.orbitalDepth - 1];
		return component;
	}

	private void CreateCircle(Transform parent, float circleSize, Color color, int sortingOrder, string circleName)
	{
		Transform transform = UnityEngine.Object.Instantiate(SOIPrefab, parent);
		transform.name = circleName;
		transform.localScale = Vector3.one * circleSize;
		transform.localPosition = Vector3.zero;
		SpriteRenderer component = transform.GetComponent<SpriteRenderer>();
		component.color = color;
		component.sortingLayerName = "Map";
		component.sortingOrder = sortingOrder;
		if (circleName == "SOI")
		{
			component.sprite = spriteSOI;
		}
	}

	public void UpdateCameraRotation(Vector3 camRotation)
	{
		foreach (Transform item in syncRotation)
		{
			item.localEulerAngles = camRotation;
		}
	}

	public static Color GetMapElementColor(Color tint, float alpha)
	{
		return new Color(1f, 1f, 1f, alpha) * tint;
	}
}
