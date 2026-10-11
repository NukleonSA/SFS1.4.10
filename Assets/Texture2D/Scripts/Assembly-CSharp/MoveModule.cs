using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MoveModule : Module
{
	[Serializable]
	public class MoveData
	{
		public Type type;

		public float offset;

		[ShowIf("ShowTransform", true)]
		public Transform transform;

		[ShowIf("ShowSpriteRenderer", true)]
		public SpriteRenderer spriteRenderer;

		[ShowIf("ShowImage", true)]
		public Image image;

		[ShowIf("ShowCurveX", true)]
		public AnimationCurve X;

		[ShowIf("ShowCurveY", true)]
		public AnimationCurve Y;

		[ShowIf("ShowGradient", true)]
		public Gradient gradient;

		[ShowIf("ShowAudio", true)]
		public AudioSource audioSource;

		public MoveData()
		{
			X = new AnimationCurve();
			Y = new AnimationCurve();
			gradient = new Gradient();
		}

		private bool ShowTransform()
		{
			return (new bool[10] { true, true, true, false, false, false, false, true, true, false })[(int)type];
		}

		private bool ShowSpriteRenderer()
		{
			return type == Type.SpriteColor;
		}

		private bool ShowImage()
		{
			return type == Type.ImageColor;
		}

		private bool ShowCurveX()
		{
			return (new bool[10] { true, true, true, true, true, false, false, true, true, true })[(int)type];
		}

		private bool ShowCurveY()
		{
			return (new bool[10] { false, true, true, true, true, false, false, false, false, false })[(int)type];
		}

		private bool ShowGradient()
		{
			return type == Type.SpriteColor || type == Type.ImageColor;
		}

		private bool ShowAudio()
		{
			return type == Type.SoundVolume;
		}
	}

	public enum Type
	{
		RotationZ = 0,
		Scale = 1,
		Position = 2,
		CenterOfMass = 3,
		CenterOfDrag = 4,
		SpriteColor = 5,
		ImageColor = 6,
		Active = 7,
		Inactive = 8,
		SoundVolume = 9
	}

	public string pourpose;

	[InlineProperty]
	[FoldoutGroup("", 0)]
	public FloatValueHolder time;

	[InlineProperty]
	[FoldoutGroup("", 0)]
	public FloatValueHolder targetTime;

	[FoldoutGroup("", 0)]
	public float animationTime;

	[Space]
	[InlineProperty]
	[FoldoutGroup("", 0)]
	public MoveData[] animationElements = new MoveData[0];

	public override List<object> SaveVariables
	{
		get
		{
			List<object> list = new List<object>();
			list.Add(time);
			list.Add(targetTime);
			return list;
		}
	}

	private void Start()
	{
		UpdateAnimation();
	}

	public void TargetTime(float none)
	{
		base.enabled = true;
	}

	private void Update()
	{
		float num = ((!(Time.deltaTime > 0f)) ? 1f : Time.deltaTime) / animationTime;
		if (Mathf.Abs(targetTime.floatValue - time.floatValue) < num)
		{
			time.floatValue = targetTime.floatValue;
			base.enabled = false;
		}
		else
		{
			time.floatValue += num * Mathf.Sign(targetTime.floatValue - time.floatValue);
		}
		UpdateAnimation();
	}

	public void SetTargetTime(float newTargetTime)
	{
		targetTime.floatValue = newTargetTime;
		base.enabled = true;
	}

	public void AddToTargetTime(float change)
	{
		targetTime.floatValue += change;
		base.enabled = true;
	}

	public void AddToTargetTimeClamped(string data)
	{
		string[] array = data.Split(","[0]);
		if (array.Length == 3)
		{
			targetTime.floatValue = Mathf.Clamp(targetTime.floatValue + float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]));
			base.enabled = true;
		}
	}

	public void SetTime(float newTime)
	{
		time.floatValue = newTime;
		UpdateAnimation();
		base.enabled = true;
	}

	public void UpdateAnimation()
	{
		for (int i = 0; i < animationElements.Length; i++)
		{
			switch (animationElements[i].type)
			{
			case Type.RotationZ:
				animationElements[i].transform.localEulerAngles = new Vector3(0f, 0f, animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset));
				break;
			case Type.Position:
				animationElements[i].transform.localPosition = new Vector3(animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset), animationElements[i].Y.Evaluate(time.floatValue - animationElements[i].offset), 0f);
				break;
			case Type.Scale:
				animationElements[i].transform.localScale = new Vector3(animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset), animationElements[i].Y.Evaluate(time.floatValue - animationElements[i].offset), 0f);
				break;
			case Type.SpriteColor:
				animationElements[i].spriteRenderer.color = animationElements[i].gradient.Evaluate(time.floatValue - animationElements[i].offset);
				break;
			case Type.ImageColor:
				animationElements[i].image.color = animationElements[i].gradient.Evaluate(time.floatValue - animationElements[i].offset);
				break;
			case Type.Active:
				if (animationElements[i].transform.gameObject.activeSelf != animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset) > 0f)
				{
					animationElements[i].transform.gameObject.SetActive(animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset) > 0f);
				}
				break;
			case Type.Inactive:
				if (animationElements[i].transform.gameObject.activeSelf != animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset) <= 0f)
				{
					animationElements[i].transform.gameObject.SetActive(animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset) <= 0f);
				}
				break;
			case Type.SoundVolume:
				animationElements[i].audioSource.volume = animationElements[i].X.Evaluate(time.floatValue - animationElements[i].offset);
				break;
			}
		}
	}
}
