using NewBuildSystem;
using UnityEngine;

public class ParachuteModule : Module
{
	public enum State
	{
		Packed = 0,
		SemiDeployed = 1,
		Deployed = 2,
		Relased = 3
	}

	public float maxDeployVelocity;

	public double minDeployHeight;

	public float drag;

	public AnimationCurve dragCurve;

	public Double3 lastPos;

	public float lastMovingTime;

	public MoveModule moveModule;

	public Transform parachute;

	public void UpdateDeploymentPercent(float empty)
	{
		part.UpdateDragSurfaces(part.GetSurfacesFloat());
		part.activeDragSurfaces.Add(new Part.ActiveDragSurafce(new PartData.DragSurface(-1, true, float.NaN, drag * dragCurve.Evaluate(moveModule.time.floatValue)), part.orientation, 0f));
	}

	private void LateUpdate()
	{
		if (moveModule.targetTime.floatValue == 1f || moveModule.targetTime.floatValue == 2f)
		{
			if (moveModule.targetTime.floatValue == 1f && Ref.mainVesselTerrainHeight < 110.0)
			{
				DeployParachute(2f, false);
			}
			CalculateParachuteRotation();
			if ((Ref.velocityOffset + GetComponentInParent<Rigidbody2D>().velocity).sqrMagnitude > 0.0025000000000000005)
			{
				lastMovingTime = Time.time;
			}
			else if (lastMovingTime + 0.5f < Time.time)
			{
				RelaseParachute();
			}
		}
	}

	public void CalculateParachuteRotation()
	{
		Double3 @double = Ref.positionOffset + base.transform.position;
		Vector2 toVector = (lastPos - @double).toVector2;
		float z = Mathf.Atan2(toVector.y, toVector.x) * 57.29578f - 90f + Mathf.Sin(Time.time) * 3f;
		parachute.eulerAngles = new Vector3(0f, 0f, z);
		if (toVector.sqrMagnitude > 100f)
		{
			lastPos = @double + toVector.normalized * 10f;
		}
		Debug.DrawLine((@double - Ref.positionOffset).toVector3, (lastPos - Ref.positionOffset).toVector3);
	}

	public override void OnPartUsed()
	{
		if (Ref.timeWarping)
		{
			if (moveModule.targetTime.floatValue == 0f)
			{
				MenuManager.ShowMsg("时间加速时无法展开降落伞");
			}
			return;
		}
		Rigidbody2D componentInParent = GetComponentInParent<Rigidbody2D>();
		if (moveModule.targetTime.floatValue == 0f)
		{
			if (!Ref.controller.loadedPlanet.data.hasAtmosphere || Ref.mainVesselHeight > Ref.controller.loadedPlanet.data.ATMOSPHERE_DATA.PHYSICS.height * 0.8)
			{
				MenuManager.ShowMsg("无法在真空中展开降落伞");
				return;
			}
			if (Ref.mainVesselTerrainHeight > minDeployHeight)
			{
				MenuManager.ShowMsg("无法在上方展开降落伞" + minDeployHeight + "m");
				return;
			}
			if ((Ref.velocityOffset + componentInParent.velocity).magnitude2d > (double)maxDeployVelocity)
			{
				MenuManager.ShowMsg("移动速度超过" + maxDeployVelocity + "m/s");
				return;
			}
			if ((Ref.velocityOffset + componentInParent.velocity).magnitude2d < 3.0)
			{
				MenuManager.ShowMsg("移动速度低于3m/s时无法展开降落伞");
				return;
			}
			MenuManager.ShowMsg("降落伞半展开");
			DeployParachute(1f, true);
		}
		else if (moveModule.targetTime.floatValue == 1f)
		{
			if (Ref.mainVesselTerrainHeight > 500.0)
			{
				MenuManager.ShowMsg("无法在500米以上完全展开降落伞");
				return;
			}
			if ((Ref.velocityOffset + componentInParent.velocity).magnitude2d > (double)(maxDeployVelocity / 5f))
			{
				MenuManager.ShowMsg("移动速度超过" + maxDeployVelocity / 5f + "m/s");
				return;
			}
			MenuManager.ShowMsg("降落伞完全展开");
			DeployParachute(2f, false);
		}
		else if (moveModule.targetTime.floatValue == 2f && !(moveModule.time.floatValue < 2f))
		{
			MenuManager.ShowMsg("降落伞已切断");
			RelaseParachute();
		}
	}

	public void DeployParachute(float targetTime, bool alignWithVelocity)
	{
		moveModule.SetTargetTime(targetTime);
		if (alignWithVelocity)
		{
			Rigidbody2D componentInParent = GetComponentInParent<Rigidbody2D>();
			Double3 @double = Ref.positionOffset + base.transform.position;
			lastPos = @double - (Ref.velocityOffset + componentInParent.velocity);
			CalculateParachuteRotation();
		}
	}

	public void RelaseParachute()
	{
		moveModule.SetTime(3f);
		moveModule.SetTargetTime(3f);
	}
}
