using System;
using System.Collections.Generic;
using Assets.Scripts.Save;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
	[Header(" Controller")]
	public string startAdress;

	public Planet loadedPlanet;

	public List<Vessel> vessels;

	public float camTargetAngle;

	public float camAngularVelocity;

	public float createHeight;

	[Header(" Time")]
	public double globalTime;

	public int timewarpPhase;

	public int[] warpSpeeds;

	public double startedTimewarpTime;

	[Header(" Rotation Controll")]
	public float xAxis;

	[Header(" UI Refs")]
	public Image throttleBar;

	public Transform fuelIconPrefab;

	public Text throttlePercentUI;

	public Text throttleOnUI;

	public Text topRightText;

	[Space]
	public MoveModule throttleColorMove;

	[Space]
	public Transform bottomLeftHolder;

	[Space]
	public Transform fuelIconsHolder;

	public List<Transform> fuelIcons;

	public Transform fuelText;

	public Transform powerText;

	[Header(" Prefabs Refs")]
	public Transform vesselPrefab;

	public bool enableVelocityOffset;

	[Header("Camera")]
	[BoxGroup]
	public float cameraDistanceGame;

	[BoxGroup]
	public LayerMask gameView;

	[BoxGroup]
	public LayerMask scaledView;

	[BoxGroup]
	public LayerMask mapView;

	[BoxGroup]
	public float scaledSpaceThreshold;

	public Transform stars;

	public Transform audioListener;

	public Transform explosionParticle;

	public float updatedPersistantTime;

	public float regularUpdateTime;

	public Vector2 mainVesselLocalPosition;

	public Vector2 cameraOffset;

	public Transform directionArrow;

	public CanvasScaler canvas;

	public Canvas canvass;

	public Material flameByHeightMaterial;

	public Vector2 viewPositons;

	public ResourceModule fromTank;

	public ResourceModule toTank;

	public MoveModule fromTankUI;

	public MoveModule toTankUI;

	public TextMesh fromTankPercentText;

	public TextMesh toTankPercentText;

	public TextMesh fromTankPercentTextShadow;

	public TextMesh toTankPercentTextShadow;

	public string cheatsText;

	public PartData emptyPlaceholderPart;

	public void SetCameraDistance(float newDistance)
	{
		cameraDistanceGame = newDistance;
	}

	private void Start()
	{
		IniciateGameScene();
	}

	private void IniciateGameScene()
	{
		updatedPersistantTime = Time.time + 10f;
		Planet planet = startAdress.Planet();
		Double3 @double = new Double3(365.0, planet.data.BASE_DATA.radius + 25.5);
		Ref.map.InitializeMap();
		cheatsText = ((!Ref.infiniteFuel) ? string.Empty : "\n无限燃料") + ((!Ref.noDrag) ? string.Empty : "\n无阻力") + ((!Ref.unbreakableParts) ? string.Empty : "\n坚不可摧") + ((!Ref.noGravity) ? string.Empty : "\n无重力");
		GameSaver.Quicksave quicksave = World.GetWorldByName().LoadPersistant();
		if (Ref.lastScene == Ref.SceneType.Build)
		{
			if (quicksave != null && quicksave.vessels.Count > 0)
			{
				Debug.Log("Loaded persistant for launch");
				quicksave.velocityOffset = Double3.zero;
				GameSaver.LoadForLaunch(quicksave, @double);
			}
			else
			{
				MonoBehaviour.print("Started new world");
				Ref.planetManager.SwitchLocation(planet, @double, false, 0.0);
				Ref.map.followingPlanet = planet;
				Ref.map.UpdateMapPosition(new Double3(0.0, @double.y));
				Ref.map.UpdateMapZoom(@double.y / 10.0);
			}
			CreatePartsFromBuild(@double);
			Ref.mainVessel.SetThrottle(new Vessel.Throttle(false, 0.65f));
			Ref.mapView = false;
			Ref.controller.SetCameraDistance(23f);
		}
		else if (quicksave != null)
		{
			GameSaver.LoadGame(quicksave);
			MonoBehaviour.print("No rocket to create, loaded persistant");
		}
		else
		{
			MonoBehaviour.print("No persistant, this should never happen in game");
		}
	}

	private void CreatePartsFromBuild(Double3 launchPadPosition)
	{
		Build.Blueprint blueprint = JsonUtility.FromJson<Build.Blueprint>(Ref.LoadJsonString(Saving.SaveKey.ToLaunch));
		PartGrid.PositionForLaunch(blueprint.parts, blueprint.rotation);
		List<Part> list = CreateRocket.CreateBuildParts((launchPadPosition - Ref.positionOffset).toVector3, blueprint);
		List<Vessel> list2 = CreateVesselsFromParts(list, Vector2.zero, 0f, new Vessel.Throttle(false, 0.65f), new List<string>());
		foreach (Part item in list)
		{
			item.CreateKeys();
		}
		foreach (Part item2 in list)
		{
			item2.UpdateData();
		}
		foreach (Part item3 in list)
		{
			item3.CreateKeys();
		}
		foreach (Part item4 in list)
		{
			item4.UpdateIcon();
		}
		Ref.mainVessel = ((list2.Count <= 0) ? null : list2[0]);
		Ref.map.UpdateVesselsMapIcons();
		RepositionFuelIcons();
	}

	public List<Vessel> CreateVesselsFromParts(List<Part> unatendedParts, Vector2 velocity, float angularVecloty, Vessel.Throttle throttle, List<string> inheritedArchivments)
	{
		List<Vessel> list = new List<Vessel>();
		for (int i = 0; i < unatendedParts.Count; i++)
		{
			if (unatendedParts[i] != null && unatendedParts[i].vessel == null && unatendedParts[i].gameObject.activeSelf)
			{
				Vessel item = Vessel.CreateVessel(unatendedParts[i], velocity, angularVecloty, throttle, inheritedArchivments, loadedPlanet.mapHolder);
				list.Add(item);
			}
		}
		return list;
	}

	public void Relaunch()
	{
		Ref.LoadScene(Ref.SceneType.Build);
		Ref.loadLaunchedRocket = true;
		AdManager.ShowFullscreen();
		World.GetWorldByName().UpdatePersistant();
	}

	public void BuildNew()
	{
		Ref.LoadScene(Ref.SceneType.Build);
		AdManager.ShowFullscreen();
		World.GetWorldByName().UpdatePersistant();
	}

	public void AskOpenTutorial()
	{
		OpenTutorial();
	}

	public void OpenTutorial()
	{
		Ref.openTutorial = true;
		Ref.LoadScene(Ref.SceneType.MainMenu);
	}

	public void ExitGameScene(bool showAd)
	{
		Ref.LoadScene(Ref.SceneType.MainMenu);
		AdManager.ShowFullscreen();
		World.GetWorldByName().UpdatePersistant();
	}

	private void FixedUpdate()
	{
		if (!Ref.timeWarping && !(Ref.mainVessel == null))
		{
			CheckPositionBounds();
			if (Ref.velocityOffset.sqrMagnitude2d > 0.0)
			{
				Ref.planetManager.UpdatePositionOffset(Ref.positionOffset + Ref.velocityOffset * Time.fixedDeltaTime);
			}
			enableVelocityOffset = GetEnableVelocityOffset(Ref.mainVessel);
			if (enableVelocityOffset)
			{
				CheckVelocityBounds();
			}
			else if (Ref.velocityOffset.sqrMagnitude2d > 0.0)
			{
				OffsetSceneVelocity(-Ref.velocityOffset.toVector2);
			}
		}
	}

	private void Update()
	{
		TransferFuel(Time.deltaTime);
		if (regularUpdateTime < Time.time)
		{
			RegularUpdate();
			regularUpdateTime = Time.time + 0.5f;
		}
		globalTime += (float)warpSpeeds[timewarpPhase] * Time.deltaTime;
		bool flag = Ref.mainVessel != null && Ref.mainVessel.orbits.Count > 0;
		double num = ((!flag) ? double.PositiveInfinity : Ref.mainVessel.orbits[0].stopTimeWarpTime);
		if (flag)
		{
			Ref.planetManager.UpdatePositionOffset(Ref.mainVessel.GetGlobalPosition.roundTo1000);
		}
		if (globalTime > num)
		{
			globalTime = num;
			Ref.planetManager.UpdatePositionOffset(Ref.mainVessel.GetGlobalPosition.roundTo1000);
			timewarpPhase = 1;
			DecelerateTime();
			MenuManager.ShowMsg("时间加速不能低于:" + loadedPlanet.data.BASE_DATA.timewarpHeight / 1000.0 + "km");
		}
	}

	private void TransferFuel(float deltaTime)
	{
		bool flag = fromTank != null;
		bool flag2 = toTank != null;
		float num = ((Screen.width <= Screen.height) ? 1f : ((float)Screen.height / (float)Screen.width * 1.075f)) * Mathf.Lerp((float)Screen.width / (float)Screen.height, 1.25f, 0.5f);
		if (fromTankPercentText.gameObject.activeSelf != (flag && cameraDistanceGame < 50f))
		{
			fromTankPercentText.gameObject.SetActive(flag && cameraDistanceGame < 50f);
		}
		if (toTankPercentText.gameObject.activeSelf != (flag2 && cameraDistanceGame < 50f))
		{
			toTankPercentText.gameObject.SetActive(flag2 && cameraDistanceGame < 50f);
		}
		if (flag)
		{
			TextMesh textMesh = fromTankPercentText;
			string text = (int)(fromTank.resourceGroup.resourcePercent * 100.0)/*cast due to constrained. prefix*/ + "%";
			fromTankPercentTextShadow.text = text;
			textMesh.text = text;
			fromTankUI.SetTargetTime((float)fromTank.resourceGroup.resourcePercent);
			fromTankUI.transform.position = fromTank.transform.TransformPoint(fromTank.part.centerOfMass) + (Vector3)cameraOffset;
			fromTankUI.transform.localScale = Vector3.one * cameraDistanceGame * num * 0.9f;
		}
		if (flag2)
		{
			TextMesh textMesh2 = toTankPercentText;
			string text = (int)(toTank.resourceGroup.resourcePercent * 100.0)/*cast due to constrained. prefix*/ + "%";
			toTankPercentTextShadow.text = text;
			textMesh2.text = text;
			toTankUI.SetTargetTime((float)toTank.resourceGroup.resourcePercent);
			toTankUI.transform.position = toTank.transform.TransformPoint(toTank.part.centerOfMass) + (Vector3)cameraOffset;
			toTankUI.transform.localScale = Vector3.one * cameraDistanceGame * num * 0.9f;
		}
		if (!flag || !flag2)
		{
			return;
		}
		if (fromTank.part.vessel != Ref.mainVessel || toTank.part.vessel != Ref.mainVessel)
		{
			fromTank = null;
			toTank = null;
			return;
		}
		double num2 = (float)warpSpeeds[timewarpPhase] * deltaTime * 0.5f;
		double resourceAmount = fromTank.resourceGroup.resourceAmount;
		double num3 = toTank.resourceGroup.resourceSpace - toTank.resourceGroup.resourceAmount;
		if (num2 > resourceAmount)
		{
			num2 = resourceAmount;
		}
		if (num2 > num3)
		{
			num2 = num3;
		}
		if (num2 != 0.0)
		{
			fromTank.resourceGroup.TakeResource(num2);
			toTank.resourceGroup.AddResource(num2);
		}
	}

	private void RegularUpdate()
	{
		if (updatedPersistantTime < Time.time)
		{
			updatedPersistantTime = Time.time + 15f;
			World.GetWorldByName().UpdatePersistant();
		}
	}

	private void LateUpdate()
	{
		int explosionCount = Ref.explosionCount;
		Ref.explosionCount = 0;
		if (Ref.mainVessel != null)
		{
			mainVesselLocalPosition = ((!Ref.timeWarping) ? Ref.mainVessel.partsManager.rb2d.worldCenterOfMass : (Ref.mainVessel.GetGlobalPosition - Ref.positionOffset).toVector2);
		}
		PositionCameraGame();
		cameraOffset = Vector2.zero;
		audioListener.transform.position = new Vector3(mainVesselLocalPosition.x, mainVesselLocalPosition.y, Ref.mapView ? (0f - Mathf.Max(cameraDistanceGame, 1000f)) : (0f - cameraDistanceGame));
		UpdateMainVesselStats();
		UpdateCameraRotation();
		UpdateUI((!(Ref.mainVessel != null)) ? 0.0 : Ref.mainVessel.GetGlobalVelocity.magnitude2d, (!(Ref.mainVesselTerrainHeight < 2000.0)) ? Ref.mainVesselHeight : Ref.mainVesselTerrainHeight, (Ref.mainVesselTerrainHeight < 2000.0) ? true : false);
		float num = ((!loadedPlanet.data.hasAtmosphere) ? 0f : Mathf.Clamp01((float)(Ref.mainVesselHeight / loadedPlanet.data.ATMOSPHERE_DATA.PHYSICS.height)));
		float num2 = ((!(loadedPlanet.data.BASE_DATA.name == startAdress)) ? 0f : (1f - num));
		flameByHeightMaterial.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.Clamp01(num2 * 1.5f)));
		Ref.main.Camera.fieldOfView = 60f * ((Screen.width <= Screen.height) ? 1f : ((float)Screen.height / (float)Screen.width * 1.075f));
		SetVelocityDirectionMarker((!(Ref.mainVessel != null)) ? Vector2.zero : ((Vector2)(Quaternion.Euler(0f, 0f, 0f - Ref.main.Camera.transform.rotation.eulerAngles.z) * Ref.mainVessel.GetGlobalVelocity.toVector3)));
		float num3 = ((Ref.mainVessel != null && Ref.mainVessel.RCS) ? 1 : 0);
		if (Ref.inputController.inputButtonsAnimation.targetTime.floatValue != num3)
		{
			Ref.inputController.inputButtonsAnimation.SetTargetTime(num3);
		}
	}

	private void PositionCameraGame()
	{
		if (Ref.mapView)
		{
			return;
		}
		scaledSpaceThreshold = ((!(Ref.mainVesselHeight > 500000.0)) ? 100000f : 5000f);
		bool flag = cameraDistanceGame > scaledSpaceThreshold;
		Ref.main.Camera.cullingMask = ((!flag) ? gameView : scaledView);
		if (!flag)
		{
			float num = ((!(Ref.mainVessel != null)) ? 100f : Ref.mainVessel.partsManager.GetPhysicalRadius());
			float f = Ref.main.Camera.fieldOfView * ((float)Math.PI / 180f);
			float num2 = Mathf.Sin(f) / ((1f + Mathf.Cos(f)) * 0.5f);
			float num3 = cameraDistanceGame * num2 / 2f * Mathf.Lerp(Mathf.Clamp01((float)Screen.width / (float)Screen.height), 1f, 0.25f);
			float num4 = Mathf.Max(num - num3, 0f);
			if (num4 == 0f)
			{
				viewPositons = Vector2.zero;
			}
			else if (viewPositons.sqrMagnitude > num4 * num4)
			{
				viewPositons /= viewPositons.magnitude / num4;
			}
			Ref.main.Camera.transform.position = new Vector3(mainVesselLocalPosition.x, mainVesselLocalPosition.y, 0f - cameraDistanceGame) + (Vector3)(cameraOffset + viewPositons);
			Utility.DrawCircle(Ref.main.Camera.transform.position, num3, 40, Color.red, 0f);
		}
		else
		{
			Ref.main.Camera.transform.position = new Vector3(mainVesselLocalPosition.x, mainVesselLocalPosition.y, 0f - cameraDistanceGame) / 10000f;
		}
		if (flag)
		{
			Ref.planetManager.PositionLowRezPlanets(globalTime);
		}
		Ref.planetManager.scaledHolder.gameObject.SetActive(flag);
		Ref.planetManager.chuncksHolder.gameObject.SetActive(!flag);
	}

	private void UpdateMainVesselStats()
	{
		if (!(Ref.mainVessel == null))
		{
			Ref.mainVesselHeight = Ref.mainVessel.GetGlobalPosition.magnitude2d - loadedPlanet.data.BASE_DATA.radius;
			Ref.mainVesselAngleToPlanet = (float)Math.Atan2(Ref.mainVessel.GetGlobalPosition.y, Ref.mainVessel.GetGlobalPosition.x) * 57.29578f;
			Ref.mainVesselTerrainHeight = Ref.mainVesselHeight - PlanetData.TerrainModule.GetTerrainSampleAtAngle(Ref.mainVesselAngleToPlanet - 90f, loadedPlanet);
		}
	}

	private void UpdateCameraRotation()
	{
		camTargetAngle = ((!(Ref.mainVesselHeight < loadedPlanet.data.BASE_DATA.timewarpHeight)) ? 0f : (Ref.mainVesselAngleToPlanet - 90f));
		Ref.main.Camera.transform.eulerAngles = new Vector3(0f, 0f, Mathf.SmoothDampAngle(Ref.main.Camera.transform.eulerAngles.z, camTargetAngle, ref camAngularVelocity, 0.5f));
		stars.transform.eulerAngles = new Vector3(0f, 0f, 100f);
		Ref.map.UpdateCameraRotation(Ref.main.Camera.transform.localEulerAngles);
	}

	private void SomeStuff()
	{
	}

	[Button("Take screenshot")]
	private void A()
	{
		ScreenCapture.CaptureScreenshot("Assets/Screenshots/Screenshot" + UnityEngine.Random.Range(0, 100000) + ".png");
	}

	public void SwitchVessel()
	{
		if (Ref.map.selected is Vessel)
		{
			SwitchVessel(Ref.map.selected as Vessel);
			if (Ref.map.target as Vessel == Ref.mainVessel)
			{
				Ref.map.SetTarget(null);
			}
		}
	}

	public void SwitchVessel(Vessel newVessel)
	{
		if (newVessel == null || newVessel == Ref.mainVessel)
		{
			return;
		}
		viewPositons = Vector2.zero;
		Vessel mainVessel = Ref.mainVessel;
		Ref.mainVessel = newVessel;
		Ref.map.UpdateVesselsMapIcons();
		Double3 getGlobalPosition = newVessel.GetGlobalPosition;
		Planet getVesselPlanet = newVessel.GetVesselPlanet;
		RepositionFuelIcons();
		Ref.map.followingPlanet = newVessel.GetVesselPlanet;
		Ref.map.UpdateMapPosition(new Double3(newVessel.GetGlobalPosition.x, newVessel.GetGlobalPosition.y, Ref.map.mapPosition.z));
		Ref.map.UpdateMapZoom(Ref.map.mapPosition.z);
		newVessel.SetThrottle(newVessel.throttle);
		bool flag = newVessel.GetGlobalPosition.magnitude2d < newVessel.GetVesselPlanet.data.BASE_DATA.radius + newVessel.GetVesselPlanet.data.BASE_DATA.timewarpHeight;
		foreach (Vessel vessel in Ref.controller.vessels)
		{
			vessel.SetVesselState(Vessel.ToState.ToUnloaded);
		}
		Vessel.ToState toState = ((Ref.timeWarping && !flag) ? Vessel.ToState.ToTimewarping : Vessel.ToState.ToRealTime);
		Ref.planetManager.SwitchLocation(getVesselPlanet, getGlobalPosition, false, 0.0);
		if (Ref.timeWarping && flag)
		{
			timewarpPhase = 1;
			DecelerateTime();
			MenuManager.ShowMsg("时间加速不能低于:" + newVessel.GetVesselPlanet.data.BASE_DATA.timewarpHeight / 1000.0 + "km");
		}
		else
		{
			if (toState == Vessel.ToState.ToRealTime)
			{
				bool flag2 = GetEnableVelocityOffset(Ref.mainVessel);
				double x = ((!flag2) ? 0.0 : (Math.Floor((Math.Abs(Ref.mainVessel.GetGlobalVelocity.x) + 25.0) / 50.0) * 50.0 * (double)Math.Sign(Ref.mainVessel.GetGlobalVelocity.x)));
				double y = ((!flag2) ? 0.0 : (Math.Floor((Math.Abs(Ref.mainVessel.GetGlobalVelocity.y) + 25.0) / 50.0) * 50.0 * (double)Math.Sign(Ref.mainVessel.GetGlobalVelocity.y)));
				Ref.velocityOffset = new Double3(x, y);
			}
			Ref.mainVessel.SetVesselState(toState);
		}
		Ref.mainVessel.SetThrottle(new Vessel.Throttle(false, newVessel.throttle.throttleRaw));
		Ref.map.Select(null);
		Ref.planetManager.totalDistanceMoved += 100000.0;
	}

	public void StartRecovery()
	{
		if (Ref.map.selected == null || !(Ref.map.selected is Vessel))
		{
			return;
		}
		Ref.inputController.leftArrow.parent.gameObject.SetActive(false);
		Ref.inputController.recoverMenuHolder.transform.GetChild(8).GetChild(1).GetComponent<Text>()
			.text = ((!(Ref.map.selected as Vessel == Ref.mainVessel)) ? "Complete Recovery" : "Complete Mission");
		List<string> list = new List<string>((Ref.map.selected as Vessel).vesselAchievements);
		if (list.Contains("达到10公里高度.") && list.Count > 2)
		{
			list.Remove("达到5公里高度.");
		}
		if (list.Contains("达到15公里高度.") && list.Count > 2)
		{
			list.Remove("达到10公里高度.");
		}
		if (list.Contains("越过卡门线,离开大气层,进入太空.") && list.Count > 2)
		{
			list.Remove("达到15公里高度.");
		}
		if (list.Contains("达到最低点" + Ref.controller.startAdress + " orbit.") && list.Count > 4)
		{
			list.Remove("越过卡门线,离开大气层,进入太空.");
		}
		Ref.inputController.recoverMenuHolder.SetActive(true);
		bool flag = Ref.inputController.canvasScalers[0].referenceResolution.x == 750f;
		Text component = Ref.inputController.recoverMenuHolder.transform.GetChild(4).GetComponent<Text>();
		Text component2 = Ref.inputController.recoverMenuHolder.transform.GetChild(6).GetComponent<Text>();
		component.transform.localPosition = new Vector3(0f, component.transform.localPosition.y);
		Ref.inputController.recoverMenuHolder.transform.GetChild(3).localPosition = new Vector3(0f, component.transform.localPosition.y + 10f);
		Ref.inputController.recoverMenuHolder.transform.GetChild(5).localPosition = new Vector3(5000f, component.transform.localPosition.y + 10f);
		Ref.inputController.recoverMenuHolder.transform.GetChild(3).localScale = new Vector3(1f, (!flag) ? 0.51f : 1f);
		Ref.inputController.recoverMenuHolder.transform.GetChild(5).localScale = new Vector3(1f, (!flag) ? 0.51f : 1f);
		component.text = string.Empty;
		component2.text = string.Empty;
		int num = 0;
		Text text = component;
		foreach (string item in list)
		{
			text.text += "- ";
			for (int i = 0; i < item.Length; i++)
			{
				if (item[i].ToString() == "/")
				{
					text.text += "\r\n \r\n ";
					num += 2;
				}
				else
				{
					text.text += item[i];
				}
			}
			text.text += "\r\n \r\n \r\n";
			num += 3;
			if (num > ((!flag) ? 24 : 51))
			{
				if (text == component2 || flag)
				{
					break;
				}
				text = component2;
				num = 0;
				component.transform.localPosition = new Vector3(-300f, component.transform.localPosition.y);
				component2.transform.localPosition = new Vector3(300f, component.transform.localPosition.y);
				Ref.inputController.recoverMenuHolder.transform.GetChild(3).localPosition = new Vector3(-300f, component.transform.localPosition.y + 10f);
				Ref.inputController.recoverMenuHolder.transform.GetChild(5).localPosition = new Vector3(300f, component.transform.localPosition.y + 10f);
			}
		}
	}

	public void CompleteRecovery()
	{
		if (Ref.map.selected == null || !(Ref.map.selected is Vessel))
		{
			return;
		}
		Vessel vessel = Ref.map.selected as Vessel;
		if (!vessel.OnSurface)
		{
			return;
		}
		if (vessel == Ref.mainVessel)
		{
			if (vessel.vesselAchievements.Contains("到达近地轨道.") && !Saving.LoadSetting(Saving.SettingKey.askedToRate1) && !Saving.LoadSetting(Saving.SettingKey.hasRated))
			{
				Saving.SaveSetting(Saving.SettingKey.askedToRate1, true);
				AskRate();
				CancelRecovery();
			}
			else if (vessel.vesselAchievements.Contains("Landed on Moon surface.") && !Saving.LoadSetting(Saving.SettingKey.askedToRate2) && !Saving.LoadSetting(Saving.SettingKey.hasRated))
			{
				Saving.SaveSetting(Saving.SettingKey.askedToRate2, true);
				AskRate();
				CancelRecovery();
			}
			else
			{
				vessel.DestroyVessel();
				ExitGameScene(true);
			}
		}
		else
		{
			vessel.DestroyVessel();
			CancelRecovery();
		}
	}

	public void CancelRecovery()
	{
		Ref.inputController.recoverMenuHolder.SetActive(false);
	}

	public void AskEliminateDebris()
	{
		string warning = Utility.SplitLines("确定清除碎片吗?//将摧毁所有无指挥舱或探测器的火箭");
		MenuManager.ShowWarning(warning, new Vector2(1300f, 335f), "清理碎片", ConfirmEliminateDebris, "取消", null, 240);
	}

	private void ConfirmEliminateDebris()
	{
		for (int i = 0; i < vessels.Count; i++)
		{
			if (!vessels[i].controlAuthority)
			{
				vessels[i].DestroyVessel();
				i--;
			}
		}
		AdManager.ShowFullscreen();
	}

	public void DestroySelectedVessel()
	{
		if (Ref.map.selected != null && Ref.map.selected is Vessel)
		{
			Vessel vessel = Ref.map.selected as Vessel;
			if (vessel == Ref.mainVessel)
			{
				MenuManager.ShowMsg("不能摧毁你控制的火箭");
				return;
			}
			string warning = Utility.SplitLines("确定摧毁选定的火箭吗?");
			MenuManager.ShowWarning(warning, new Vector2(1220f, 175f), "摧毁", ConfirmVesselDestroy, "取消", null, 160);
		}
	}

	public void ConfirmVesselDestroy()
	{
		if (Ref.map.selected != null && Ref.map.selected is Vessel)
		{
			Vessel vessel = Ref.map.selected as Vessel;
			vessel.DestroyVessel();
		}
	}

	public void ToggleThrottle()
	{
		if (Ref.timeWarping)
		{
			MenuManager.ShowMsg("时间加速时无法切换油门");
		}
		else
		{
			if (Ref.mainVessel == null)
			{
				return;
			}
			if (!Ref.mainVessel.controlAuthority)
			{
				MenuManager.ShowMsg("失去控制");
				return;
			}
			Ref.mainVessel.SetThrottle(new Vessel.Throttle(!Ref.mainVessel.throttle.throttleOn, Ref.mainVessel.throttle.throttleRaw));
			if (Ref.inputController.instructionToggleThrottleHolder.activeSelf)
			{
				Ref.inputController.instructionToggleThrottleHolder.SetActive(false);
				Ref.inputController.CheckAllInstructions();
			}
			if (Ref.mainVessel.throttle.throttleRaw != 0f)
			{
				foreach (EngineModule engineModule in Ref.mainVessel.partsManager.engineModules)
				{
					if (engineModule.engineOn.boolValue)
					{
						return;
					}
				}
			}
			Ref.inputController.PlayClickSound(0.6f);
		}
	}

	private void UpdateUI(double velocity, double height, bool terrainHeight)
	{
		float num = ((Screen.width <= Screen.height) ? 1f : ((float)Screen.height / (float)Screen.width * 1.075f)) * Mathf.Lerp((float)Screen.width / (float)Screen.height, 1.25f, 0.5f);
		bottomLeftHolder.localScale = Vector3.one * num;
		Vector3 localPosition = Utility.CameraRelativePosition(Ref.main.Camera.fieldOfView, new Vector3(0f, 0f, 1f));
		bottomLeftHolder.localPosition = localPosition;
		bool flag = velocity < 100.0;
		string text = ((int)velocity/*cast due to constrained. prefix*/).ToString();
		if (flag && ((int)(velocity * 10.0) % 10 != 0 || (int)velocity != 0))
		{
			text = text + "." + (int)(velocity * 10.0) % 10;
		}
		topRightText.text = ((!terrainHeight) ? "高度:" : "高度(地形):") + RoundUI(height) + "\n速度:" + text + "m/s" + cheatsText;
	}

	private void SetVelocityDirectionMarker(Vector2 velocity)
	{
		bool flag = velocity.sqrMagnitude > 4f && !Ref.mapView;
		if (directionArrow.gameObject.activeSelf != flag)
		{
			directionArrow.gameObject.SetActive(flag);
		}
		if (flag)
		{
			directionArrow.eulerAngles = new Vector3(0f, 0f, (float)Math.Atan2(velocity.y, velocity.x) * 57.29578f - 90f);
			float num = ((Screen.height <= Screen.width) ? 1f : ((float)Screen.width / (float)Screen.height * 1.77f));
			Vector3 vector = Utility.CameraRelativePosition(Ref.main.Camera.fieldOfView, new Vector3(0f, 0f, 1f));
			float num2 = 0f - vector.x - 0.02f * num;
			float num3 = 0f - vector.y - 0.1f * num;
			Vector3 vector2 = velocity;
			vector2 *= num2 / Mathf.Abs(vector2.x);
			if (Mathf.Abs(vector2.y) > num3)
			{
				vector2 *= num3 / Mathf.Abs(vector2.y);
			}
			directionArrow.localPosition = new Vector3(vector2.x, vector2.y, 1f);
		}
	}

	public string RoundUI(double value)
	{
		if (!double.IsInfinity(value))
		{
			int num = 0;
			string empty = string.Empty;
			if (value < 100.0)
			{
				empty = "m";
				num = 1;
			}
			else if (value < 10000.0)
			{
				empty = "m";
				num = 0;
			}
			else if (value < 100000000.0)
			{
				num = 1;
				value /= 1000.0;
				empty = "km";
			}
			else
			{
				num = 1;
				value /= 1000000.0;
				empty = "Mm";
			}
			int num2 = Ref.GetFigure(value);
			if (num2 == 0)
			{
				num2 = 1;
			}
			if (num != 0)
			{
				num2 += num + 1;
			}
			string text = value.ToString();
			if (num2 > text.Length)
			{
				text += ".0";
			}
			return text.Substring(0, num2) + empty;
		}
		return null;
	}

	private void CheckPositionBounds()
	{
		if (Ref.mainVessel.partsManager.rb2d.position.y > 1000f)
		{
			OffsetScenePosition(new Vector2(0f, 2000f));
		}
		if (Ref.mainVessel.partsManager.rb2d.position.y < -1000f)
		{
			OffsetScenePosition(new Vector2(0f, -2000f));
		}
		if (Ref.mainVessel.partsManager.rb2d.position.x > 1000f)
		{
			OffsetScenePosition(new Vector2(2000f, 0f));
		}
		if (Ref.mainVessel.partsManager.rb2d.position.x < -1000f)
		{
			OffsetScenePosition(new Vector2(-2000f, 0f));
		}
	}

	private void CheckVelocityBounds()
	{
		Vector2 velocity = Ref.mainVessel.partsManager.rb2d.velocity;
		if (Mathf.Abs(velocity.x) > 0.25f || Mathf.Abs(velocity.y) > 0.25f)
		{
			float x = Mathf.Floor((Mathf.Abs(velocity.x) + 0.25f) / 0.5f) * 0.5f * Mathf.Sign(velocity.x);
			float y = Mathf.Floor((Mathf.Abs(velocity.y) + 0.25f) / 0.5f) * 0.5f * Mathf.Sign(velocity.y);
			OffsetSceneVelocity(new Vector2(x, y));
		}
	}

	public void OffsetScenePosition(Vector2 change)
	{
		Ref.planetManager.UpdatePositionOffset(Ref.positionOffset + new Double3(change.x, change.y));
		foreach (Vessel vessel in vessels)
		{
			if (vessel.state == Vessel.State.RealTime)
			{
				vessel.partsManager.rb2d.position -= change;
			}
		}
	}

	public void OffsetSceneVelocity(Vector2 change)
	{
		Ref.velocityOffset += new Double3(change.x, change.y);
		foreach (Vessel vessel in vessels)
		{
			if (vessel.state == Vessel.State.RealTime)
			{
				vessel.partsManager.rb2d.velocity -= change;
			}
		}
	}

	public void DecelerateTime()
	{
		if (timewarpPhase != 0)
		{
			timewarpPhase--;
			MenuManager.ShowMsg(warpSpeeds[timewarpPhase] + "×时间加速", 1.5f);
			if (timewarpPhase == 0 && Ref.timeWarping)
			{
				ExitTimeWarpMode();
			}
		}
		else
		{
			Ref.inputController.instructionsTimewarp.SetActive(false);
		}
	}

	public void AccelerateTime()
	{
		if (Ref.mainVessel == null)
		{
			return;
		}
		Ref.inputController.instructionsTimewarp.SetActive(false);
		if (Ref.mainVesselHeight < loadedPlanet.data.BASE_DATA.timewarpHeight && !Ref.timeWarping)
		{
			if (!Ref.mainVessel.OnSurface)
			{
				MenuManager.ShowMsg("时间加速不能低于" + loadedPlanet.data.BASE_DATA.timewarpHeight / 1000.0 + "km", 1.5f);
				return;
			}
			if (!(Ref.mainVessel.GetGlobalVelocity.magnitude2d < 0.05000000074505806))
			{
				MenuManager.ShowMsg("地面移动时无法时间加速", 1.5f);
				return;
			}
		}
		if (Ref.mainVessel.throttle.throttleOn && Ref.mainVessel.throttle.throttleRaw > 0f)
		{
			foreach (EngineModule engineModule in Ref.mainVessel.partsManager.engineModules)
			{
				if (engineModule.engineOn.boolValue)
				{
					MenuManager.ShowMsg("加速时无法时间加速", 1.25f);
					return;
				}
			}
		}
		if (timewarpPhase != warpSpeeds.Length - 1)
		{
			timewarpPhase++;
			MenuManager.ShowMsg(warpSpeeds[timewarpPhase] + "×时间加速", 1.25f);
			if (!Ref.timeWarping)
			{
				EnterTimeWarpMode();
			}
		}
	}

	public void EnterTimeWarpMode()
	{
		Ref.timeWarping = true;
		startedTimewarpTime = globalTime;
		Ref.mainVessel.SetThrottle(new Vessel.Throttle(false, Ref.mainVessel.throttle.throttleRaw));
		foreach (Vessel vessel in Ref.controller.vessels)
		{
			if (vessel.state == Vessel.State.RealTime)
			{
				vessel.SetVesselState(Vessel.ToState.ToTimewarping);
			}
		}
		Ref.velocityOffset = Double3.zero;
		Ref.map.DrawOrbitLines();
	}

	public void ExitTimeWarpMode()
	{
		UpdateTerrain();
		Ref.timeWarping = false;
		Ref.planetManager.UpdatePositionOffset(Ref.mainVessel.GetGlobalPosition.roundTo1000);
		bool flag = GetEnableVelocityOffset(Ref.mainVessel);
		double x = ((!flag) ? 0.0 : (Math.Floor((Math.Abs(Ref.mainVessel.GetGlobalVelocity.x) + 25.0) / 50.0) * 50.0 * (double)Math.Sign(Ref.mainVessel.GetGlobalVelocity.x)));
		double y = ((!flag) ? 0.0 : (Math.Floor((Math.Abs(Ref.mainVessel.GetGlobalVelocity.y) + 25.0) / 50.0) * 50.0 * (double)Math.Sign(Ref.mainVessel.GetGlobalVelocity.y)));
		Ref.velocityOffset = new Double3(x, y);
		if (Ref.mainVessel != null)
		{
			Ref.mainVessel.SetVesselState(Vessel.ToState.ToRealTime);
		}
		foreach (Vessel vessel in Ref.controller.vessels)
		{
			if (vessel != Ref.mainVessel && (vessel.state == Vessel.State.OnRails || vessel.state == Vessel.State.Stationary))
			{
				vessel.SetVesselState(Vessel.ToState.ToRealTime);
			}
		}
	}

	private void UpdateTerrain()
	{
		Ref.planetManager.totalDistanceMoved += 100000.0;
	}

	public void RepositionFuelIcons()
	{
		for (int i = 0; i < vessels.Count; i++)
		{
			for (int j = 0; j < vessels[i].partsManager.resourceGrups.Count; j++)
			{
				vessels[i].partsManager.resourceGrups[j].fuelIcon = null;
			}
		}
		int num = ((!(Ref.mainVessel == null)) ? Ref.mainVessel.partsManager.resourceGrups.Count : 0);
		while (num > fuelIcons.Count)
		{
			fuelIcons.Add(CreateNewFuelIcon());
		}
		List<ResourceGroup> list = new List<ResourceGroup>();
		for (int k = 0; k < num; k++)
		{
			if (Ref.mainVessel.partsManager.resourceGrups[k].resourceType == Resource.Type.Fuel)
			{
				list.Add(Ref.mainVessel.partsManager.resourceGrups[k]);
			}
		}
		for (int l = 0; l < num; l++)
		{
			if (Ref.mainVessel.partsManager.resourceGrups[l].resourceType == Resource.Type.Power)
			{
				list.Add(Ref.mainVessel.partsManager.resourceGrups[l]);
			}
		}
		float num2 = 0.08f;
		fuelText.gameObject.SetActive(false);
		powerText.gameObject.SetActive(false);
		for (int m = 0; m < fuelIcons.Count; m++)
		{
			if (m < num)
			{
				if (m > 0 && list[m].resourceType == Resource.Type.Power && list[m - 1].resourceType == Resource.Type.Fuel)
				{
					num2 += 0.24f;
				}
				if (list[m].resourceType == Resource.Type.Fuel)
				{
					fuelText.transform.localPosition = new Vector3(-0.55f, (float)m * 0.32f + num2 + 0.28f, 0f);
					fuelText.gameObject.SetActive(true);
				}
				if (list[m].resourceType == Resource.Type.Power)
				{
					powerText.transform.localPosition = new Vector3(-0.55f, (float)m * 0.32f + num2 + 0.28f, 0f);
					powerText.gameObject.SetActive(true);
				}
				fuelIcons[m].gameObject.SetActive(true);
				fuelIcons[m].GetChild(0).localScale = new Vector3((float)list[m].resourcePercent, fuelIcons[m].GetChild(0).localScale.y, 1f);
				list[m].fuelIcon = fuelIcons[m];
				list[m].fuelIcon.localPosition = new Vector3(0f, (float)m * 0.32f + num2, 0f);
				fuelIcons[m].transform.rotation = Ref.main.Camera.transform.rotation;
			}
			else
			{
				fuelIcons[m].gameObject.SetActive(false);
			}
		}
	}

	private Transform CreateNewFuelIcon()
	{
		Transform transform = UnityEngine.Object.Instantiate(fuelIconPrefab, fuelIconsHolder);
		transform.SetAsFirstSibling();
		return transform;
	}

	private bool GetEnableVelocityOffset(Vessel vessel)
	{
		return Ref.planetManager.colliderChuncks.Count == 0;
	}

	public void AskRate()
	{
		MenuManager.ShowWarning("Would you like to rate or review the game? \n\n Rating the game helps me a lot as a developer. \n\n Leaving a review will also help me improve the game, as I still read a LOT of reviews each day.", new Vector2(1260f, 750f), "Rate", OpenRating, "取消", StartRecovery, 220);
	}

	public void OpenRating()
	{
		Application.OpenURL("https://play.google.com/store/apps/details?id=com.StefMorojna.SpaceflightSimulator");
		Saving.SaveSetting(Saving.SettingKey.hasRated, true);
		StartRecovery();
	}
}
