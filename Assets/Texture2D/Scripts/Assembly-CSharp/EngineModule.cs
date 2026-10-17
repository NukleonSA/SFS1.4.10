using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EngineModule : Module
{
	[Serializable]
	public struct Flame
	{
		public Transform flame;

		public Vector4 sizeBounds;
	}

	[Serializable]
	public struct FlameParticle
	{
		public ParticleSystem particle;

		public ParticleSystem.MainModule particleMain;

		public FlameParticle(ParticleSystem particle)
		{
			this.particle = particle;
			particleMain = particle.main;
		}
	}

	[BoxGroup("1", false, false, 0)]
	[InlineProperty]
	public BoolValueHolder engineOn;

	[BoxGroup("2", false, false, 0)]
	[SuffixLabel("kn", false)]
	public float thrust;

	public FlowModule[] resourceSources;

	[InlineProperty]
	public FloatValueHolder consumption;

	[Space]
	public MoveModule nozzleMove;

	[InlineProperty]
	public FloatValueHolder throttle;

	public AudioSource audioSource;

	[BoxGroup("Flame Data", false, false, 0)]
	public Transform deflectedParticleHolder;

	[BoxGroup("Flame Data", false, false, 0)]
	public float flameLenght;

	[BoxGroup("Flame Data", false, false, 0)]
	public LayerMask particleCollisionMask;

	[BoxGroup("4", false, false, 0)]
	public int[] interstagePath;

	private FlameParticle deflectedParticle1;

	private FlameParticle deflectedParticle2;

	public bool showParametersDescription = true;

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(engineOn);
			return list;
		}
	}

	public override List<string> DescriptionVariables
	{
		get
		{
			List<string> result;
			if (showParametersDescription)
			{
				List<string> list = new List<string>();
				list.Add("推力:" + (int)(thrust * 2f) / 10 + "t");
				list.Add("比冲(Isp):" + (int)(thrust / consumption.floatValue / 9.8f) + "s");
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

	private void Start()
	{
		if (deflectedParticleHolder != null)
		{
			deflectedParticle1 = new FlameParticle(deflectedParticleHolder.GetChild(0).GetComponent<ParticleSystem>());
			deflectedParticle2 = new FlameParticle(deflectedParticleHolder.GetChild(1).GetComponent<ParticleSystem>());
		}
	}

	public override void OnPartUsed()
	{
		if (!Ref.timeWarping)
		{
			ToggleEngine(true);
		}
		else
		{
			MenuManager.ShowMsg("Cannot toggle engine while time warping");
		}
	}

	private bool IsCovered()
	{
		return interstagePath.Length == 0 || Utility.GetByPath(base.transform, interstagePath).gameObject.activeSelf;
	}

	public void ToggleEngine(bool showMsg)
	{
		if (IsCovered())
		{
			return;
		}
		if (engineOn.boolValue)
		{
			engineOn.boolValue = false;
			if (showMsg)
			{
				MenuManager.ShowMsg("引擎已关闭");
			}
			if (nozzleMove != null)
			{
				nozzleMove.SetTargetTime(0f);
			}
		}
		else
		{
			for (int i = 0; i < resourceSources.Length; i++)
			{
				if (resourceSources[i].state != FlowModule.State.Flowing)
				{
					MenuManager.ShowMsg((resourceSources[i].state != FlowModule.State.NoSource) ? ("没有" + Resource.GetResourceName(resourceSources[i].resourceType).ToLower()) : ("No " + Resource.GetResourceName(resourceSources[i].resourceType).ToLower() + " source"));
					return;
				}
			}
			engineOn.boolValue = true;
			if (showMsg)
			{
				MenuManager.ShowMsg("引擎已开启");
			}
		}
		UpdateEngineThrottle(base.transform.parent.GetComponentInParent<Vessel>().throttle);
	}

	public void OnSourceStateChange()
	{
		if (throttle.floatValue != 0f && !FlowModule.ConfirmSources(resourceSources, Ref.mainVessel == part.vessel) && engineOn.boolValue)
		{
			ToggleEngine(false);
		}
	}

	public void UpdateEngineThrottle(Vessel.Throttle newThrottle)
	{
		bool flag = engineOn.boolValue && newThrottle.throttleOn && newThrottle.throttleRaw > 0f && !Ref.timeWarping;
		throttle.floatValue = ((!flag) ? 0f : newThrottle.throttle);
		audioSource.volume = throttle.floatValue * 0.6f;
		if (flag == audioSource.isPlaying)
		{
			return;
		}
		if (audioSource.isPlaying)
		{
			audioSource.Stop();
			return;
		}
		audioSource.Play();
		if (Ref.mainVessel != null)
		{
			for (int i = 0; i < Ref.mainVessel.partsManager.engineModules.Count; i++)
			{
				Ref.mainVessel.partsManager.engineModules[i].audioSource.time = 0f;
			}
		}
	}

	public void ApplyThrust(ref float totalThrust, Rigidbody2D rb2d, float xAxis, Vessel.Throttle throttle, bool mainVessel)
	{
		if (!engineOn.boolValue)
		{
			return;
		}
		// 用含小数倍率的 SizeX/SizeY，让推力面积与喷口动画跟着缩放；Mx=My=1 时与原版一致。
		xAxis *= part.orientation.SizeX * part.orientation.SizeY;
		if (nozzleMove != null)
		{
			nozzleMove.SetTargetTime(xAxis);
		}
		if (!throttle.throttleOn || throttle.throttle == 0f)
		{
			return;
		}
		float num = thrust * throttle.throttle;
		totalThrust += num;
		Vector2 vector = Quaternion.Euler(0f, 0f, base.transform.rotation.eulerAngles.z) * new Vector2(0f, part.orientation.SizeY);
		Vector2 force = vector * num;
		Vector2 vector2 = (base.transform.localPosition + part.centerOfMass * part.orientation) * num;
		rb2d.AddForceAtPosition(force, rb2d.GetRelativePoint(vector2 / num));
		if (deflectedParticleHolder == null)
		{
			return;
		}
		bool flag = false;
		if (Ref.mainVesselTerrainHeight < 100.0)
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, -vector, flameLenght, particleCollisionMask);
			if ((bool)raycastHit2D)
			{
				flag = true;
				deflectedParticle1.particle.transform.localPosition = new Vector3(0f, (0f - raycastHit2D.distance) / deflectedParticle1.particle.transform.lossyScale.y * Mathf.Abs(deflectedParticle1.particle.transform.localScale.y), 0f);
				deflectedParticle2.particle.transform.localPosition = new Vector3(0f, (0f - raycastHit2D.distance) / deflectedParticle2.particle.transform.lossyScale.y * Mathf.Abs(deflectedParticle2.particle.transform.localScale.y), 0f);
				deflectedParticle1.particleMain.startColor = new Color(1f, 1f, 1f, throttle.throttleRaw * (1f - raycastHit2D.distance / flameLenght));
				deflectedParticle2.particleMain.startColor = new Color(1f, 1f, 1f, throttle.throttleRaw * (1f - raycastHit2D.distance / flameLenght));
			}
		}
		if (deflectedParticle1.particle.gameObject.activeSelf != flag)
		{
			deflectedParticle1.particle.gameObject.SetActive(flag);
			deflectedParticle2.particle.gameObject.SetActive(flag);
		}
	}
}
