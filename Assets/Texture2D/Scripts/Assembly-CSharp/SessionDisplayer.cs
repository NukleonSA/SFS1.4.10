using ServerTransferProgram;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class SessionDisplayer : MonoBehaviour
{
	[OnInspectorGUI("UpdateInspector", true)]
	[ReadOnly]
	public string status = "No text object found!";

	[ReadOnly]
	public string preview = "Nothing to show yet.";

	private Text t;

	private void Start()
	{
		t = GetComponent<Text>();
	}

	private void Update()
	{
		t.text = t.text.FormatTo(AnalyticsSessionInfo.userId);
	}

	private void UpdateInspector()
	{
		if (GetComponent<Text>() == null)
		{
			status = "No text object found!";
			return;
		}
		status = "Ok!";
		preview = GetComponent<Text>().text.FormatTo(AnalyticsSessionInfo.userId);
	}
}
