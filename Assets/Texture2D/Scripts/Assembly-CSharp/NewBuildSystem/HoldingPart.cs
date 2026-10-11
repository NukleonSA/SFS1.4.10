using System;
using UnityEngine;

namespace NewBuildSystem
{
	[Serializable]
	public class HoldingPart
	{
		public Vector2 holdingOffset;

		public PlacedPart part;

		public HoldingPart(Vector2 holdingOffset, PlacedPart part)
		{
			this.holdingOffset = holdingOffset;
			this.part = part;
		}

		public void Rotate90(float roundStregth)
		{
			Vector2 vector = (Vector2)part.partIcon.position + part.partData.centerOfRotation * part.orientation;
			Vector2 vector2 = holdingOffset + part.partData.centerOfRotation * part.orientation;
			part.orientation.Rotate90();
			holdingOffset = vector2 - part.partData.centerOfRotation * part.orientation;
			Transform parent = ((!(part.partData != null)) ? null : part.partIcon.parent);
			if (part.partData != null)
			{
				UnityEngine.Object.Destroy(part.partIcon.gameObject);
			}
			part.partIcon = PartGrid.LoadIcon(part.partData, part.orientation, part.skinId, part.position, part.partData.prefab.localScale, parent, 100, Color.white, true);
			Utility.ApplySwitchesToIcon(part.partIcon, part.partData, Utility.GetKeys(part.partData, Utility.Location.held, part.orientation, new bool[0], new bool[0]));
			part.partIcon.transform.position = vector - part.partData.centerOfRotation * part.orientation;
		}

		public void FlipX(float roundStregth)
		{
			Vector2 vector = (Vector2)part.partIcon.position + part.partData.centerOfRotation * part.orientation;
			Vector2 vector2 = holdingOffset + part.partData.centerOfRotation * part.orientation;
			part.orientation.FlipX();
			holdingOffset = vector2 - part.partData.centerOfRotation * part.orientation;
			Transform parent = ((!(part.partData != null)) ? null : part.partIcon.parent);
			if (part.partData != null)
			{
				UnityEngine.Object.Destroy(part.partIcon.gameObject);
			}
			part.partIcon = PartGrid.LoadIcon(part.partData, part.orientation, part.skinId, part.position, part.partData.prefab.localScale, parent, 100, Color.white, true);
			Utility.ApplySwitchesToIcon(part.partIcon, part.partData, Utility.GetKeys(part.partData, Utility.Location.held, part.orientation, new bool[0], new bool[0]));
			part.partIcon.transform.position = vector - part.partData.centerOfRotation * part.orientation;
		}

		public void FlipY(float roundStregth)
		{
			Vector2 vector = (Vector2)part.partIcon.position + part.partData.centerOfRotation * part.orientation;
			Vector2 vector2 = holdingOffset + part.partData.centerOfRotation * part.orientation;
			part.orientation.FlipY();
			holdingOffset = vector2 - part.partData.centerOfRotation * part.orientation;
			Transform parent = ((!(part.partData != null)) ? null : part.partIcon.parent);
			if (part.partData != null)
			{
				UnityEngine.Object.Destroy(part.partIcon.gameObject);
			}
			part.partIcon = PartGrid.LoadIcon(part.partData, part.orientation, part.skinId, part.position, part.partData.prefab.localScale, parent, 100, Color.white, true);
			Utility.ApplySwitchesToIcon(part.partIcon, part.partData, Utility.GetKeys(part.partData, Utility.Location.held, part.orientation, new bool[0], new bool[0]));
			part.partIcon.transform.position = vector - part.partData.centerOfRotation * part.orientation;
		}

		public void NextSkin()
		{
			part.NextSkin();
		}

		public static bool isNull(HoldingPart holdingPart)
		{
			return holdingPart == null || holdingPart.part == null || holdingPart.part.partData == null;
		}
	}
}
