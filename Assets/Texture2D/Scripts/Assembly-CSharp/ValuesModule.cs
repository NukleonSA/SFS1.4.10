using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
public class ValuesModule : Module
{
	public delegate void UpdateFloatValueDelegate(float oldValue);

	[Serializable]
	public class Holder
	{
		[Serializable]
		public class MethodRefHolder
		{
			[HorizontalGroup(0f, 0, 0, 0)]
			public Component component;

			[HorizontalGroup(0f, 0, 0, 0)]
			[CustomValueDrawer("Draw")]
			public string methodName;

			public MethodRefHolder(Component component, string methodName)
			{
				this.component = component;
				this.methodName = methodName;
			}
		}

		[HorizontalGroup(0f, 0, 0, 0)]
		[LabelWidth(50f)]
		[LabelText("Name:")]
		public string name;

		[HorizontalGroup(0f, 0, 0, 0)]
		[HideLabel]
		public float floatValue;

		[HorizontalGroup(0f, 0, 0, 0)]
		[HideLabel]
		public Color label = Color.white;

		[HideInInspector]
		public bool hasValidDelegate;

		public UpdateFloatValueDelegate updateDelegate;

		[LabelText("On Value Change")]
		public MethodRefHolder[] methodsHolder = new MethodRefHolder[0];

	}

	[Serializable]
	public class Link
	{
		public enum Type
		{
			Add = 0,
			Multiply = 1
		}

		[InlineProperty]
		[BoxGroup]
		public FloatValueHolder a;

		[InlineProperty]
		[BoxGroup]
		public FloatValueHolder b;

		[BoxGroup]
		public Type operation;

		[InlineProperty]
		[BoxGroup]
		public FloatValueHolder output;

		public void Recalculate()
		{
			output.floatValue = ((operation != Type.Multiply) ? (a.floatValue + b.floatValue) : (a.floatValue * b.floatValue));
		}
	}

	public Holder[] values = new Holder[0];

	public Link[] links;

	private void Start()
	{
		for (int i = 0; i < values.Length; i++)
		{
			for (int j = 0; j < values[i].methodsHolder.Length; j++)
			{
				Component component = values[i].methodsHolder[j].component;
				string methodName = values[i].methodsHolder[j].methodName;
				UpdateFloatValueDelegate updateFloatValueDelegate = Delegate.CreateDelegate(typeof(UpdateFloatValueDelegate), component, methodName) as UpdateFloatValueDelegate;
				if (values[i].updateDelegate != null)
				{
					Holder obj = values[i];
					obj.updateDelegate = (UpdateFloatValueDelegate)Delegate.Combine(obj.updateDelegate, updateFloatValueDelegate);
				}
				else
				{
					values[i].updateDelegate = updateFloatValueDelegate;
				}
			}
		}
		for (int k = 0; k < values.Length; k++)
		{
			values[k].hasValidDelegate = values[k].updateDelegate != null;
			if (values[k].hasValidDelegate)
			{
				values[k].updateDelegate(values[k].floatValue);
			}
		}
	}

	[Button]
	public void ScanForMethods()
	{
		Component[] components = base.gameObject.GetComponents<Component>();
		Component[] array = components;
		foreach (Component component in array)
		{
			MemberInfo[] members = component.GetType().GetMembers();
			for (int j = 0; j < members.Length; j++)
			{
				if (!members[j].ToString().Contains("FloatValueHolder "))
				{
					continue;
				}
				MethodInfo[] methods = component.GetType().GetMethods();
				for (int k = 0; k < methods.Length; k++)
				{
					string text = members[j].Name;
					if (methods[k].Name.ToLower() == text.ToLower())
					{
						FloatValueHolder floatValueHolder = component.GetType().GetField(text).GetValue(component) as FloatValueHolder;
						if (floatValueHolder.useExternalReference)
						{
							List<Holder.MethodRefHolder> list = new List<Holder.MethodRefHolder>(values[floatValueHolder.index].methodsHolder);
							list.Add(new Holder.MethodRefHolder(component, methods[k].Name));
							values[floatValueHolder.index].methodsHolder = list.ToArray();
						}
					}
				}
			}
		}
		for (int l = 0; l < links.Length; l++)
		{
			AddToUpdateDelegates(links[l].a);
			AddToUpdateDelegates(links[l].b);
		}
	}

	private void AddToUpdateDelegates(FloatValueHolder a)
	{
		if (!a.useExternalReference)
		{
			return;
		}
		List<Holder.MethodRefHolder> list = new List<Holder.MethodRefHolder>(values[a.index].methodsHolder);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].methodName == "Recalculate")
			{
				return;
			}
		}
		list.Add(new Holder.MethodRefHolder(this, "Recalculate"));
		values[a.index].methodsHolder = list.ToArray();
	}

	public void Recalculate(float empty)
	{
		for (int i = 0; i < links.Length; i++)
		{
			links[i].Recalculate();
		}
	}
}
