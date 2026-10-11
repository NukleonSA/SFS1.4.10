using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class MyAnalytics : MonoBehaviour
{
	public enum Type
	{
		Test2 = 0
	}

	public static void Event(Type type)
	{
		Analytics.CustomEvent(type.ToString(), new Dictionary<string, object> { 
		{
			"User Id",
			AnalyticsSessionInfo.userId
		} });
	}
}
