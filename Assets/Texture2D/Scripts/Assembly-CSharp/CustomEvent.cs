using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class CustomEvent : MonoBehaviour
{
	[HideLabel]
	[Space(3f)]
	public string function;

	public UnityEvent customEvent;

	[Button]
	public void InvokeEvenets()
	{
		customEvent.Invoke();
	}

	public void InvokeEventsDelayed(float delay)
	{
		Invoke("InvokeEvenets", delay);
	}

	public void Awake()
	{
	}
}
