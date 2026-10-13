using System;
using System.Collections.Generic;
using Assets.Scripts.Addons.FileStructure;
using Assets.Scripts.Save;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace NewBuildSystem
{
	public class Build : MonoBehaviour
	{
		[Serializable]
		public class Blueprint
		{
			[Serializable]
			public class PlacedPartSave
			{
				public string partName;

				public Vector2 position;

				public Orientation orientation;

				public int skinId;

				public PlacedPartSave(PlacedPart part)
				{
					partName = part.partData.name;
					position = part.position;
					orientation = part.orientation;
					skinId = part.skinId;
				}

				public static List<PlacedPartSave> ToSave(List<PlacedPart> parts)
				{
					List<PlacedPartSave> list = new List<PlacedPartSave>();
					for (int i = 0; i < parts.Count; i++)
					{
						list.Add(new PlacedPartSave(parts[i]));
					}
					return list;
				}

				public static List<PlacedPart> FromSave(List<PlacedPartSave> parts)
				{
					List<PlacedPart> list = new List<PlacedPart>();
					for (int i = 0; i < parts.Count; i++)
					{
						PartData partData = parts[i].partName.PartData();
						if (partData != null)
						{
							list.Add(new PlacedPart(null, parts[i].position, parts[i].orientation, false, parts[i].skinId, partData, new List<string>()));
						}
					}
					return list;
				}
			}

			public string saveName;

			public Vector3 cameraPosition;

			public List<PlacedPartSave> parts;

			public int rotation;

			public Blueprint(Vector3 cameraPosition, List<PlacedPart> parts, int rotation)
			{
				this.cameraPosition = cameraPosition;
				this.parts = PlacedPartSave.ToSave(parts);
				this.rotation = rotation;
			}

			public static List<string> GetBlueprintNames()
			{
				List<string> list = new List<string>();
				ObjectFile[] array = Saving.BlueprintLocation().FilesAtLocation();
				foreach (ObjectFile objectFile in array)
				{
					if (objectFile.GetPathExtention() == "bp")
					{
						list.Add(objectFile.FileName());
					}
				}
				return list;
			}

			public static void CreateBlueprint(string blueprintName, Blueprint blueprint)
			{
				blueprintName = blueprintName.Escape();
				if (blueprintName == string.Empty)
				{
					blueprintName = "Unnamed Blueprint";
				}
				Saving.BlueprintLocation().ToFile().CreatePath();
				ObjectFile objectFile = Saving.BlueprintLocation().ToFile();
				objectFile.Extend(blueprintName + ".bp");
				objectFile.PreventOverwrite();
				objectFile.FlushJUTILObject(blueprint);
			}

			public static Blueprint LoadBlueprint(string name)
			{
				ObjectFile objectFile = Saving.BlueprintLocation().ToFile(name + ".bp", false);
				if (!objectFile.Exists())
				{
					return null;
				}
				Blueprint obj;
				objectFile.CastFromJUTIL<Blueprint>(out obj);
				return obj;
			}

			public static void DeleteBlueprint(string name)
			{
				ObjectFile objectFile = Saving.BlueprintLocation().ToFile(name + ".bp", false);
				objectFile.Delete();
			}
		}

		public static Build main;

		public PickPartGrid pickGrid;

		public PartGrid buildGrid;

		[BoxGroup]
		[Range(0f, 1f)]
		public float snapStrength;

		[BoxGroup]
		public float smoothIncrease;

		[BoxGroup]
		public float smoothDecay;

		[BoxGroup]
		[Space]
		public float pickMenuDistance;

		[BoxGroup]
		public Vector2 pickMenuPosition;

		public float minZoom;

		public float maxZoomBasic;

		public float maxZoomFull;

		public HoldingPart[] holdingParts = new HoldingPart[5];

		[BoxGroup("UI", true, false, 0)]
		public Text exitButton;

		[BoxGroup("UI", true, false, 0)]
		public Text partName;

		[BoxGroup("UI", true, false, 0)]
		public Text partDescription;

		[BoxGroup("UI", true, false, 0)]
		public RectTransform descriptionBackground;

		[BoxGroup("UI", true, false, 0)]
		public GameObject zoomButtonsHolder;

		[BoxGroup("UI", true, false, 0)]
		public GameObject descriptionHolder;

		public Vector3 camPosBasic;

		public Vector3 camPosExtended;

		[BoxGroup("Icon Generation", true, false, 0)]
		public Camera previewIconCamera;

		public Image skinIcon;

		public Text stats;

		[BoxGroup("Snap", true, false, 0)]
		public Transform snapIconPrefab;

		[BoxGroup("Snap", true, false, 0)]
		public List<Transform> snapIconsPool = new List<Transform>();

		[BoxGroup("Snap", true, false, 0)]
		public int poolIndex;

		[BoxGroup("Snap", true, false, 0)]
		public float iconScale;

		[BoxGroup("Snap", true, false, 0)]
		public float snapDistance;

		public float maxZoom
		{
			get
			{
				return maxZoomFull * 3f * 3f;
			}
		}

		private void Awake()
		{
			main = this;
		}

		private void Start()
		{
			PositionCameraStart();
			if (Ref.lastScene == Ref.SceneType.Game && World.GetWorldByName().LoadPersistant().mainVesselId != -1)
			{
				exitButton.text = "继续";
			}
			pickGrid.Initialize();
			pickGrid.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(0f, Screen.height, pickMenuDistance)) + (Vector3)pickMenuPosition;
			if (Ref.loadLaunchedRocket)
			{
				Ref.loadLaunchedRocket = false;
				LoadSave(JsonUtility.FromJson<Blueprint>(Ref.LoadJsonString(Saving.SaveKey.ToLaunch)));
			}
		}

		private void DrawSnapPoints()
		{
			Vector3 size = Vector3.one * ((0f - Ref.main.Camera.transform.position.z) * 0.006f + 0.04f);
			List<string> holdingTypes = GetHoldingTypes();
			poolIndex = 0;
			foreach (PlacedPart part in buildGrid.parts)
			{
				PartData.SwitchPoint[] switchPoints = part.partData.switchPoints;
				foreach (PartData.SwitchPoint switchPoint in switchPoints)
				{
					if (switchPoint.magnet && holdingTypes.Contains(switchPoint.type))
					{
						DrawSnapPoint(part.GetWorldPosition(switchPoint.position), size);
					}
				}
			}
			HoldingPart[] array = holdingParts;
			foreach (HoldingPart holdingPart in array)
			{
				if (HoldingPart.isNull(holdingPart))
				{
					continue;
				}
				PartData.SwitchPoint[] switchPoints2 = holdingPart.part.partData.switchPoints;
				foreach (PartData.SwitchPoint switchPoint2 in switchPoints2)
				{
					if (switchPoint2.magnet)
					{
						DrawSnapPoint((Vector3)(switchPoint2.position * holdingPart.part.orientation) + holdingPart.part.partIcon.position, size);
					}
				}
			}
			for (int l = poolIndex; l < snapIconsPool.Count && snapIconsPool[l].gameObject.activeSelf; l++)
			{
				snapIconsPool[l].gameObject.SetActive(false);
			}
		}

		private void DrawSnapPoint(Vector3 position, Vector3 size)
		{
			if (poolIndex == snapIconsPool.Count)
			{
				snapIconsPool.Add(UnityEngine.Object.Instantiate(snapIconPrefab, base.transform));
			}
			snapIconsPool[poolIndex].localPosition = position;
			snapIconsPool[poolIndex].localScale = size * iconScale;
			if (!snapIconsPool[poolIndex].gameObject.activeSelf)
			{
				snapIconsPool[poolIndex].gameObject.SetActive(true);
			}
			poolIndex++;
		}

		private List<string> GetHoldingTypes()
		{
			List<string> list = new List<string>();
			HoldingPart[] array = holdingParts;
			foreach (HoldingPart holdingPart in array)
			{
				if (HoldingPart.isNull(holdingPart))
				{
					continue;
				}
				PartData.SwitchPoint[] switchPoints = holdingPart.part.partData.switchPoints;
				foreach (PartData.SwitchPoint switchPoint in switchPoints)
				{
					if (switchPoint.magnet && !list.Contains(switchPoint.type))
					{
						list.Add(switchPoint.type);
					}
				}
			}
			return list;
		}

		public void PositionCameraStart()
		{
			Ref.main.Camera.transform.position = camPosExtended;
		}

		private void LateUpdate()
		{
			DrawSnapPoints();
			if (!MenuManager.EnabledMenuBack)
			{
				if (Input.GetKeyDown(KeyCode.A))
				{
					Rotate90();
				}
				if (Input.GetKeyDown(KeyCode.S))
				{
					FlipX();
				}
				if (Input.GetKeyDown(KeyCode.D))
				{
					FlipY();
				}
				if (Input.GetKeyDown(KeyCode.F))
				{
					NextSkin();
				}
			}
			MoveCamera(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) * 0.03f);
		}

		private void Update()
		{
			if (MenuManager.EnabledMenuBack)
			{
				return;
			}
			MoveCamera(new Vector3(0f, 0f, (0f - Ref.inputController.horizontalAxis) * Time.deltaTime));
			bool flag = false;
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]))
				{
					Vector2 vector = holdingParts[i].part.partIcon.position + (Vector3)(holdingParts[i].part.partData.centerOfRotation * holdingParts[i].part.orientation);
					flag = pickGrid.IsInsideDropArea(Utility.ToDepth(vector, pickMenuDistance));
					break;
				}
			}
			if (descriptionHolder.activeSelf != flag)
			{
				descriptionHolder.SetActive(flag);
			}
			if (Input.GetKeyDown("u"))
			{
				Ref.inputController.leftArrow.transform.parent.gameObject.SetActive(!Ref.inputController.leftArrow.transform.parent.gameObject.activeSelf);
			}
		}

		public int GetOrientation()
		{
			return 0;
		}

		public void MoveCamera(Vector3 delta)
		{
			Transform transform = Ref.main.Camera.transform;
			transform.position = new Vector3(Mathf.Clamp(transform.position.x + delta.x, 0f, buildGrid.gridSize.Value.x), Mathf.Clamp(transform.position.y + delta.y, 0f, buildGrid.gridSize.Value.y), 0f - Mathf.Clamp((0f - transform.position.z) * (1f - delta.z), minZoom, maxZoom));
			pickGrid.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(0f, Screen.height, pickMenuDistance)) + (Vector3)pickMenuPosition;
		}

		public void OnClickUI(Vector2 clickPosPixel)
		{
			Transform transform = pickGrid.PointCastButtons(Camera.main.ScreenToWorldPoint((Vector3)clickPosPixel + new Vector3(0f, 0f, pickMenuDistance)));
			if (!(transform == null) && !(transform.GetComponent<CustomEvent>() == null))
			{
				transform.GetComponent<CustomEvent>().InvokeEvenets();
				StartCoroutine(Ref.inputController.ClickGlow(transform.GetChild(0).gameObject));
			}
		}

		public void OnTouchStart(int fingerId, Vector2 touchPosWorld)
		{
			if (!MenuManager.EnabledMenuBack)
			{
				Transform transform = pickGrid.PointCastButtons(Utility.ToDepth(touchPosWorld, pickMenuDistance));
				if (transform != null)
				{
					Ref.inputController.touchesInfo[fingerId].touchState = Touch.TouchState.OnUI;
					Ref.inputController.touchesInfo[fingerId].touchDownButton = transform;
				}
				else
				{
					TryTakePart(fingerId, touchPosWorld);
				}
			}
		}

		public void OnTouchStay(int fingerId, Vector2 touchPosWorld, Vector2 deltaPixel)
		{
			if (!MenuManager.EnabledMenuBack)
			{
				if (HoldingPart.isNull(holdingParts[fingerId]))
				{
					MoveCamera((Vector2)(Camera.main.ScreenToWorldPoint((Vector3)deltaPixel + new Vector3(Screen.width / 2, Screen.height / 2, 0f - Camera.main.transform.position.z)) - Camera.main.transform.position));
					pickGrid.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(0f, Screen.height, pickMenuDistance)) + (Vector3)pickMenuPosition;
					return;
				}
				Vector2 vector = touchPosWorld + holdingParts[fingerId].holdingOffset;
				Vector2 snapPosition = GetSnapPosition(vector, holdingParts[fingerId].part);
				snapStrength = GetSnapStrength(vector, holdingParts[fingerId].part, deltaPixel);
				holdingParts[fingerId].part.partIcon.position = Vector3.Lerp(vector, snapPosition, snapStrength);
			}
		}

		public void OnTouchEnd(int fingerId, Vector2 touchPosWorld)
		{
			if (!MenuManager.EnabledMenuBack && !HoldingPart.isNull(holdingParts[fingerId]))
			{
				Vector2 partPos = touchPosWorld + holdingParts[fingerId].holdingOffset;
				Vector2 snapPos = GetSnapPosition(partPos, holdingParts[fingerId].part);
				Vector2 position = Vector2.Lerp(partPos, snapPos, 1f - snapStrength) + (HoldingPart.isNull(holdingParts[fingerId]) ? Vector2.zero : (holdingParts[fingerId].part.partData.centerOfRotation * holdingParts[fingerId].part.orientation));
				if (!IsInsideDiscardZone(position))
				{
					PlacedPart part = holdingParts[fingerId].part;
					buildGrid.PlacePart(new PlacedPart(null, snapPos, part.orientation.DeepCopy(), true, part.skinId, part.partData, new List<string>()), true);
				}
				if (holdingParts[fingerId].part.partIcon != null)
				{
					UnityEngine.Object.Destroy(holdingParts[fingerId].part.partIcon.gameObject);
				}
				holdingParts[fingerId] = null;
			}
		}

		private bool IsInsideDiscardZone(Vector2 position)
		{
			if (pickGrid.IsInsideDropArea(Utility.ToDepth(position, pickMenuDistance)))
			{
				return true;
			}
			Vector2 vector = new Vector2(-0.5f, -0.5f);
			Vector2 vector2 = buildGrid.gridSize.Value + new Vector2(0.5f, 0.5f);
			if (!Utility.IsInsideRange(position.x, vector.x, vector2.x) || !Utility.IsInsideRange(position.y, vector.y, vector2.y))
			{
				return true;
			}
			return false;
		}

		private Vector2 GetSnapPosition(Vector2 partPos, PlacedPart part)
		{
			bool hasSnap = false;
			Vector2 magnetSnapPos = GetMagnetSnapPos(partPos, part, ref hasSnap);
			if (hasSnap)
			{
				return magnetSnapPos;
			}
			Vector2 vector = Utility.RoundToHalf(partPos);
			if (!buildGrid.Overlap(part, vector))
			{
				return vector;
			}
			Vector2 offset = partPos - vector;
			return (!buildGrid.Overlap(part, vector)) ? vector : (GetNonOverlapPos(vector, offset, part) + vector);
		}

		private Vector2 GetNonOverlapPos(Vector2 partPosRounded, Vector2 offset, PlacedPart part)
		{
			if (offset.y > 0f && TryCorrect(partPosRounded, new Vector2(0f, 0.5f), part))
			{
				return new Vector2(0f, 0.5f);
			}
			if (offset.y < 0f && TryCorrect(partPosRounded, new Vector2(0f, -0.5f), part))
			{
				return new Vector2(0f, -0.5f);
			}
			if (offset.x > 0f && TryCorrect(partPosRounded, new Vector2(0.5f, 0f), part))
			{
				return new Vector2(0.5f, 0f);
			}
			if (offset.x < 0f && TryCorrect(partPosRounded, new Vector2(-0.5f, 0f), part))
			{
				return new Vector2(-0.5f, 0f);
			}
			if ((double)offset.y > -0.35 && TryCorrect(partPosRounded, new Vector2(0f, 0.5f), part))
			{
				return new Vector2(0f, 0.5f);
			}
			if ((double)offset.y < 0.35 && TryCorrect(partPosRounded, new Vector2(0f, -0.5f), part))
			{
				return new Vector2(0f, -0.5f);
			}
			if ((double)offset.x > -0.35 && TryCorrect(partPosRounded, new Vector2(0.5f, 0f), part))
			{
				return new Vector2(0.5f, 0f);
			}
			if ((double)offset.x < 0.35 && TryCorrect(partPosRounded, new Vector2(-0.5f, 0f), part))
			{
				return new Vector2(-0.5f, 0f);
			}
			return Vector2.zero;
		}

		private Vector2 GetMagnetSnapPos(Vector2 partPos, PlacedPart part, ref bool hasSnap)
		{
			Vector2 vector = Vector2.zero;
			float num = snapDistance * snapDistance;
			PartData.SwitchPoint[] switchPoints = part.partData.switchPoints;
			foreach (PartData.SwitchPoint switchPoint in switchPoints)
			{
				if (!switchPoint.magnet)
				{
					continue;
				}
				Vector2 vector2 = partPos + switchPoint.position * part.orientation;
				foreach (PlacedPart part2 in buildGrid.parts)
				{
					PartData.SwitchPoint[] switchPoints2 = part2.partData.switchPoints;
					foreach (PartData.SwitchPoint switchPoint2 in switchPoints2)
					{
						if (switchPoint2.magnet && !(switchPoint.type != switchPoint2.type))
						{
							Vector2 vector3 = part2.position + switchPoint2.position * part2.orientation;
							Vector2 vector4 = vector2 - vector3;
							float sqrMagnitude = vector4.sqrMagnitude;
							if (!(sqrMagnitude > num) && !buildGrid.Overlap(part, partPos - vector4))
							{
								num = sqrMagnitude;
								vector = vector4;
							}
						}
					}
				}
			}
			hasSnap = vector.sqrMagnitude > 0f;
			return partPos - vector;
		}

		private float ConnectedSurface(PlacedPart partA, Vector2 position, int fingerId)
		{
			partA.position = position;
			float num = 0f;
			PartData.AttachmentSurface[] surfaces = partA.appliedData.surfaces;
			foreach (PartData.AttachmentSurface surfaceA in surfaces)
			{
				foreach (PlacedPart part in buildGrid.parts)
				{
					PartData.AttachmentSurface[] surfaces2 = part.appliedData.surfaces;
					foreach (PartData.AttachmentSurface surfaceB in surfaces2)
					{
						num += Mathf.Max(0f, PlacedPart.SurfacesConnect(partA, surfaceA, part, surfaceB));
					}
				}
			}
			return num;
		}

		private bool TryCorrect(Vector2 original, Vector2 change, PlacedPart part)
		{
			return !buildGrid.Overlap(part, original + change);
		}

		private float GetSnapStrength(Vector2 partPos, PlacedPart part, Vector2 deltaPixel)
		{
			return (!pickGrid.IsInsideDropArea(Utility.ToDepth(partPos + part.partData.centerOfRotation * part.orientation, pickMenuDistance))) ? Mathf.Clamp01(snapStrength - deltaPixel.magnitude / (float)Screen.height * smoothIncrease + smoothDecay * Time.deltaTime) : 0f;
		}

		public void Rotate90()
		{
			bool flag = false;
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]))
				{
					holdingParts[i].Rotate90(snapStrength);
					flag = true;
				}
			}
			if (!flag)
			{
				pickGrid.orientation.Rotate90();
				pickGrid.LoadIcons();
			}
		}

		public void FlipX()
		{
			bool flag = false;
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]))
				{
					holdingParts[i].FlipX(snapStrength);
					flag = true;
				}
			}
			if (!flag)
			{
				pickGrid.orientation.FlipX();
				pickGrid.LoadIcons();
			}
		}

		public void FlipY()
		{
			bool flag = false;
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]))
				{
					holdingParts[i].FlipY(snapStrength);
					flag = true;
				}
			}
			if (!flag)
			{
				pickGrid.orientation.FlipY();
				pickGrid.LoadIcons();
			}
		}

		public List<Skin> GetSkins()
		{
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]))
				{
					if (holdingParts[i].part.partData.skins == null || holdingParts[i].part.partData.skins.skins == null)
					{
						return new List<Skin>();
					}
					List<Skin> list = new List<Skin>(holdingParts[i].part.partData.skins.skins);
					if (list.Count > 0)
					{
						list.RemoveAt(holdingParts[i].part.skinId);
					}
					return list;
				}
			}
			return new List<Skin>();
		}

		public void NextSkin()
		{
			bool flag = false;
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]))
				{
					holdingParts[i].NextSkin();
					flag = true;
				}
			}
			skinIcon.sprite = GetSkinIcon();
			if (!flag)
			{
				MenuManager.ShowMsg("Hold a part to change its skin");
			}
		}

		private Sprite GetSkinIcon()
		{
			for (int i = 0; i < holdingParts.Length; i++)
			{
				if (!HoldingPart.isNull(holdingParts[i]) && holdingParts[i].part.partData.skins != null && holdingParts[i].part.partData.skins != null && holdingParts[i].part.partData.skins.skins.Length > 0)
				{
					return holdingParts[i].part.partData.skins.skins[holdingParts[i].part.skinId].icon;
				}
			}
			return null;
		}

		public void TryTakePart(int fingerId, Vector2 touchPosWorld)
		{
			if (IsHolding(fingerId))
			{
				UnityEngine.Object.Destroy(holdingParts[fingerId].part.partIcon.gameObject);
			}
			if (HoldingPart.isNull(holdingParts[fingerId]))
			{
				holdingParts[fingerId] = pickGrid.TryTakePart(Utility.ToDepth(touchPosWorld, pickMenuDistance), touchPosWorld);
			}
			if (HoldingPart.isNull(holdingParts[fingerId]))
			{
				holdingParts[fingerId] = buildGrid.TryTakePart(touchPosWorld, true);
			}
			if (!HoldingPart.isNull(holdingParts[fingerId]))
			{
				LoadHoldingIcon(fingerId, touchPosWorld);
			}
		}

		private void LoadHoldingIcon(int fingerId, Vector2 touchPosWorld)
		{
			PlacedPart part = holdingParts[fingerId].part;
			LoadPartDescription(part.partData);
			part.partIcon = PartGrid.LoadIcon(part.partData, part.orientation, part.skinId, touchPosWorld, Vector3.one, null, 100, Color.white, false);
			List<string> keys = Utility.GetKeys(holdingParts[fingerId].part.partData, Utility.Location.held, part.orientation, new bool[0], new bool[0]);
			Utility.ApplySwitchesToIcon(part.partIcon, part.partData, keys);
			holdingParts[fingerId].FlipX(0f);
			holdingParts[fingerId].FlipX(0f);
		}

		private bool IsHolding(int fingerId)
		{
			return !HoldingPart.isNull(holdingParts[fingerId]) && holdingParts[fingerId].part.partIcon != null;
		}

		public void LoadSave(Blueprint buildSaveToLoad)
		{
			buildGrid.LoadBuild(Blueprint.PlacedPartSave.FromSave(buildSaveToLoad.parts), new List<PlacedPart>());
			Camera.main.transform.position = buildSaveToLoad.cameraPosition;
			MoveCamera(Vector3.zero);
		}

		public void ExitBuildScene()
		{
			Invoke("AskExit", 0.1f / Time.timeScale);
		}

		private void AskExit()
		{
			if (buildGrid.parts.Count == 0)
			{
				Exit();
				return;
			}
			string warning = Utility.SplitLines("确定要退出吗?//未保存的建造进度将丢失");
			MenuManager.ShowWarning(warning, new Vector2(1200f, 265f), "退出", Exit, "取消", null, 160);
		}

		private void Exit()
		{
			if (Ref.lastScene == Ref.SceneType.Game && World.GetWorldByName().LoadPersistant().mainVesselId != -1)
			{
				Ref.LoadScene(Ref.SceneType.Game);
				Ref.lastScene = Ref.SceneType.MainMenu;
			}
			else
			{
				Ref.LoadScene(Ref.SceneType.MainMenu);
			}
		}

		public void Launch()
		{
			Invoke("TryLaunch", 0.1f / Time.timeScale);
		}

		private void TryLaunch()
		{
			if (buildGrid.HasAnyParts())
			{
				if (!buildGrid.HasControlAuthority())
				{
					DisableUI();
					string warning = Utility.SplitLines("火箭没有指挥舱,无法控制");
					MenuManager.ShowWarning(warning, new Vector2(1200f, 175f), "继续发射", GoForLaunch, "取消发射", EnableUI, 275);
				}
				else if (buildGrid.GetTWR() < 1f)
				{
					DisableUI();
					string warning2 = Utility.SplitLines("引擎动力不足,无法举起火箭//请使用更强的发动机,或减轻火箭质量");
					MenuManager.ShowWarning(warning2, new Vector2(1220f, 395f), "继续发射", GoForLaunch, "取消发射", EnableUI, 275);
				}
				else if (!buildGrid.HasParachute())
				{
					DisableUI();
					string warning3 = Utility.SplitLines("你的火箭没有降落伞");
					MenuManager.ShowWarning(warning3, new Vector2(1050f, 95f), "继续发射", GoForLaunch, "取消发射", EnableUI, 275);
				}
				else
				{
					GoForLaunch();
				}
			}
		}

		private void GoForLaunch()
		{
			string jsonData = JsonUtility.ToJson(new Blueprint(Ref.main.Camera.transform.position, buildGrid.parts, GetOrientation()));
			Ref.SaveJsonString(jsonData, Saving.SaveKey.ToLaunch);
			Ref.LoadScene(Ref.SceneType.Game);
		}

		public string GetBlueprintJson()
		{
			return JsonUtility.ToJson(GetBlueprint());
		}

		public Blueprint GetBlueprint()
		{
			return new Blueprint(Ref.main.Camera.transform.position, buildGrid.parts, GetOrientation());
		}

		public static Texture2D CreatePreviewIcon(string jsonData)
		{
			Blueprint blueprint = JsonUtility.FromJson<Blueprint>(jsonData);
			Vector2 size = Vector2.zero;
			PartGrid.CenterToPositon(blueprint.parts, ref size, new Vector2(0f, -200f));
			List<GameObject> list = new List<GameObject>();
			foreach (Blueprint.PlacedPartSave part in blueprint.parts)
			{
				PartData partData = part.partName.PartData();
				int skinId = 0;
				if (partData != null)
				{
					list.Add(PartGrid.LoadIcon(partData, part.orientation, skinId, part.position, partData.prefab.localScale, null, 0, Color.white, true).gameObject);
				}
			}
			main.previewIconCamera.transform.position = new Vector3(0f, -200f, Mathf.Clamp(0f - Mathf.Max(size.x, size.y), -100f, -1f) * 0.52f);
			main.previewIconCamera.Render();
			while (list.Count > 0)
			{
				UnityEngine.Object.DestroyImmediate(list[0]);
				list.RemoveAt(0);
			}
			Texture2D texture2D = new Texture2D(main.previewIconCamera.targetTexture.width, main.previewIconCamera.targetTexture.height, TextureFormat.RGB24, false);
			RenderTexture.active = main.previewIconCamera.targetTexture;
			texture2D.ReadPixels(new Rect(0f, 0f, main.previewIconCamera.targetTexture.width, main.previewIconCamera.targetTexture.height), 0, 0);
			texture2D.Apply();
			return texture2D;
		}

		public void LoadPartDescription(PartData partData)
		{
			partName.text = partData.displayName;
			partDescription.text = Utility.SplitLines(partData.GetDescriptionRaw());
			descriptionBackground.sizeDelta = new Vector2(445f, Utility.GetLinesCount(partData.GetDescriptionRaw()) * 32 + 55);
		}

		public void DisableUI()
		{
		}

		public void EnableUI()
		{
		}

		public void UpdateStats()
		{
			float mass = buildGrid.GetMass();
			stats.text = "推力/重量:" + Mathf.Round(buildGrid.GetThrust() / 9.8f / mass * 100f) / 100f + "质量:" + Mathf.Round(mass * 2f * 10f) / 10f + "t";
		}
	}
}
