using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace NewBuildSystem
{
	public class PartGrid : MonoBehaviour
	{
		[Serializable]
		public class UndoStep
		{
			public List<PlacedPart> parts;

			public List<PlacedPart> innactiveParts;

			public UndoStep(List<PlacedPart> newParts, List<PlacedPart> newInnactiveParts)
			{
				parts = new List<PlacedPart>();
				for (int i = 0; i < newParts.Count; i++)
				{
					parts.Add(new PlacedPart(newParts[i], newParts[i].selected, new List<string>(), true));
				}
				innactiveParts = new List<PlacedPart>();
				for (int j = 0; j < newInnactiveParts.Count; j++)
				{
					innactiveParts.Add(new PlacedPart(newInnactiveParts[j], false, new List<string>(), true));
				}
			}
		}

		public Utility.Vector2Specifier gridSize;

		[BoxGroup]
		public SpriteRenderer gridSprite1;

		[BoxGroup]
		public SpriteRenderer gridSprite2;

		[BoxGroup]
		public Transform topShade;

		[BoxGroup]
		public Transform rightShade;

		[BoxGroup]
		public Transform centerMarker;

		public List<PlacedPart> parts;

		public List<PlacedPart> inactiveParts;

		public List<PlacedPart> changed;

		[BoxGroup("Undo", true, false, 0)]
		public Image undoIcon;

		[BoxGroup("Undo", true, false, 0)]
		public List<UndoStep> undoSteps;

		private void LateUpdate()
		{
			ProcessQueue();
		}

		private void Start()
		{
			SetBuildGridSize();
		}

		private void AddPart(PlacedPart partToAdd, bool active)
		{
			partToAdd.partIcon = LoadIcon(partToAdd.partData, partToAdd.orientation, partToAdd.skinId, partToAdd.position, partToAdd.partData.prefab.localScale, base.transform, (!active) ? (-50) : 0, Color.white, true);
			if (active)
			{
				parts.Add(partToAdd);
				AddToQueue(partToAdd);
				partToAdd.UpdateInteractionKeys(parts, Utility.Location.build);
				partToAdd.UpdateData();
				for (int i = 0; i < parts.Count; i++)
				{
					if (parts[i] != partToAdd && PlacedPart.IsOverplaing(partToAdd, parts[i]))
					{
						Debug.Log("disabled " + parts[i].partData.name);
						MoveToInactive(parts[i]);
						i--;
					}
				}
			}
			else
			{
				inactiveParts.Add(partToAdd);
			}
		}

		private void DisableBelow(PlacedPart part)
		{
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i] != part && PlacedPart.IsOverplaing(part, parts[i]))
				{
					MoveToInactive(parts[i]);
					i--;
				}
			}
		}

		private void RemovePart(PlacedPart partToRemove)
		{
			if (partToRemove.partIcon != null)
			{
				UnityEngine.Object.Destroy(partToRemove.partIcon.gameObject);
			}
			bool flag = parts.Remove(partToRemove);
			bool flag2 = inactiveParts.Remove(partToRemove);
			if (flag)
			{
				AddToQueue(partToRemove);
			}
		}

		private void MoveToInactive(PlacedPart partToMove)
		{
			RemovePart(partToMove);
			AddPart(partToMove, false);
		}

		private void MoveToActive(PlacedPart partToMove)
		{
			RemovePart(partToMove);
			AddPart(partToMove, true);
		}

		private bool CanInteract(PlacedPart A, PlacedPart B)
		{
			PartData.AttachmentSurface[] attachmentSurfaces = A.partData.attachmentSurfaces;
			foreach (PartData.AttachmentSurface surfaceA in attachmentSurfaces)
			{
				PartData.AttachmentSurface[] attachmentSurfaces2 = B.partData.attachmentSurfaces;
				foreach (PartData.AttachmentSurface surfaceB in attachmentSurfaces2)
				{
					if (PlacedPart.SurfacesConnect(A, surfaceA, B, surfaceB) > 0f)
					{
						return true;
					}
				}
			}
			PartData.SwitchPoint[] switchPoints = A.partData.switchPoints;
			foreach (PartData.SwitchPoint switchPointB in switchPoints)
			{
				PartData.SwitchPoint[] switchPoints2 = B.partData.switchPoints;
				foreach (PartData.SwitchPoint switchPointA in switchPoints2)
				{
					if (PlacedPart.PointsConnect(B, switchPointA, A, switchPointB))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void AddToQueue(PlacedPart part)
		{
			if (!changed.Contains(part))
			{
				changed.Add(part);
			}
		}

		private List<PlacedPart> GetAffected(PlacedPart part)
		{
			List<PlacedPart> list = new List<PlacedPart>();
			foreach (PlacedPart part2 in parts)
			{
				if (part2 != part && CanInteract(part, part2))
				{
					list.Add(part2);
				}
			}
			return list;
		}

		private void ProcessQueue()
		{
			if (changed.Count == 0)
			{
				return;
			}
			List<PlacedPart> list = new List<PlacedPart>(changed);
			foreach (PlacedPart item in changed)
			{
				list.AddRange(GetAffected(item));
			}
			List<PlacedPart> list2 = new List<PlacedPart>(list);
			foreach (PlacedPart item2 in list)
			{
				list2.AddRange(GetAffected(item2));
			}
			foreach (PlacedPart item3 in list2)
			{
				if (parts.Contains(item3))
				{
					bool hitboxChanged = false;
					bool surfaceChanged = false;
					item3.UpdateInteractionKeys(parts, Utility.Location.build);
					item3.UpdateData(ref hitboxChanged, ref surfaceChanged);
					item3.UpdateIcon();
					if (hitboxChanged)
					{
						DisableBelow(item3);
					}
				}
			}
			UpdateInactive();
			changed.Clear();
			Build.main.UpdateStats();
		}

		public HoldingPart TryTakePart(Vector2 mousePos, bool remove)
		{
			Vector2 vector = mousePos - (Vector2)base.transform.position;
			PlacedPart placedPart = PointCastParts(vector, parts);
			if (placedPart == null)
			{
				placedPart = PointCastParts(vector, inactiveParts);
			}
			if (placedPart == null)
			{
				return null;
			}
			CreateUndoStep();
			List<PlacedPart> selectedParts = GetSelectedParts();
			if (remove)
			{
				RemovePart(placedPart);
			}
			Ref.inputController.PlayClickSound(0.2f);
			return new HoldingPart(placedPart.position - vector, new PlacedPart(null, placedPart.position, placedPart.orientation.DeepCopy(), false, placedPart.skinId, placedPart.partData, new List<string>()));
		}

		public List<PlacedPart> GetSelectedParts()
		{
			List<PlacedPart> list = new List<PlacedPart>();
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i].selected)
				{
					list.Add(parts[i]);
				}
			}
			return list;
		}

		public void PlacePart(PlacedPart newPart, bool placeActive)
		{
			CreateUndoStep();
			ClampToLimits(newPart);
			Ref.inputController.PlayClickSound(0.6f);
			AddPart(newPart, placeActive);
		}

		public bool Overlap(PlacedPart part, Vector2 position)
		{
			for (int num = parts.Count - 1; num >= 0; num--)
			{
				if (PlacedPart.IsOverplaing(part, position, parts[num]))
				{
					return true;
				}
			}
			return false;
		}

		public void ClampToLimits(PlacedPart part)
		{
			Vector2 vector = Vector2.zero;
			Vector2 vector2 = Vector2.zero;
			PlacedPart.AppliedPartData.HitboxMesh[] hitboxes = part.appliedData.hitboxes;
			foreach (PlacedPart.AppliedPartData.HitboxMesh hitboxMesh in hitboxes)
			{
				Vector2[] vertices = hitboxMesh.vertices;
				foreach (Vector2 vector3 in vertices)
				{
					Vector2 vector4 = part.position + vector3 * part.orientation;
					vector = new Vector2(Mathf.Max(vector.x, vector4.x), Mathf.Max(vector.y, vector4.y));
					vector2 = new Vector2(Mathf.Min(vector2.x, vector4.x), Mathf.Min(vector2.y, vector4.y));
				}
			}
			Vector2 vector5 = new Vector2(Mathf.Min(0f, vector2.x), Mathf.Min(0f, vector2.y)) + new Vector2(Mathf.Max(0f, vector.x - gridSize.Value.x), Mathf.Max(0f, vector.y - gridSize.Value.y));
			part.position = Utility.RoundToHalf(part.position - vector5);
		}

		private void UpdateInactive()
		{
			for (int i = 0; i < inactiveParts.Count; i++)
			{
				if (!Ref.hasPartsExpansion && inactiveParts[i].partData.inFull)
				{
					continue;
				}
				bool flag = false;
				for (int j = 0; j < parts.Count; j++)
				{
					if (PlacedPart.IsOverplaing(inactiveParts[i], parts[j]))
					{
						flag = true;
					}
				}
				if (!flag)
				{
					MoveToActive(inactiveParts[i]);
					i--;
				}
			}
		}

		public static PlacedPart PointCastParts(Vector2 pointPositionLocal, List<PlacedPart> castParts)
		{
			PlacedPart placedPart = null;
			for (int i = 0; i < castParts.Count; i++)
			{
				if (PlacedPart.IsInside(pointPositionLocal, castParts[i], 0f) && (placedPart == null || castParts[i].partData.depth >= placedPart.partData.depth))
				{
					placedPart = castParts[i];
				}
			}
			if (placedPart != null)
			{
				return placedPart;
			}
			for (int j = 0; j < castParts.Count; j++)
			{
				if (PlacedPart.IsInside(pointPositionLocal, castParts[j], 0.25f) && (placedPart == null || castParts[j].partData.depth >= placedPart.partData.depth))
				{
					placedPart = castParts[j];
				}
			}
			return placedPart;
		}

		public void SetBuildGridSize()
		{
			gridSprite1.size = new Vector2(gridSize.Value.x, gridSize.Value.y - 0.1f) * 2f;
			gridSprite2.size = new Vector2(gridSize.Value.x, gridSize.Value.y - 0.1f) / 2f;
			Transform obj = gridSprite1.transform;
			Vector3 position = new Vector2(gridSize.Value.x, gridSize.Value.y - 0.1f) / 2f;
			gridSprite1.transform.position = position;
			obj.position = position;
			topShade.position = new Vector3(0f, gridSize.Value.y - 0.5f, 0f);
			rightShade.position = new Vector3(gridSize.Value.x, 0f, 0f);
			topShade.localScale = new Vector3(gridSize.Value.x, 22f, 1f);
			rightShade.localScale = new Vector3(5f, gridSize.Value.y + 22f - 0.5f, 1f);
			centerMarker.position = new Vector3(gridSize.Value.x / 2f, 0f);
			centerMarker.localScale = new Vector3(1f, gridSize.Value.y - 0.5f);
		}

		public static void CenterParts(List<PlacedPart> parts, Vector2 targetCenter)
		{
			Vector2 max = Vector2.zero;
			Vector2 min = Vector2.zero;
			GetMinMax(parts, ref min, ref max);
			Vector2 vector = (min + max) / 2f - targetCenter;
			for (int i = 0; i < parts.Count; i++)
			{
				parts[i].position -= vector;
			}
		}

		public static void GetMinMax(List<PlacedPart> parts, ref Vector2 min, ref Vector2 max)
		{
			if (parts.Count == 0)
			{
				return;
			}
			min = new Vector2(1000f, 1000f);
			max = new Vector2(-1000f, -1000f);
			foreach (PlacedPart part in parts)
			{
				PlacedPart.AppliedPartData.HitboxMesh[] hitboxes = part.appliedData.hitboxes;
				foreach (PlacedPart.AppliedPartData.HitboxMesh hitboxMesh in hitboxes)
				{
					Vector2[] vertices = hitboxMesh.vertices;
					foreach (Vector2 vector in vertices)
					{
						Vector2 vector2 = part.position + vector * part.orientation;
						max = new Vector2(Mathf.Max(max.x, vector2.x), Mathf.Max(max.y, vector2.y));
						min = new Vector2(Mathf.Min(min.x, vector2.x), Mathf.Min(min.y, vector2.y));
					}
				}
			}
		}

		public static void PositionForLaunch(List<Build.Blueprint.PlacedPartSave> parts, int rotation)
		{
			Vector2 vector = Vector2.zero;
			Vector2 vector2 = Vector2.one * float.PositiveInfinity;
			foreach (Build.Blueprint.PlacedPartSave part in parts)
			{
				PartData partData = part.partName.PartData();
				if (partData == null)
				{
					continue;
				}
				PartData.Hitbox[] hitbox = partData.hitbox;
				foreach (PartData.Hitbox hitbox2 in hitbox)
				{
					Vector2[] vertices = hitbox2.GetVertices();
					foreach (Vector2 vector3 in vertices)
					{
						Vector2 vector4 = part.position + vector3 * part.orientation;
						vector = new Vector2(Mathf.Max(vector.x, vector4.x), Mathf.Max(vector.y, vector4.y));
						vector2 = new Vector2(Mathf.Min(vector2.x, vector4.x), Mathf.Min(vector2.y, vector4.y));
					}
				}
			}
			for (int k = 0; k < parts.Count; k++)
			{
				parts[k].position -= vector2 - new Vector2(Utility.RoundToHalf((vector2.x - vector.x) / 2f), 0f);
			}
		}

		public static void CenterToPositon(List<Build.Blueprint.PlacedPartSave> parts, ref Vector2 size, Vector2 targetCenter)
		{
			Vector2 vector = Vector2.zero;
			Vector2 vector2 = Vector2.one * float.PositiveInfinity;
			foreach (Build.Blueprint.PlacedPartSave part in parts)
			{
				PartData partData = part.partName.PartData();
				if (partData == null)
				{
					continue;
				}
				PartData.Hitbox[] hitbox = partData.hitbox;
				foreach (PartData.Hitbox hitbox2 in hitbox)
				{
					Vector2[] vertices = hitbox2.GetVertices();
					foreach (Vector2 vector3 in vertices)
					{
						Vector2 vector4 = part.position + vector3 * part.orientation;
						vector = new Vector2(Mathf.Max(vector.x, vector4.x), Mathf.Max(vector.y, vector4.y));
						vector2 = new Vector2(Mathf.Min(vector2.x, vector4.x), Mathf.Min(vector2.y, vector4.y));
					}
				}
			}
			Vector2 vector5 = (vector2 + vector) / 2f;
			Vector2 vector6 = targetCenter - vector5;
			for (int k = 0; k < parts.Count; k++)
			{
				parts[k].position += vector6;
			}
			size = vector - vector2;
		}

		public static Transform LoadIcon(PartData partData, Orientation orientation, int skinId, Vector3 localPosition, Vector3 localScale, Transform parent, int sortingOrderAdd, Color color, bool forGrid)
		{
			Transform transform = LoadIconSub(partData.prefab, localPosition, localScale, orientation, parent, sortingOrderAdd, color, forGrid);
			Orientation.ApplyOrientation(transform, orientation);
			Utility.ApplySkin(transform, partData, skinId, orientation);
			return transform;
		}

		public static Transform LoadIconSub(Transform icon, Vector3 localPosition, Vector3 localScale, Orientation orientation, Transform parent, int sortingOrderAdd, Color color, bool forGrid)
		{
			Transform transform = new GameObject().transform;
			transform.parent = parent;
			transform.name = icon.name + " (C)";
			transform.localPosition = localPosition;
			transform.localRotation = icon.localRotation;
			transform.localScale = localScale;
			transform.gameObject.SetActive(icon.gameObject.activeSelf);
			if (icon.GetComponent<SpriteRenderer>() != null)
			{
				SpriteRenderer spriteRenderer = transform.gameObject.AddComponent<SpriteRenderer>();
				spriteRenderer.sprite = icon.GetComponent<SpriteRenderer>().sprite;
				spriteRenderer.sortingOrder = icon.GetComponent<SpriteRenderer>().sortingOrder + sortingOrderAdd;
				spriteRenderer.drawMode = icon.GetComponent<SpriteRenderer>().drawMode;
				spriteRenderer.size = icon.GetComponent<SpriteRenderer>().size;
				spriteRenderer.sharedMaterial = icon.GetComponent<SpriteRenderer>().sharedMaterial;
				Color white = Color.white;
				spriteRenderer.color = icon.GetComponent<SpriteRenderer>().color * color;
			}
			if (icon.GetComponent<SpriteMesh>() != null)
			{
				SpriteMesh component = icon.GetComponent<SpriteMesh>();
				UnityEngine.Object.DestroyImmediate(transform.GetComponent<SpriteRenderer>());
				SpriteMesh spriteMesh = transform.gameObject.AddComponent(typeof(SpriteMesh)) as SpriteMesh;
				spriteMesh.sprite = component.sprite;
				spriteMesh.shade = component.shade;
				spriteMesh.points = component.CopyPoints();
				spriteMesh.color = component.color;
				spriteMesh.SortingLayerName = component.SortingLayerName;
				spriteMesh.SortingOrder = component.SortingOrder + sortingOrderAdd;
				spriteMesh.cutout = component.cutout;
				spriteMesh.cutoutUV = component.cutoutUV;
				spriteMesh.centerOffset = component.centerOffset;
				spriteMesh.borderScale = component.borderScale;
				spriteMesh.drawMode = component.drawMode;
				int num = 180 * orientation;
				if (num < 0)
				{
					num += 360;
				}
				if (num > 360)
				{
					num -= 360;
				}
				spriteMesh.flipX = num <= 45 || num >= 225;
				spriteMesh.Generate();
				transform.GetComponent<MeshRenderer>().sharedMaterial = component.GetComponent<MeshRenderer>().sharedMaterial;
			}
			for (int i = 0; i < icon.childCount; i++)
			{
				LoadIconSub(icon.GetChild(i), icon.GetChild(i).localPosition, icon.GetChild(i).localScale, orientation, transform, sortingOrderAdd, color, forGrid);
			}
			return transform;
		}

		public void LoadBuild(List<PlacedPart> newParts, List<PlacedPart> newInactiveParts)
		{
			DeleteAllIcons();
			Vector2 max = Vector2.zero;
			Vector2 min = Vector2.zero;
			GetMinMax(newParts, ref min, ref max);
			Vector2 vector = new Vector2(Mathf.Max(0f, max.x - gridSize.Value.x), Mathf.Max(0f, max.y - (gridSize.Value.y - 0.5f)));
			Vector2 vector2 = new Vector2(Utility.RoundToHalf(Mathf.Min(min.x, vector.x)), Utility.RoundToHalf(Mathf.Min(min.y, vector.y)));
			for (int i = 0; i < newParts.Count; i++)
			{
				newParts[i].position -= vector2;
			}
			if (!Ref.hasPartsExpansion)
			{
				for (int j = 0; j < newParts.Count; j++)
				{
					if (newParts[j].partData.inFull)
					{
						newInactiveParts.Add(newParts[j]);
						newParts.RemoveAt(j);
						j--;
					}
				}
			}
			parts = newParts;
			inactiveParts = newInactiveParts;
			LoadAllIcons();
			Build.main.UpdateStats();
		}

		public void LoadAllIcons()
		{
			DeleteAllIcons();
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i].partData != null && parts[i].partData.prefab != null)
				{
					parts[i].partIcon = LoadIcon(parts[i].partData, parts[i].orientation, parts[i].skinId, parts[i].position, Vector2.one, base.transform, 0, Color.white, true);
				}
			}
			for (int j = 0; j < inactiveParts.Count; j++)
			{
				if (inactiveParts[j].partData != null && inactiveParts[j].partData.prefab != null)
				{
					inactiveParts[j].partIcon = LoadIcon(inactiveParts[j].partData, inactiveParts[j].orientation, inactiveParts[j].skinId, inactiveParts[j].position, Vector2.one, base.transform, -50, Color.white, true);
				}
			}
			UpdateAllConnections();
		}

		public void UpdateAllConnections()
		{
			foreach (PlacedPart part in parts)
			{
				part.UpdateInteractionKeys(parts, Utility.Location.build);
			}
			foreach (PlacedPart part2 in parts)
			{
				part2.UpdateData();
			}
			foreach (PlacedPart part3 in parts)
			{
				part3.UpdateInteractionKeys(parts, Utility.Location.build);
			}
			foreach (PlacedPart part4 in parts)
			{
				part4.UpdateIcon();
			}
		}

		public void DeleteAllIcons()
		{
			foreach (PlacedPart part in parts)
			{
				if (part.partIcon != null)
				{
					UnityEngine.Object.DestroyImmediate(part.partIcon.gameObject);
				}
			}
			foreach (PlacedPart inactivePart in inactiveParts)
			{
				if (inactivePart.partIcon != null)
				{
					UnityEngine.Object.DestroyImmediate(inactivePart.partIcon.gameObject);
				}
			}
		}

		public void ClearBuildSpace()
		{
			DeleteAllIcons();
			parts.Clear();
			inactiveParts.Clear();
			Build.main.UpdateStats();
		}

		public bool HasAnyParts()
		{
			return parts.Count > 0;
		}

		public bool HasControlAuthority()
		{
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i].partData.prefab.GetComponent<ControlModule>() != null)
				{
					return true;
				}
			}
			return false;
		}

		public bool HasParachute()
		{
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i].partData.prefab.GetComponent<ParachuteModule>() != null)
				{
					return true;
				}
			}
			return false;
		}

		public float GetTWR()
		{
			return GetThrust() / 9.8f / GetMass();
		}

		public float GetThrust()
		{
			float num = 0f;
			foreach (PlacedPart part in parts)
			{
				if (!(part.partData.prefab.GetComponent<EngineModule>() == null) && !Utility.GetByPath(part.partIcon, part.partData.prefab.GetComponent<EngineModule>().interstagePath).gameObject.activeSelf)
				{
					num += part.partData.prefab.GetComponent<EngineModule>().thrust;
				}
			}
			return num;
		}

		public float GetMass()
		{
			float num = 0f;
			for (int i = 0; i < parts.Count; i++)
			{
				num += parts[i].partData.prefab.GetComponent<Part>().mass;
			}
			return num;
		}

		public void CreateUndoStep()
		{
			if (undoSteps.Count >= 15)
			{
				undoSteps.RemoveAt(0);
			}
			undoSteps.Add(new UndoStep(parts, inactiveParts));
			undoIcon.color = new Color(1f, 1f, 1f, 0.9f);
		}

		public void Undo()
		{
			if (undoSteps.Count != 0)
			{
				LoadBuild(undoSteps[undoSteps.Count - 1].parts, undoSteps[undoSteps.Count - 1].innactiveParts);
				undoSteps.RemoveAt(undoSteps.Count - 1);
				undoIcon.color = ((undoSteps.Count != 0) ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.6f));
			}
		}
	}
}
