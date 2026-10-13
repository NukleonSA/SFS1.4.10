using System;
using System.Collections.Generic;
using NewBuildSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Part : MonoBehaviour
{
	[Serializable]
	public class Joint
	{
		[Serializable]
		public class Save
		{
			public int fromPartId;

			public int toPartId;

			public Vector2 anchor;

			public int fromSurfaceIndex;

			public int toSurfaceIndex;

			public bool fuelFlow;

			public Save(int fromPartId, int toPartId, Vector2 anchor, int fromSurfaceIndex, int toSurfaceIndex, bool fuelFlow)
			{
				this.fromPartId = fromPartId;
				this.toPartId = toPartId;
				this.anchor = anchor;
				this.fromSurfaceIndex = fromSurfaceIndex;
				this.toSurfaceIndex = toSurfaceIndex;
				this.fuelFlow = fuelFlow;
			}

			public static int GetToPartIndex(Joint joint, List<Part> parts)
			{
				for (int i = 0; i < parts.Count; i++)
				{
					if (parts[i] == joint.toPart)
					{
						return i;
					}
				}
				return -1;
			}
		}

		public Part fromPart;

		public Part toPart;

		public int fromSurfaceIndex;

		public int toSurfaceIndex;

		public Vector2 anchor;

		public float coveredAmount;

		public bool resourceFlow;

		public Joint(Vector2 anchor, Part fromPart, Part toPart, int fromSurfaceIndex, int toSurfaceIndex, bool resourceFlow)
		{
			this.fromPart = fromPart;
			this.toPart = toPart;
			this.fromSurfaceIndex = fromSurfaceIndex;
			this.toSurfaceIndex = toSurfaceIndex;
			this.anchor = anchor;
			this.resourceFlow = resourceFlow;
			if (fromPart.partData.attachmentSurfaces.Length > fromSurfaceIndex && toPart.partData.attachmentSurfaces.Length > toSurfaceIndex)
			{
				coveredAmount = SurfacesConnect(anchor, fromPart, fromPart.partData.attachmentSurfaces[fromSurfaceIndex], toPart, toPart.partData.attachmentSurfaces[toSurfaceIndex]);
			}
			else
			{
				MonoBehaviour.print("from " + fromSurfaceIndex + "零件名:" + fromPart.partData.name);
				MonoBehaviour.print("to " + toSurfaceIndex + "零件名:" + fromPart.partData.name);
			}
			fromPart.joints.Add(this);
			toPart.joints.Add(this);
		}

		public static float SurfacesConnect(Vector2 anchor, Part partA, PartData.AttachmentSurface surfaceA, Part partB, PartData.AttachmentSurface surfaceB)
		{
			bool flag = Mathf.Abs((surfaceA.size * partA.orientation).x) > 0.05f;
			bool flag2 = Mathf.Abs((surfaceB.size * partB.orientation).x) > 0.05f;
			if (flag != flag2)
			{
				return 0f;
			}
			Vector2 vector = surfaceA.start * partA.orientation;
			Vector2 vector2 = anchor + surfaceB.start * partB.orientation;
			if ((!flag) ? (Mathf.Abs(vector.x - vector2.x) > 0.05f) : (Mathf.Abs(vector.y - vector2.y) > 0.05f))
			{
				return 0f;
			}
			Vector2 vector3 = (surfaceA.start + surfaceA.size) * partA.orientation;
			Vector2 vector4 = anchor + (surfaceB.start + surfaceB.size) * partB.orientation;
			return (!flag) ? Utility.Overlap(vector.y, vector3.y, vector2.y, vector4.y) : Utility.Overlap(vector.x, vector3.x, vector2.x, vector4.x);
		}

		public Vector2 RelativeAnchor(Part part)
		{
			return (!(fromPart == part)) ? (-anchor) : anchor;
		}
	}

	[Serializable]
	public class ActiveDragSurafce
	{
		public bool point;

		[HideIf("point", true)]
		public float angleRad;

		public float size;

		public ActiveDragSurafce(PartData.DragSurface dragSurface, Orientation orientation, float coveredArea)
		{
			point = dragSurface.point;
			size = dragSurface.size - coveredArea;
			angleRad = ((!point) ? (dragSurface.angleRad * orientation) : float.NaN);
		}
	}

	[Serializable]
	public class Save
	{
		[Header("Part")]
		public string partName;

		public Orientation orientation;

		public int skinId;

		public Module.Save[] moduleSaves;

		public List<string> interactionKeys;

		public Save(Part part, List<Part> parts)
		{
			partName = part.partData.name;
			orientation = part.orientation;
			skinId = part.skinId;
			moduleSaves = new Module.Save[part.modules.Length];
			for (int i = 0; i < part.modules.Length; i++)
			{
				moduleSaves[i] = new Module.Save(part.modules[i].GetType().ToString(), part.modules[i].SaveVariables);
			}
			interactionKeys = part.interactionKeys;
		}
	}

	public Vessel vessel;

	[HideInInspector]
	public int connectionCheckId;

	[FoldoutGroup("Part Data", 0)]
	[ReadOnly]
	public PartData partData;

	[FoldoutGroup("Part Data", 0)]
	[InlineProperty]
	public float mass;

	[FoldoutGroup("Part Data", 0)]
	public Vector3 centerOfMass;

	[FoldoutGroup("Part Data", 0)]
	public Vector2 centerOfDrag;

	[FoldoutGroup("Part Data", 0)]
	public Orientation orientation;

	[FoldoutGroup("Part Data", 0)]
	public int skinId;

	[FoldoutGroup("Joints and Active Drag Surface", 0)]
	[TableList]
	public List<Joint> joints;

	[FoldoutGroup("Joints and Active Drag Surface", 0)]
	[TableList]
	public List<ActiveDragSurafce> activeDragSurfaces;

	public List<string> interactionKeys = new List<string>();

	[HideInInspector]
	public Module[] modules;

	public PlacedPart.AppliedPartData appliedData;

	public bool useEvent;

	[ShowIf("useEvent", true)]
	public UnityEvent onPartUsed;

	private void Awake()
	{
		modules = GetComponents<Module>();
	}

	private void OnValidate()
	{
		modules = GetComponents<Module>();
	}

	public void SetMass(float newMass)
	{
		mass = newMass;
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (vessel.partsManager.partToPartDamage && !(collision.relativeVelocity.sqrMagnitude < 30f))
		{
			DestroyPart(true, false);
		}
	}

	public virtual void UsePart()
	{
		if (useEvent)
		{
			onPartUsed.Invoke();
		}
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].OnPartUsed();
		}
	}

	public void DestroyPart(bool createExplosion, bool disablePartToPartDamage)
	{
		while (joints.Count > 0)
		{
			// 【性能优化】循环内跳过"全船重算分组": 下面第 251 行在循环结束后
			// 已经完整重算过一次, 中间这些重算对最终状态没有任何影响。
			// 原版每个关节都重算一次 => 多零件同时爆炸时关节数×全船遍历 => 卡顿。
			DestroyJoint(joints[0], disablePartToPartDamage, false);
		}
		if (vessel != null)
		{
			vessel.partsManager.parts.Remove(this);
		}
		base.gameObject.SetActive(false);
		if (vessel != null)
		{
			vessel.partsManager.UpdatePartsGruping(disablePartToPartDamage, vessel);
		}
		if (createExplosion && ++Ref.explosionCount < 5)
		{
			Transform transform = UnityEngine.Object.Instantiate(Ref.controller.explosionParticle, base.transform.TransformPoint(centerOfMass), Quaternion.identity);
			transform.localScale = Vector3.one * (mass * 1.2f + 0.5f);
			UnityEngine.Object.Destroy(transform.gameObject, 3f);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void GetDrag(float velocityAngle, float rot, ref float sumedDrag, ref Vector2 sumedCenterOfDrag)
	{
		float num = 0f;
		for (int i = 0; i < activeDragSurfaces.Count; i++)
		{
			if (activeDragSurfaces[i].point)
			{
				num += activeDragSurfaces[i].size;
				continue;
			}
			float num2 = Mathf.Cos(activeDragSurfaces[i].angleRad + rot - velocityAngle);
			if (num2 > 0f)
			{
				num += num2 * num2 * activeDragSurfaces[i].size;
			}
		}
		sumedDrag += num * partData.dragMultiplier;
		sumedCenterOfDrag.x += (base.transform.localPosition.x + centerOfDrag.x) * num;
		sumedCenterOfDrag.y += (base.transform.localPosition.y + centerOfDrag.y) * num;
	}

	public static void DestroyJoint(Joint jointToDestroy, bool disablePartToPartDamage)
	{
		DestroyJoint(jointToDestroy, disablePartToPartDamage, true);
	}

	// regroup=false 时跳过"全船重算分组"(调用方会在合适时机统一重算一次)
	// 去掉的是冗余重复计算, 最终连接分组结果与原来完全相同。
	public static void DestroyJoint(Joint jointToDestroy, bool disablePartToPartDamage, bool regroup)
	{
		jointToDestroy.fromPart.joints.Remove(jointToDestroy);
		jointToDestroy.toPart.joints.Remove(jointToDestroy);
		jointToDestroy.fromPart.UpdateDragSurfaces();
		jointToDestroy.toPart.UpdateDragSurfaces();
		if (regroup)
		{
			jointToDestroy.fromPart.vessel.partsManager.UpdatePartsGruping(disablePartToPartDamage, jointToDestroy.fromPart.vessel);
		}
	}

	public void UpdateDragSurfaces()
	{
		float[] surfacesFloat = GetSurfacesFloat();
		UpdateDragSurfaces(surfacesFloat);
	}

	public void CreateKeys()
	{
		interactionKeys = Utility.GetKeys(partData, Utility.Location.game, orientation, GetSurfacesBool(), GetSwitchPoints(this, vessel.partsManager.parts));
	}

	public void UpdateIcon()
	{
		Utility.ApplySwitchesToIcon(base.transform, partData, interactionKeys);
	}

	public void UpdateData()
	{
		appliedData = new PlacedPart.AppliedPartData(partData, interactionKeys);
	}

	public bool[] GetSurfacesBool()
	{
		float[] surfacesFloat = GetSurfacesFloat();
		bool[] array = new bool[surfacesFloat.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = surfacesFloat[i] > 0.05f;
		}
		return array;
	}

	public float[] GetSurfacesFloat()
	{
		float[] array = new float[partData.attachmentSurfaces.Length];
		foreach (Joint joint in joints)
		{
			int num = ((!(joint.fromPart == this)) ? joint.toSurfaceIndex : joint.fromSurfaceIndex);
			if (array.Length > num)
			{
				array[num] += joint.coveredAmount;
			}
			else
			{
				Debug.Log("连接点超出范围:" + partData.name);
			}
		}
		return array;
	}

	public static bool[] GetSwitchPoints(Part part, List<Part> parts)
	{
		bool[] array = new bool[part.partData.switchPoints.Length];
		for (int i = 0; i < part.partData.switchPoints.Length; i++)
		{
			Vector2 posA = (Vector2)part.transform.localPosition + part.partData.switchPoints[i].position * part.orientation;
			foreach (Part part2 in parts)
			{
				if (part2.partData != null && part2 != part)
				{
					PartData.SwitchPoint[] switchPoints = part2.partData.switchPoints;
					foreach (PartData.SwitchPoint switchPointB in switchPoints)
					{
						array[i] = array[i] || PointsConnect(posA, part.partData.switchPoints[i], part2, switchPointB);
					}
				}
			}
		}
		return array;
	}

	public static bool PointsConnect(Vector2 posA, PartData.SwitchPoint switchPointA, Part partB, PartData.SwitchPoint switchPointB)
	{
		return switchPointB.trigger && switchPointA.type == switchPointB.type && Vector2.SqrMagnitude(posA - ((Vector2)partB.transform.localPosition + switchPointB.position * partB.orientation)) < 0.010000001f;
	}

	public void UpdateDragSurfaces(float[] surfaceCoverAmount)
	{
		activeDragSurfaces.Clear();
		for (int i = 0; i < partData.dragSurfaces.Length; i++)
		{
			if (partData.dragSurfaces[i].surfaceId != -1)
			{
				if (partData.dragSurfaces[i].surfaceId < surfaceCoverAmount.Length && partData.dragSurfaces[i].size > surfaceCoverAmount[partData.dragSurfaces[i].surfaceId])
				{
					activeDragSurfaces.Add(new ActiveDragSurafce(partData.dragSurfaces[i], orientation, surfaceCoverAmount[partData.dragSurfaces[i].surfaceId]));
				}
			}
			else
			{
				activeDragSurfaces.Add(new ActiveDragSurafce(partData.dragSurfaces[i], orientation, 0f));
			}
		}
	}

	public bool HasEngineModule()
	{
		for (int i = 0; i < modules.Length; i++)
		{
			if (modules[i] is EngineModule)
			{
				return true;
			}
		}
		return false;
	}
	public bool HasDockingPortModule()
	{
		for (int i = 0; i < modules.Length; i++)
		{
			if (modules[i] is DockingPortModule)
			{
				return true;
			}
		}
		return false;
	}

	public Vector2 GetPartPosition(Rigidbody2D rb2d)
	{
		return GetPartPosition(rb2d.worldCenterOfMass, rb2d.centerOfMass, rb2d.rotation);
	}

	public Vector2 GetPartPosition(Vector2 worldCenterOfMass, Vector2 localCenterOfMass, float rotation)
	{
		rotation *= (float)Math.PI / 180f;
		float num = Mathf.Cos(rotation);
		float num2 = Mathf.Sin(rotation);
		Vector2 vector = (Vector2)base.transform.localPosition - localCenterOfMass;
		return worldCenterOfMass + new Vector2(vector.x * num - vector.y * num2, vector.x * num2 + vector.y * num);
	}

	public static int GetPartListIndex(Part part, List<Part> parts)
	{
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i] == part)
			{
				return i;
			}
		}
		return -1;
	}

	public ResourceModule GetResourceModule()
	{
		for (int i = 0; i < modules.Length; i++)
		{
			if (modules[i] is ResourceModule)
			{
				return modules[i] as ResourceModule;
			}
		}
		return null;
	}
}
