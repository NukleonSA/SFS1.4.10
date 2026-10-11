using System.Collections.Generic;
using UnityEngine;

public class RcsModule : Module
{
	public MoveModule effect;

	public float torqueAngle;

	public float directionAngle;

	public float thrust;

	public float resourceConsuption;

	public bool showParametersDescription = true;

	public override List<string> DescriptionVariables
	{
		get
		{
			List<string> result;
			if (showParametersDescription)
			{
				List<string> list = new List<string>();
				list.Add("推力:" + thrust + "kN");
				list.Add("比冲:" + (int)(thrust / resourceConsuption / 9.8f) + "s");
				result = list;
			}
			else
			{
				result = new List<string>();
			}
			return result;
		}
	}

	public override bool IsSorted()
	{
		return true;
	}

	public override void OnPartUsed()
	{
		part.vessel.partsManager.ToggleRCS(part.vessel, part.vessel == Ref.mainVessel);
	}

	public void UseRcs(Rigidbody2D rb2d, Vessel vessel, float horizontalAxis, float rcsInputDirection, bool directionInput, ref double fuelUsed)
	{
		effect.SetTargetTime(0f);
		Vector2 vector = effect.transform.TransformDirection(Vector3.left);
		Vector2 vector2 = base.transform.localPosition + part.centerOfMass * part.orientation;
		Vector2 posToCenterOfMass = rb2d.centerOfMass - vector2;
		if (TorqueThrust(vector, posToCenterOfMass, rb2d, vessel, horizontalAxis) || (directionInput && DirectionThrust(vector, rcsInputDirection)))
		{
			fuelUsed += (double)resourceConsuption * (double)Time.fixedDeltaTime;
			rb2d.AddForceAtPosition(vector * thrust, rb2d.GetRelativePoint(vector2));
			effect.SetTargetTime(0.9f);
		}
	}

	private bool TorqueThrust(Vector2 thrustNormal, Vector2 posToCenterOfMass, Rigidbody2D rb2d, Vessel vessel, float horizontalAxis)
	{
		if (Ref.inputController.horizontalAxis == 0f && Mathf.Abs(rb2d.angularVelocity) < 2f)
		{
			return false;
		}
		float current = Mathf.Atan2(thrustNormal.y, thrustNormal.x) * 57.29578f - rb2d.rotation;
		float num = Mathf.Atan2(posToCenterOfMass.y, posToCenterOfMass.x) * 57.29578f;
		return (horizontalAxis == 1f && Mathf.Abs(Mathf.DeltaAngle(current, num + 90f)) < torqueAngle) || (horizontalAxis == -1f && Mathf.Abs(Mathf.DeltaAngle(current, num - 90f)) < torqueAngle);
	}

	private bool DirectionThrust(Vector2 thrustNormal, float rcsInputDirection)
	{
		float current = Mathf.Atan2(thrustNormal.y, thrustNormal.x) * 57.29578f;
		return Mathf.Abs(Mathf.DeltaAngle(current, rcsInputDirection)) < directionAngle;
	}
}
