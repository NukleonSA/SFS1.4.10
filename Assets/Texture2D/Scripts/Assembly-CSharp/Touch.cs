using System;
using NewBuildSystem;
using UnityEngine;

public class Touch : MonoBehaviour
{
	[Serializable]
	public struct FingerData
	{
		public TouchState touchState;

		public Transform touchDownButton;

		public float touchDownTime;

		public Vector2 lastFingerPosPixel;
	}

	public enum TouchState
	{
		NotTouching = 0,
		OnEmpty = 1,
		OnUI = 2
	}

	[Header("Touch")]
	[Space(6f)]
	public FingerData[] touchesInfo = new FingerData[5];

	public int zoomFinger1 = -1;

	public int zoomFinger2 = -1;

	public void GetTouchInputs()
	{
		Double3 summedPositionDelta = Double3.zero;
		int dragingFingerCount = 0;
		Ref.inputController.horizontalAxis = 0f;
		Ref.inputController.rcsInput = Vector2.zero;
		for (int i = 0; i < Input.touchCount; i++)
		{
			int fingerId = Input.GetTouch(i).fingerId;
			if (Input.GetTouch(i).phase == TouchPhase.Ended || Input.GetTouch(i).phase == TouchPhase.Canceled)
			{
				EndTouch(fingerId, i, Input.GetTouch(i).position);
			}
			if (Input.GetTouch(i).phase == TouchPhase.Began)
			{
				StartTouch(fingerId, i, Input.GetTouch(i).position);
			}
			StayTouch(fingerId, i, ref summedPositionDelta, ref dragingFingerCount, ref Ref.inputController.horizontalAxis, ref Ref.inputController.rcsInput);
		}
		if (GetTouchIdFromFingerId(zoomFinger1) != -1 && GetTouchIdFromFingerId(zoomFinger2) != -1)
		{
			float num = Vector2.Distance(touchesInfo[zoomFinger1].lastFingerPosPixel, touchesInfo[zoomFinger2].lastFingerPosPixel);
			float num2 = Vector2.Distance(Input.GetTouch(GetTouchIdFromFingerId(zoomFinger1)).position, Input.GetTouch(GetTouchIdFromFingerId(zoomFinger2)).position);
			float num3 = num / num2;
			Ref.inputController.ApplyZoom((num3 - 1f) * 1.5f + 1f);
		}
		if (summedPositionDelta.x != 0.0 || summedPositionDelta.y != 0.0)
		{
			if (dragingFingerCount == 0)
			{
				dragingFingerCount = 1;
			}
			Ref.inputController.ApplyDraging(summedPositionDelta / dragingFingerCount);
		}
		for (int j = 0; j < Input.touchCount; j++)
		{
			touchesInfo[Input.GetTouch(j).fingerId].lastFingerPosPixel = Input.GetTouch(j).position;
		}
	}

	private void StartTouch(int fingerId, int i, Vector2 posPixel)
	{
		touchesInfo[fingerId].touchDownTime = Time.time;
		touchesInfo[fingerId].lastFingerPosPixel = posPixel;
		GameObject gameObject = Ref.inputController.PointCastUI(posPixel, Ref.inputController.uIColliders);
		if (gameObject != null)
		{
			touchesInfo[fingerId].touchState = TouchState.OnUI;
			touchesInfo[fingerId].touchDownButton = gameObject.transform;
			if (Ref.timeWarping && (gameObject.name == "Left Arrow Button" || gameObject.name == "Right Arrow Button"))
			{
				MenuManager.ShowMsg("Cannot turn while time warping");
			}
			return;
		}
		touchesInfo[fingerId].touchState = TouchState.OnEmpty;
		Vector2 posWorld = Camera.main.ScreenToWorldPoint((Vector3)posPixel + Vector3.forward * (0f - Ref.main.Camera.transform.position.z));
		Ref.inputController.StartTouchEmpty(posWorld, fingerId);
		int lastOnEmptyId = GetLastOnEmptyId(i);
		if (lastOnEmptyId != -1)
		{
			zoomFinger1 = Input.GetTouch(i).fingerId;
			zoomFinger2 = lastOnEmptyId;
		}
	}

	private int GetLastOnEmptyId(int i)
	{
		int result = -1;
		for (int j = 0; j < Input.touchCount; j++)
		{
			if (j != i && touchesInfo[Input.GetTouch(j).fingerId].touchState == TouchState.OnEmpty && (Ref.currentScene != Ref.SceneType.Build || HoldingPart.isNull(Build.main.holdingParts[Input.GetTouch(j).fingerId])))
			{
				result = Input.GetTouch(j).fingerId;
			}
		}
		return result;
	}

	private void EndTouch(int fingerId, int i, Vector2 posPixel)
	{
		bool flag = touchesInfo[fingerId].touchDownTime > Time.time - 0.2f;
		switch (touchesInfo[fingerId].touchState)
		{
		case TouchState.OnUI:
			if (flag)
			{
				Ref.inputController.ClickUI(posPixel);
			}
			break;
		case TouchState.OnEmpty:
			Ref.inputController.EndTouchEmpty(PixelPosToWorldPos(posPixel), fingerId, flag);
			break;
		}
		if (fingerId == zoomFinger1 || fingerId == zoomFinger2)
		{
			zoomFinger1 = -1;
			zoomFinger2 = -1;
		}
		touchesInfo[fingerId].touchState = TouchState.NotTouching;
	}

	private void StayTouch(int fingerId, int i, ref Double3 summedPositionDelta, ref int dragingFingerCount, ref float horizontalAxis, ref Vector2 rcsInput)
	{
		switch (touchesInfo[fingerId].touchState)
		{
		case TouchState.OnEmpty:
			if (Ref.currentScene == Ref.SceneType.Build)
			{
				Vector2 deltaPixel = touchesInfo[fingerId].lastFingerPosPixel - Input.GetTouch(i).position;
				Vector2 posWorld = Camera.main.ScreenToWorldPoint((Vector3)Input.GetTouch(i).position + Vector3.forward * (0f - Ref.main.Camera.transform.position.z));
				Ref.inputController.TouchStayEmpty(posWorld, deltaPixel, fingerId);
			}
			if (Ref.currentScene == Ref.SceneType.Game)
			{
				Vector2 vector = touchesInfo[fingerId].lastFingerPosPixel - Input.GetTouch(i).position;
				summedPositionDelta += (Vector3)vector;
				dragingFingerCount++;
			}
			break;
		case TouchState.OnUI:
		{
			if (touchesInfo[fingerId].touchDownButton == Ref.inputController.leftArrow)
			{
				horizontalAxis = -1f;
			}
			else if (touchesInfo[fingerId].touchDownButton == Ref.inputController.rightArrow)
			{
				horizontalAxis = 1f;
			}
			else if (touchesInfo[fingerId].touchDownButton == Ref.inputController.up)
			{
				rcsInput.y = 1f;
			}
			else if (touchesInfo[fingerId].touchDownButton == Ref.inputController.down)
			{
				rcsInput.y = -1f;
			}
			else if (touchesInfo[fingerId].touchDownButton == Ref.inputController.right)
			{
				rcsInput.x = 1f;
			}
			else if (touchesInfo[fingerId].touchDownButton == Ref.inputController.left)
			{
				rcsInput.x = -1f;
			}
			if (!(Ref.controller != null) || !(Ref.controller.throttlePercentUI != null) || !(touchesInfo[fingerId].touchDownButton == Ref.controller.throttlePercentUI.transform.parent))
			{
				break;
			}
			float num = touchesInfo[fingerId].lastFingerPosPixel.y - Input.GetTouch(i).position.y;
			if (num == 0f || !(Ref.mainVessel != null))
			{
				break;
			}
			if (Ref.mainVessel.controlAuthority)
			{
				Ref.mainVessel.SetThrottle(new Vessel.Throttle(Ref.mainVessel.throttle.throttleOn, Mathf.Clamp01(Ref.mainVessel.throttle.throttleRaw - num / 318f)));
				if (Ref.inputController.instructionSlideThrottleHolder.activeSelf)
				{
					Ref.inputController.instructionSlideThrottleHolder.SetActive(false);
					Ref.inputController.CheckAllInstructions();
				}
			}
			else if (MenuManager.main.msgText.color.a < 0.6f)
			{
				MenuManager.ShowMsg("失去控制");
			}
			break;
		}
		}
	}

	private int GetTouchIdFromFingerId(int finger)
	{
		for (int i = 0; i < Input.touchCount; i++)
		{
			if (Input.GetTouch(i).fingerId == finger)
			{
				return i;
			}
		}
		return -1;
	}

	public Vector2 PixelPosToWorldPos(Vector2 posPixel)
	{
		if (Ref.mapView && Ref.currentScene == Ref.SceneType.Game)
		{
			Double3 @double = Double3.ToDouble3((posPixel - new Vector2(Screen.width / 2, Screen.height / 2)) / Screen.height);
			float f = Ref.main.Camera.fieldOfView * ((float)Math.PI / 180f);
			float num = Mathf.Sin(f) / ((1f + Mathf.Cos(f)) * 0.5f);
			return (@double * (Ref.map.mapPosition.z / 10000.0) * num).RotateZ(Ref.main.Camera.transform.eulerAngles.z * ((float)Math.PI / 180f)).toVector2;
		}
		return Ref.main.Camera.ScreenToWorldPoint((Vector3)posPixel + Vector3.forward * (0f - Ref.main.Camera.transform.position.z));
	}
}
