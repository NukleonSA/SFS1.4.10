using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace NewBuildSystem
{
	[CreateAssetMenu]
	public class PartData : SerializedScriptableObject
	{
		[Serializable]
		public class Hitbox
		{
			public Vector3[] hitboxPoints = new Vector3[0];

			public bool hitboxOnly;

			public const float edge = 0.001f;

			public Vector2[] GetVertices()
			{
				Vector2[] array = new Vector2[hitboxPoints.Length * 2];
				float num = Mathf.Sign(hitboxPoints[1].y - hitboxPoints[0].y);
				for (int i = 0; i < hitboxPoints.Length; i++)
				{
					float num2 = ((i != 0) ? ((i != hitboxPoints.Length - 1) ? 0f : (-0.001f)) : 0.001f);
					array[i * 2] = (Vector2)hitboxPoints[i] + new Vector2(0f - (hitboxPoints[i].z - 0.001f), num2 * num);
					array[i * 2 + 1] = (Vector2)hitboxPoints[i] + new Vector2(hitboxPoints[i].z - 0.001f, num2 * num);
				}
				return array;
			}

			public int[] GetIndices()
			{
				List<int> list = new List<int>();
				for (int i = 0; i < hitboxPoints.Length - 1; i++)
				{
					list.Add(i * 2);
					list.Add(i * 2 + 1);
					list.Add(i * 2 + 2);
					list.Add(i * 2 + 1);
					list.Add(i * 2 + 2);
					list.Add(i * 2 + 3);
				}
				return list.ToArray();
			}
		}

		[Serializable]
		public class AttachmentSurface
		{
			[GUIColor(1f, 0.9f, 0.9f, 1f)]
			public string switchName;

			public Vector2 size;

			public Vector2 start;

			public bool fuelFlow;

			public float surfaceLenght
			{
				get
				{
					return Mathf.Abs(size.x + size.y);
				}
			}
		}

		[Serializable]
		public class DragSurface
		{
			[BoxGroup]
			public int surfaceId = -1;

			[BoxGroup]
			public bool point;

			[BoxGroup]
			[ShowIf("ShowAngleDeg", true)]
			public float angleDeg;

			[BoxGroup]
			[HideIf("point", true)]
			public float angleRad;

			[BoxGroup]
			public float size;

			public DragSurface(int surfaceId, bool point, float angleRad, float size)
			{
				this.surfaceId = surfaceId;
				this.point = point;
				this.angleRad = angleRad;
				this.size = size;
			}

			private bool ShowAngleDeg()
			{
				return surfaceId == -1 && !point;
			}
		}

		[Serializable]
		public class SwitchPoint
		{
			[GUIColor(1f, 0.9f, 0.9f, 1f)]
			public string switchName;

			public Vector2 position;

			[ValueDropdown("TypeList")]
			public string type;

			public bool trigger;

			public bool magnet;

			private string[] TypeList
			{
				get
				{
					return new string[15]
					{
						"None", "2 wide", "4 wide", "6 wide", "8 wide", "10 wide", "12 wide", "Engine Bottom", "4 fairing", "6 fairing",
						"8 fairing", "10 fairing", "12 fairing", "14 fairing", "16 fairing"
					};
				}
			}
		}

		[Serializable]
		public class Switch
		{
			[Serializable]
			public class Output
			{
				public enum Type
				{
					Active = 0,
					Innactive = 1,
					SpriteColor = 2,
					MeshColor = 3,
					DragSurface = 4,
					SetMesh = 5,
					Hitbox = 6,
					Surface = 7
				}

				public enum MeshSetType
				{
					Width = 0,
					PosX = 1,
					PosY = 2,
					UVy = 3
				}

				public string code = "t";

				public Type type;

				public MeshSetType setType;

				public int[] path = new int[0];

				public Color color = Color.white;

				public int index;

				public int hitboxIndex;
			}

			[Serializable]
			public class Variable
			{
				[GUIColor(1f, 0.9f, 0.9f, 1f)]
				[HorizontalGroup(0f, 0, 0, 0)]
				[HideLabel]
				public string switchName;

				[GUIColor(1f, 0.9f, 0.9f, 1f)]
				[HorizontalGroup(0f, 0, 0, 0)]
				[HideLabel]
				public string code;
			}

			[HideInInspector]
			public PartData parent;

			public Variable[] variables = new Variable[0];

			[CustomValueDrawer("DrawOutput")]
			public Output[] outputs;
		}

		[Serializable]
		public class Skins
		{
			[Serializable]
			public class Path
			{
				public int[] path = new int[0];
			}

			[HideInInspector]
			public PartData parent;

			public Skin[] skins;

			[CustomValueDrawer("DrawPaths")]
			public Path[] paths = new Path[0];
		}

		[CustomValueDrawer("DrawPartData")]
		public string displayName;

		[TextArea(1, 10)]
		public string description;

		public Transform prefab;

		public float breakVelocity = 5f;

		[Space]
		public Hitbox[] hitbox;

		[TableList]
		[Space]
		public AttachmentSurface[] attachmentSurfaces;

		[TableList]
		[Space]
		public SwitchPoint[] switchPoints = new SwitchPoint[0];

		[Space]
		[FoldoutGroup("Drag", 0)]
		public float dragMultiplier = 1f;

		[Space]
		[FoldoutGroup("Drag", 0)]
		public DragSurface[] dragSurfaces;

		[Space]
		[InlineProperty]
		[HideLabel]
		[BoxGroup]
		public Switch switchData;

		[BoxGroup("Skins", false, false, 0)]
		[InlineProperty]
		[HideLabel]
		public Skins skins;

		[FoldoutGroup("Build Data", 0)]
		public Vector2 centerOfRotation;

		[FoldoutGroup("Build Data", 0)]
		public float depth;

		[FoldoutGroup("Build Data", 0)]
		public float pickGridScale = 1f;

		[FoldoutGroup("Build Data", 0)]
		public bool flip2stickX;

		[FoldoutGroup("Build Data", 0)]
		public bool flip2stickY;

		[FoldoutGroup("Build Data", 0)]
		public Vector2 pickOffset = Vector2.up;

		[BoxGroup("Avalibility Data", true, false, 0)]
		public bool inFull;

		public string[] oldName;

		[Button]
		[FoldoutGroup("Drag", 0)]
		[HideInInspector]
		public void TransferSurfaces()
		{
			List<DragSurface> list = new List<DragSurface>();
			for (int i = 0; i < attachmentSurfaces.Length; i++)
			{
				float num = Mathf.Atan2(attachmentSurfaces[i].size.y, attachmentSurfaces[i].size.x);
				if (Vector3.Cross(attachmentSurfaces[i].start - centerOfRotation, attachmentSurfaces[i].size).z > 0f)
				{
					num += (float)Math.PI;
				}
				list.Add(new DragSurface(i, false, num, attachmentSurfaces[i].size.magnitude));
			}
			for (int j = 0; j < dragSurfaces.Length; j++)
			{
				if (dragSurfaces[j].surfaceId == -1)
				{
					list.Add(dragSurfaces[j]);
				}
			}
			dragSurfaces = list.ToArray();
		}

		public virtual string GetDescriptionRaw()
		{
			string empty = string.Empty;
			empty = empty + "质量:" + prefab.GetComponent<Part>().mass * 2f + "t/";
			Part component = prefab.GetComponent<Part>();
			List<string> list = new List<string>();
			for (int i = 0; i < component.modules.Length; i++)
			{
				list.AddRange(component.modules[i].DescriptionVariables);
			}
			for (int j = 0; j < list.Count; j++)
			{
				empty = empty + list[j] + "/";
			}
			return empty + description;
		}

		public void OnValidate()
		{
			if (prefab != null)
			{
				prefab.GetComponent<Part>().partData = this;
			}
			switchData.parent = this;
			skins.parent = this;
		}
	}
}
