using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace NewBuildSystem
{
	[Serializable]
	public class PlacedPart
	{
		[Serializable]
		public class AppliedPartData
		{
			[Serializable]
			public class HitboxMesh
			{
				public Vector3[] hitboxPoints;

				public bool hitboxOnly;

				public Vector2[] vertices;

				public int[] indices;

				public HitboxMesh(PartData.Hitbox hitbox)
				{
					hitboxOnly = hitbox.hitboxOnly;
					hitboxPoints = hitbox.hitboxPoints;
					vertices = hitbox.GetVertices();
					indices = hitbox.GetIndices();
				}

				public static HitboxMesh[] ToMesh(List<PartData.Hitbox> hitboxes)
				{
					HitboxMesh[] array = new HitboxMesh[hitboxes.Count];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = new HitboxMesh(hitboxes[i]);
					}
					return array;
				}
			}

			public HitboxMesh[] hitboxes = new HitboxMesh[0];

			public PartData.AttachmentSurface[] surfaces = new PartData.AttachmentSurface[0];

			public AppliedPartData(PartData partData, List<string> keys)
			{
				bool hitboxChange = false;
				bool surfaceChanged = false;
				Update(partData, keys, ref hitboxChange, ref surfaceChanged);
			}

			public AppliedPartData()
			{
			}

			public void Update(PartData partData, List<string> keys, ref bool hitboxChange, ref bool surfaceChanged)
			{
				List<PartData.Hitbox> list = DeepCopy(partData.hitbox);
				List<PartData.AttachmentSurface> list2 = DeepCopy(partData.attachmentSurfaces);
				Dictionary<string, float> variableKeys = Parser.GetVariableKeys(partData, keys);
				PartData.Switch.Output[] outputs = partData.switchData.outputs;
				foreach (PartData.Switch.Output output in outputs)
				{
					float num = Parser.Process(output.code, keys, variableKeys);
					if (output.type == PartData.Switch.Output.Type.Surface)
					{
						if (output.setType == PartData.Switch.Output.MeshSetType.Width)
						{
							if (num <= -100f)
							{
								list2[output.index].size = Vector2.zero;
							}
							else
							{
								if (list2[output.index].size.x != 0f)
								{
									list2[output.index].size.x += num * 2f * Mathf.Sign(list2[output.index].size.x);
									list2[output.index].start.x -= num * Mathf.Sign(list2[output.index].size.x);
								}
								if (list2[output.index].size.y != 0f)
								{
									list2[output.index].size.y += num * 2f * Mathf.Sign(list2[output.index].size.y);
									list2[output.index].start.y -= num * Mathf.Sign(list2[output.index].size.y);
								}
							}
							surfaceChanged = surfaceChanged || (surfaces.Length > output.index && list2[output.index].surfaceLenght != surfaces[output.index].surfaceLenght);
						}
						if (output.setType == PartData.Switch.Output.MeshSetType.PosX)
						{
							list2[output.index].start.x += num;
							surfaceChanged = surfaceChanged || (surfaces.Length > output.index && list2[output.index].start.x != surfaces[output.index].start.x);
						}
						if (output.setType == PartData.Switch.Output.MeshSetType.PosY)
						{
							list2[output.index].start.y += num;
							surfaceChanged = surfaceChanged || (surfaces.Length > output.index && list2[output.index].start.y != surfaces[output.index].start.y);
						}
					}
					if (output.type == PartData.Switch.Output.Type.Hitbox)
					{
						if (output.setType == PartData.Switch.Output.MeshSetType.Width)
						{
							list[output.hitboxIndex].hitboxPoints[output.index].z += num;
							hitboxChange = hitboxChange || (hitboxes.Length > output.hitboxIndex && list[output.hitboxIndex].hitboxPoints[output.index].z != hitboxes[output.hitboxIndex].hitboxPoints[output.index].z);
						}
						if (output.setType == PartData.Switch.Output.MeshSetType.PosX)
						{
							list[output.hitboxIndex].hitboxPoints[output.index].x += num;
							hitboxChange = hitboxChange || (hitboxes.Length > output.hitboxIndex && list[output.hitboxIndex].hitboxPoints[output.index].x != hitboxes[output.hitboxIndex].hitboxPoints[output.index].x);
						}
						if (output.setType == PartData.Switch.Output.MeshSetType.PosY)
						{
							list[output.hitboxIndex].hitboxPoints[output.index].y += num;
							hitboxChange = hitboxChange || (hitboxes.Length > output.hitboxIndex && list[output.hitboxIndex].hitboxPoints[output.index].y != hitboxes[output.hitboxIndex].hitboxPoints[output.index].y);
						}
					}
				}
				if (hitboxChange)
				{
					Debug.Log("Hitbox Changed!");
				}
				hitboxes = HitboxMesh.ToMesh(list);
				surfaces = list2.ToArray();
			}

			public List<PartData.Hitbox> DeepCopy(PartData.Hitbox[] a)
			{
				List<PartData.Hitbox> list = new List<PartData.Hitbox>();
				for (int i = 0; i < a.Length; i++)
				{
					list.Add(new PartData.Hitbox());
					list[i].hitboxOnly = a[i].hitboxOnly;
					list[i].hitboxPoints = new Vector3[a[i].hitboxPoints.Length];
					for (int j = 0; j < a[i].hitboxPoints.Length; j++)
					{
						list[i].hitboxPoints[j] = a[i].hitboxPoints[j];
					}
				}
				return list;
			}

			public List<PartData.AttachmentSurface> DeepCopy(PartData.AttachmentSurface[] a)
			{
				List<PartData.AttachmentSurface> list = new List<PartData.AttachmentSurface>();
				for (int i = 0; i < a.Length; i++)
				{
					list.Add(new PartData.AttachmentSurface
					{
						start = a[i].start,
						size = a[i].size,
						fuelFlow = a[i].fuelFlow
					});
				}
				return list;
			}
		}

		public Transform partIcon;

		[DrawWithUnity]
		[BoxGroup]
		public Vector2 position;

		[BoxGroup]
		[Space]
		public Orientation orientation;

		public bool selected;

		public int skinId;

		public PartData partData;

		public List<string> interactionKeys;

		public AppliedPartData appliedData;

		public PlacedPart(Transform partIcon, Vector2 position, Orientation orientation, bool selected, int skinId, PartData partData, List<string> keys)
		{
			this.position = position;
			this.orientation = orientation;
			Orientation orientation2 = this.orientation;
			if (orientation2 != null)
			{
				if (orientation2.x == 0)
				{
					orientation2.x = 1;
				}
				if (orientation2.y == 0)
				{
					orientation2.y = 1;
				}
			}
			this.selected = selected;
			this.skinId = skinId;
			this.partData = partData;
			appliedData = new AppliedPartData(partData, keys);
		}

		public PlacedPart(PlacedPart a, bool selected, List<string> keys, bool forUndo)
		{
			position = a.position;
			orientation = a.orientation.DeepCopy();
			this.selected = selected;
			skinId = a.skinId;
			partData = a.partData;
			appliedData = ((!forUndo) ? new AppliedPartData(partData, keys) : new AppliedPartData());
		}

		public void NextSkin()
		{
			if (partData.skins.skins.Length != 0)
			{
				skinId = (skinId + 1) % partData.skins.skins.Length;
				Utility.ApplySkin(partIcon, partData, skinId, orientation);
			}
		}

		public Vector2 GetWorldPosition(Vector2 pos)
		{
			return position + pos * orientation;
		}

		public static bool IsInside(Vector2 a, PlacedPart b, float edge)
		{
			AppliedPartData.HitboxMesh[] hitboxes = b.appliedData.hitboxes;
			foreach (AppliedPartData.HitboxMesh hitboxMesh in hitboxes)
			{
				for (int j = 0; j < hitboxMesh.indices.Length; j += 3)
				{
					if (Triangulator.InsideTriangle(a, b.position + hitboxMesh.vertices[hitboxMesh.indices[j]] * b.orientation, b.position + hitboxMesh.vertices[hitboxMesh.indices[j + 1]] * b.orientation, b.position + hitboxMesh.vertices[hitboxMesh.indices[j + 2]] * b.orientation, edge))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool IsOverplaing(PlacedPart a, PlacedPart b)
		{
			return IsOverplaing(a, a.position, b);
		}

		public static bool IsOverplaing(PlacedPart a, Vector2 position, PlacedPart b)
		{
			AppliedPartData.HitboxMesh[] hitboxes = a.appliedData.hitboxes;
			foreach (AppliedPartData.HitboxMesh hitboxMesh in hitboxes)
			{
				if (hitboxMesh.hitboxOnly)
				{
					continue;
				}
				for (int j = 0; j < hitboxMesh.indices.Length; j += 3)
				{
					Vector2 a2 = position + hitboxMesh.vertices[hitboxMesh.indices[j]] * a.orientation;
					Vector2 a3 = position + hitboxMesh.vertices[hitboxMesh.indices[j + 1]] * a.orientation;
					Vector2 a4 = position + hitboxMesh.vertices[hitboxMesh.indices[j + 2]] * a.orientation;
					AppliedPartData.HitboxMesh[] hitboxes2 = b.appliedData.hitboxes;
					foreach (AppliedPartData.HitboxMesh hitboxMesh2 in hitboxes2)
					{
						if (hitboxMesh2.hitboxOnly)
						{
							continue;
						}
						for (int l = 0; l < hitboxMesh2.indices.Length; l += 3)
						{
							Vector2 b2 = b.position + hitboxMesh2.vertices[hitboxMesh2.indices[l]] * b.orientation;
							Vector2 b3 = b.position + hitboxMesh2.vertices[hitboxMesh2.indices[l + 1]] * b.orientation;
							Vector2 b4 = b.position + hitboxMesh2.vertices[hitboxMesh2.indices[l + 2]] * b.orientation;
							if (Triangulator.Intersect(a2, a3, a4, b2, b3, b4))
							{
								return false;
							}
						}
					}
				}
			}
			return false;
		}

		public static float SurfacesConnect(PlacedPart partA, PartData.AttachmentSurface surfaceA, PlacedPart partB, PartData.AttachmentSurface surfaceB)
		{
			bool flag = Mathf.Abs((surfaceA.size * partA.orientation).x) > 0.05f;
			bool flag2 = Mathf.Abs((surfaceB.size * partB.orientation).x) > 0.05f;
			if (flag != flag2)
			{
				return 0f;
			}
			Vector2 worldPosition = partA.GetWorldPosition(surfaceA.start);
			Vector2 worldPosition2 = partB.GetWorldPosition(surfaceB.start);
			if ((!flag) ? (Mathf.Abs(worldPosition.x - worldPosition2.x) > 0.05f) : (Mathf.Abs(worldPosition.y - worldPosition2.y) > 0.05f))
			{
				return 0f;
			}
			Vector2 worldPosition3 = partA.GetWorldPosition(surfaceA.start + surfaceA.size);
			Vector2 worldPosition4 = partB.GetWorldPosition(surfaceB.start + surfaceB.size);
			return (!flag) ? Utility.Overlap(worldPosition.y, worldPosition3.y, worldPosition2.y, worldPosition4.y) : Utility.Overlap(worldPosition.x, worldPosition3.x, worldPosition2.x, worldPosition4.x);
		}

		public static bool PointsConnect(PlacedPart partA, PartData.SwitchPoint switchPointA, PlacedPart partB, PartData.SwitchPoint switchPointB)
		{
			return switchPointB.trigger && Vector2.SqrMagnitude(partA.GetWorldPosition(switchPointA.position) - partB.GetWorldPosition(switchPointB.position)) < 0.010000001f && switchPointA.type == switchPointB.type;
		}

		public void UpdateInteractionKeys(List<PlacedPart> parts, Utility.Location location)
		{
			interactionKeys = Utility.GetKeys(partData, location, orientation, GetConnectionSurfaces(parts), GetSwitchPoints(parts));
		}

		public void UpdateData()
		{
			bool hitboxChange = false;
			appliedData.Update(partData, interactionKeys, ref hitboxChange, ref hitboxChange);
		}

		public void UpdateData(ref bool hitboxChanged, ref bool surfaceChanged)
		{
			appliedData.Update(partData, interactionKeys, ref hitboxChanged, ref surfaceChanged);
		}

		public void UpdateIcon()
		{
			Utility.ApplySwitchesToIcon(partIcon, partData, interactionKeys);
		}

		public void UpdateConnected(List<PlacedPart> parts, bool updateData, bool updateIcon)
		{
		}

		private bool[] GetConnectionSurfaces(List<PlacedPart> parts)
		{
			List<bool> list = new List<bool>();
			PartData.AttachmentSurface[] surfaces = appliedData.surfaces;
			foreach (PartData.AttachmentSurface surfaceA in surfaces)
			{
				list.Add(SurfaceConnects(surfaceA, parts));
			}
			return list.ToArray();
		}

		private bool SurfaceConnects(PartData.AttachmentSurface surfaceA, List<PlacedPart> parts)
		{
			foreach (PlacedPart part in parts)
			{
				if (part == this)
				{
					continue;
				}
				PartData.AttachmentSurface[] surfaces = part.appliedData.surfaces;
				foreach (PartData.AttachmentSurface surfaceB in surfaces)
				{
					if (SurfacesConnect(this, surfaceA, part, surfaceB) > 0.01f)
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool[] GetSwitchPoints(List<PlacedPart> parts)
		{
			bool[] array = new bool[partData.switchPoints.Length];
			for (int i = 0; i < partData.switchPoints.Length; i++)
			{
				foreach (PlacedPart part in parts)
				{
					if (part.partData != null && part != this)
					{
						PartData.SwitchPoint[] switchPoints = part.partData.switchPoints;
						foreach (PartData.SwitchPoint switchPointB in switchPoints)
						{
							array[i] = array[i] || PointsConnect(this, partData.switchPoints[i], part, switchPointB);
						}
					}
				}
			}
			return array;
		}
	}
}
