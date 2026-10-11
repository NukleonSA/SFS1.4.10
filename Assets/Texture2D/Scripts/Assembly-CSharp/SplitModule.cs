using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SplitModule : Module
{
	[Serializable]
	public class Piece
	{
		public Vector2 separationForce;

		public GameObject[] toDisable;

		public int[] transferSurfaces;
	}

	public bool fairing;

	public bool triggerPartsAfterSeparation;

	[InlineProperty]
	public FloatValueHolder pieceIndex = new FloatValueHolder
	{
		floatValue = -1f
	};

	[Space]
	public Piece[] pieces;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(pieceIndex);
			return list;
		}
	}

	public override bool IsSorted()
	{
		return true;
	}

	public override void OnModuleLoaded()
	{
		UpdatePieceActive();
	}

	private void UpdatePieceActive()
	{
		if (pieceIndex.floatValue != -1f)
		{
			GameObject[] toDisable = pieces[(int)pieceIndex.floatValue].toDisable;
			foreach (GameObject gameObject in toDisable)
			{
				gameObject.SetActive(false);
			}
		}
	}

	public override void OnPartUsed()
	{
		Split();
	}

	private void Split()
	{
		if (pieceIndex.floatValue != -1f)
		{
			return;
		}
		List<SplitModule> partsToPush = new List<SplitModule>();
		if (fairing)
		{
			foreach (SplitModule connectedFairing in GetConnectedFairings())
			{
				connectedFairing.Deploy(ref partsToPush);
			}
		}
		else
		{
			Deploy(ref partsToPush);
		}
		foreach (SplitModule item in partsToPush)
		{
			Vector2 relativePoint = item.transform.localPosition + item.part.centerOfMass * item.part.orientation;
			Vector2 relativePoint2 = item.part.vessel.partsManager.rb2d.GetRelativePoint(relativePoint);
			Vector2 force = item.transform.TransformVector(item.pieces[(int)item.pieceIndex.floatValue].separationForce);
			item.part.vessel.partsManager.rb2d.AddForceAtPosition(force, relativePoint2);
		}
		if (!triggerPartsAfterSeparation)
		{
			return;
		}
		if (fairing)
		{
			partsToPush[0].part.UsePart();
			return;
		}
		foreach (SplitModule item2 in partsToPush)
		{
			item2.part.UsePart();
		}
	}

	public void Deploy(ref List<SplitModule> partsToPush)
	{
		if (pieceIndex.floatValue != -1f)
		{
			return;
		}
		for (int i = 0; i < pieces.Length; i++)
		{
			SplitModule component = CreateRocket.CreatePart(part.partData.prefab, part.orientation, part.transform.parent, part.transform.localPosition, part.skinId, part.interactionKeys).GetComponent<SplitModule>();
			part.vessel.partsManager.parts.Add(component.part);
			for (int j = 0; j < pieces[i].transferSurfaces.Length; j++)
			{
				SeparatorModule.TransferJoints(part, pieces[i].transferSurfaces[j], component.part, 0);
			}
			partsToPush.Add(component);
			component.pieceIndex.floatValue = i;
			component.UpdatePieceActive();
			component.part.UpdateIcon();
		}
		part.DestroyPart(false, true);
	}

	private List<SplitModule> GetConnectedFairings()
	{
		List<SplitModule> list = new List<SplitModule>();
		list.Add(this);
		Ref.connectionCheckId++;
		base.part.connectionCheckId = Ref.connectionCheckId;
		List<Part> list2 = new List<Part>(1);
		list2.Add(base.part);
		List<Part> list3 = list2;
		while (list3.Count > 0)
		{
			for (int i = 0; i < list3[0].joints.Count; i++)
			{
				Part part = ((!(list3[0].joints[i].fromPart == list3[0])) ? list3[0].joints[i].fromPart : list3[0].joints[i].toPart);
				if (part.connectionCheckId != Ref.connectionCheckId)
				{
					part.connectionCheckId = Ref.connectionCheckId;
					SplitModule component = part.GetComponent<SplitModule>();
					if (!(component == null))
					{
						list3.Add(part);
						list.Add(component);
					}
				}
			}
			list3.RemoveAt(0);
		}
		return list;
	}
}
