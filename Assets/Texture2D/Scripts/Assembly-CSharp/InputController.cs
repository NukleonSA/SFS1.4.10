using System;
using System.Collections;
using System.Collections.Generic;
using NewBuildSystem;
using UnityEngine;
using UnityEngine.UI;

public class InputController : Touch
{
	[Serializable]
	public class UIColliders
	{
		public string grupName;

		public BoxCollider2D[] colliders;
	}

	[Header("Input Controller")]
	[Space(6f)]
	public CanvasScaler[] canvasScalers;

	[Space]
	public LayerMask maskPartHibox;

	[Space]
	public LayerMask partsCollider;

	[Space]
	public UIColliders[] uIColliders;

	[Space]
	[Space]
	public float horizontalAxis;

	public Vector2 rcsInput;

	public Transform leftArrow;

	public Transform rightArrow;

	public Transform up;

	public Transform down;

	public Transform left;

	public Transform right;

	public GameObject recoverMenuHolder;

	public GameObject instructionsPartsHolder;

	public GameObject instructionToggleThrottleHolder;

	public GameObject instructionSlideThrottleHolder;

	public GameObject instructionsMap;

	public GameObject instructionsTimewarp;

	public GameObject menuHolder;

	public MoveModule inputButtonsAnimation;

	[Space]
	public AudioSource clickSound;

	public AudioListener audioListener;
	public Soundtrack soundtrackController;

	public Transform resolutionReference;

	public MoveModule dropdownMenuAnimation;

	// 性能: LateUpdate 每帧都要开关左右箭头指示子物体, 缓存引用避免反复 GetChild
	private GameObject leftArrowIndicator;

	private GameObject rightArrowIndicator;

	private void Start()
	{
		SetFps();
		StartCoroutine(ReapplyFrameRateRoutine());   // Android 高刷模式异步生效, 需要重试
		StartCoroutine(FrameRateWatchdog());         // 防止自适应刷新率(ADFR)把帧率降回 60
		SetEnableMusic();
		SetEnableSound();
		Screen.sleepTimeout = -1;
		switch (Ref.currentScene)
		{
		case Ref.SceneType.MainMenu:
			Screen.orientation = ScreenOrientation.Portrait;
			break;
		case Ref.SceneType.Build:
			Screen.orientation = ScreenOrientation.Portrait;
			break;
		case Ref.SceneType.Game:
			Screen.orientation = (Saving.LoadSetting(Saving.SettingKey.disableAutoRotate) ? ScreenOrientation.Portrait : ScreenOrientation.AutoRotation);
			if (Ref.lastScene == Ref.SceneType.Build)
			{
				instructionToggleThrottleHolder.SetActive(true);
				if (!Saving.LoadSetting(Saving.SettingKey.seenGameInstructions))
				{
					instructionsPartsHolder.SetActive(true);
					instructionSlideThrottleHolder.SetActive(true);
				}
			}
			break;
		}
	}

	// 把帧率设置抽出来, 供多处调用
	public static void ApplyFrameRate()
	{
		int fps = Saving.LoadFps();
		// 1) 垂直同步会覆盖 targetFrameRate, 高帧率必须关闭它
		QualitySettings.vSyncCount = 0;
		// 2) 设定目标帧率
		Application.targetFrameRate = fps;
#if UNITY_ANDROID && !UNITY_EDITOR
		// 分工:
		//   30/60 档 -> 完全交给 Unity 的 Application.targetFrameRate(原版机制), 不做任何持续干预。
		//   90/120 档 -> 额外用原生 API 请求屏幕进入高刷模式(Unity 自己到不了这一层)。
		// 注意: 窗口的刷新率偏好是"粘性"的, 从 90/120 切回 30/60 时必须清除一次,
		//       否则屏幕会一直停在 120Hz, 导致 30/60 锁不住。清除之后就完全交给 Unity。
		if (fps > 60)
		{
			int targetFps = fps;

			// ① Unity 官方 API: Screen.SetResolution 第 4 个参数就是 preferredRefreshRate
			int curHz = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
			if (curHz < fps)
			{
				Screen.SetResolution(Screen.width, Screen.height, FullScreenMode.FullScreenWindow, new RefreshRate
				{
					numerator = (uint)fps,
					denominator = 1u
				});
			}

			// ② 原生: 请求屏幕刷新率 / 显示模式(高刷机型真正认这些)
			try
			{
				AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				activity.Call("runOnUiThread", new AndroidJavaRunnable(delegate
				{
					try
					{
						AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
						AndroidJavaObject attrs = window.Call<AndroidJavaObject>("getAttributes");
						attrs.Set("preferredRefreshRate", (float)targetFps);
						// 枚举显示模式, 取不超过目标帧率的最高档(SurfaceControl 相关调用见下)
						try
						{
							AndroidJavaObject wm = activity.Call<AndroidJavaObject>("getWindowManager");
							AndroidJavaObject disp = wm.Call<AndroidJavaObject>("getDefaultDisplay");
							AndroidJavaObject[] modes = disp.Call<AndroidJavaObject[]>("getSupportedModes");
							int bestId = -1;
							float bestHz = 0f;
							float maxHz = 0f;
							if (modes != null)
							{
								foreach (AndroidJavaObject m in modes)
								{
									float hz = m.Call<float>("getRefreshRate");
									int mid = m.Call<int>("getModeId");
									if (hz > maxHz) { maxHz = hz; }
									if (hz <= (float)targetFps + 0.5f && hz > bestHz) { bestHz = hz; bestId = mid; }
									m.Dispose();
								}
							}
							if (bestId >= 0)
							{
								attrs.Set("preferredDisplayModeId", bestId);
							}
							if (disp != null) { disp.Dispose(); }
							if (wm != null) { wm.Dispose(); }
						}
						catch (System.Exception e6)
						{
							UnityEngine.Debug.LogWarning("[FPS] preferredDisplayModeId 设置失败: " + e6.Message);
						}
						window.Call("setAttributes", attrs);
						attrs.Dispose();
						window.Dispose();
					}
					catch (System.Exception e2)
					{
						UnityEngine.Debug.LogWarning("[FPS] 请求高刷失败: " + e2.Message);
					}
				}));
				unityPlayer.Dispose();
			}
			catch (System.Exception e)
			{
				UnityEngine.Debug.LogWarning("[FPS] 高刷请求派发失败: " + e.Message);
			}
		}
		else
		{
			// 30/60 档: 每次都确保窗口刷新率偏好指向 60Hz 模式(幂等操作, 避免因首帧前调用过早而漏设),
			// 让屏幕落在 60Hz —— 这样 Unity 的 Application.targetFrameRate 才能干净分频, 精确锁 30/60。
			// 实测教训: 若把偏好清成"无偏好", 屏幕会落到设备默认的 120Hz, 此时 30 档只能锁到 60。
			// 锁定本身仍完全由 Unity 的 targetFrameRate 负责, 与原版机制一致。
			try
			{
				AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				activity.Call("runOnUiThread", new AndroidJavaRunnable(delegate
				{
					try
					{
						AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
						AndroidJavaObject attrs = window.Call<AndroidJavaObject>("getAttributes");
						attrs.Set("preferredRefreshRate", 60f);
						try
						{
							AndroidJavaObject wm = activity.Call<AndroidJavaObject>("getWindowManager");
							AndroidJavaObject disp = wm.Call<AndroidJavaObject>("getDefaultDisplay");
							AndroidJavaObject[] modes = disp.Call<AndroidJavaObject[]>("getSupportedModes");
							int bestId = -1;
							float bestHz = 0f;
							float bestDiff = float.MaxValue;
							if (modes != null)
							{
								foreach (AndroidJavaObject m in modes)
								{
									float hz = m.Call<float>("getRefreshRate");
									int mid = m.Call<int>("getModeId");
									float d = System.Math.Abs(hz - 60f);
									if (d < bestDiff) { bestDiff = d; bestId = mid; bestHz = hz; }
									m.Dispose();
								}
							}
							if (bestId >= 0)
							{
								attrs.Set("preferredDisplayModeId", bestId);
							}
							if (disp != null) { disp.Dispose(); }
							if (wm != null) { wm.Dispose(); }
						}
						catch (System.Exception e7)
						{
							UnityEngine.Debug.LogWarning("[FPS] 低档模式选择失败: " + e7.Message);
						}
						window.Call("setAttributes", attrs);
						attrs.Dispose();
						window.Dispose();
					}
					catch (System.Exception e)
					{
						UnityEngine.Debug.LogWarning("[FPS] 低档刷新率设置失败: " + e.Message);
					}
				}));
				unityPlayer.Dispose();
			}
			catch (System.Exception e)
			{
				UnityEngine.Debug.LogWarning("[FPS] 低档设置派发失败: " + e.Message);
			}
		}
					TryNativeFrameRateVote(fps);   // 向系统明确投票(FIXED_SOURCE), 让刷新率稳定
		#endif
		// 诊断: 便于在真机日志里确认(开发用)
	}

	// 向 Android 明确"本应用帧率固定为 fps"(FRAME_RATE_COMPATIBILITY_FIXED_SOURCE),
	// 这样系统合成层不会在 30/60 之间来回推断 —— 对应你要的"屏幕刷新率稳定在 30"。
	// 必须用 AndroidJNI 精确签名: Unity 的 Call<T>() 会按泛型返回类型匹配签名,
	// 而 setFrameRate/apply 返回 Transaction/void, 用它必然误报 NoSuchMethodError。
	private static int sLastVotedFps = -1;

	private static void TryNativeFrameRateVote(int fps)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		if (sLastVotedFps == fps)
		{
			return;
		}
		try
		{
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			if (activity == null)
			{
				unityPlayer.Dispose();
				return;
			}
			int want = fps;
			activity.Call("runOnUiThread", new AndroidJavaRunnable(delegate
			{
				AndroidJavaObject sc = null;
				try
				{
					// 遍历视图树找 Unity 的 SurfaceView, 取它的 SurfaceControl
					AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
					AndroidJavaObject decor = window.Call<AndroidJavaObject>("getDecorView");
					System.Collections.Generic.Queue<AndroidJavaObject> queue = new System.Collections.Generic.Queue<AndroidJavaObject>();
					queue.Enqueue(decor);
					int guard = 0;
					while (queue.Count > 0 && guard++ < 64 && sc == null)
					{
						AndroidJavaObject v = queue.Dequeue();
						try
						{
							AndroidJavaObject holder = v.Call<AndroidJavaObject>("getHolder");
							if (holder != null)
							{
								AndroidJavaObject surf = holder.Call<AndroidJavaObject>("getSurface");
								if (surf != null && surf.Call<bool>("isValid"))
								{
									try { sc = v.Call<AndroidJavaObject>("getSurfaceControl"); }
									catch (System.Exception) { sc = null; }
								}
								if (surf != null) { surf.Dispose(); }
								holder.Dispose();
							}
						}
						catch (System.Exception) { }
						if (sc != null) { v.Dispose(); break; }
						try
						{
							int cnt = v.Call<int>("getChildCount");
							for (int i = 0; i < cnt; i++)
							{
								queue.Enqueue(v.Call<AndroidJavaObject>("getChildAt", i));
							}
						}
						catch (System.Exception) { }
						v.Dispose();
					}
					if (sc == null)
					{
						try { sc = decor.Call<AndroidJavaObject>("getRootSurfaceControl"); }
						catch (System.Exception) { sc = null; }
					}
				}
				catch (System.Exception e)
				{
					UnityEngine.Debug.LogWarning("[FPS] 取 SurfaceControl 失败: " + e.Message);
				}
				if (sc == null)
				{
					UnityEngine.Debug.LogWarning("[FPS] 未取得 SurfaceControl, 无法向系统投票");
					return;
				}
				// 用精确签名调用 SurfaceControl.Transaction#setFrameRate + apply
				try
				{
					System.IntPtr scRaw = sc.GetRawObject();
					System.IntPtr txClass = AndroidJNI.FindClass("android/view/SurfaceControl$Transaction");
					if (txClass == System.IntPtr.Zero)
					{
						UnityEngine.Debug.LogWarning("[FPS] SurfaceControl$Transaction 类不存在");
						return;
					}
					System.IntPtr ctor = AndroidJNI.GetMethodID(txClass, "<init>", "()V");
					if (ctor == System.IntPtr.Zero)
					{
						UnityEngine.Debug.LogWarning("[FPS] Transaction 构造函数不存在");
						return;
					}
					System.IntPtr tx = AndroidJNI.NewObject(txClass, ctor, new jvalue[0]);
					System.IntPtr setMid = AndroidJNI.GetMethodID(txClass, "setFrameRate",
						"(Landroid/view/SurfaceControl;FII)Landroid/view/SurfaceControl$Transaction;");
					if (setMid == System.IntPtr.Zero)
					{
						UnityEngine.Debug.LogWarning("[FPS] Transaction.setFrameRate 签名不存在");
						return;
					}
					jvalue[] jargs = new jvalue[4];
					jargs[0].l = scRaw;
					jargs[1].f = (float)want;
					jargs[2].i = 1;   // FRAME_RATE_COMPATIBILITY_FIXED_SOURCE
					jargs[3].i = 0;   // CHANGE_FRAME_RATE_ALWAYS
					tx = AndroidJNI.CallObjectMethod(tx, setMid, jargs);
					System.IntPtr applyMid = AndroidJNI.GetMethodID(txClass, "apply", "()V");
					if (applyMid == System.IntPtr.Zero)
					{
						UnityEngine.Debug.LogWarning("[FPS] Transaction.apply()V 不存在");
						return;
					}
					AndroidJNI.CallVoidMethod(tx, applyMid, new jvalue[0]);
					sLastVotedFps = want;
				}
				catch (System.Exception e2)
				{
					UnityEngine.Debug.LogWarning("[FPS] 投票失败: " + e2.Message);
				}
				finally
				{
					sc.Dispose();
				}
			}));
			unityPlayer.Dispose();
		}
		catch (System.Exception e)
		{
			UnityEngine.Debug.LogWarning("[FPS] 投票派发失败: " + e.Message);
		}
#endif
	}

	public void SetFps()
	{
		ApplyFrameRate();
	}

	// 尽可能早地请求目标帧率(Android 需要在首帧前就请求高刷新率模式;
	// 另外 ProjectSettings 里的 Optimized Frame Pacing(Swappy) 已关闭,
	// 否则它会接管帧率调度并忽略 targetFrameRate)
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
	private static void InitFrameRateEarly()
	{
		ApplyFrameRate();
	}

	// Android 切换显示刷新率是异步的, 且太早调用(窗口还没建好)会被忽略 ——
	// 所以在启动后的一段时间内重试若干次, 直到显示刷新率达标或次数用尽。
	private System.Collections.IEnumerator ReapplyFrameRateRoutine()
	{
		for (int i = 0; i < 8; i++)
		{
			yield return new WaitForSeconds(i == 0 ? 0.1f : 0.5f);
			ApplyFrameRate();
			int target = Saving.LoadFps();
			int now = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
			if (target <= 60 || now >= target)
			{
				yield break;
			}
		}
	}

	// 看门狗: 高刷机型(尤其国产 ADFR 自适应刷新率)会在无触控/画面静止时把屏幕刷新率
	// 降回 60, 导致"不操作就掉回 60"。这里持续监测, 一旦被降下去就立刻重新请求。
	private System.Collections.IEnumerator FrameRateWatchdog()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(1f);
			int target = Saving.LoadFps();
			if (target <= 60)
			{
				continue;
			}
			int now = (int)System.Math.Round(Screen.currentResolution.refreshRateRatio.value);
			int actual = (Time.smoothDeltaTime > 0f) ? (int)(1f / Time.smoothDeltaTime) : 0;
			bool bad;
			if (target > 60)
			{
				bad = (now < target) || (actual < target - 5);          // 高刷档: 被降下来
			}
			else
			{
				bad = (actual > target + 8);                            // 30/60 档: 锁不住(跑太快)
			}
			if (bad)
			{
				ApplyFrameRate();
			}
		}
	}

	// 从后台恢复时重新应用(Android 可能重置显示模式)
	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			ApplyFrameRate();
		}
	}

	public void SetEnableMusic()
	{
		if (soundtrackController != null)
		{
			soundtrackController.gameObject.SetActive(!Saving.LoadSetting(Saving.SettingKey.disableMusic));
		}
	}

	public void SetEnableSound()
	{
		AudioListener.volume = ((!Saving.LoadSetting(Saving.SettingKey.disableSound)) ? 1 : 0);
	}

	public void CheckAllInstructions()
	{
		if (!instructionsPartsHolder.activeSelf && !instructionSlideThrottleHolder.activeSelf && !instructionToggleThrottleHolder.activeSelf)
		{
			Saving.SaveSetting(Saving.SettingKey.seenGameInstructions, true);
		}
	}

	private void LateUpdate()
	{
		GetTouchInputs();
		if (Ref.currentScene == Ref.SceneType.Game)
		{
			// 性能: 原来每帧各访问两次 GetChild(0).gameObject, 子物体是固定的, 缓存一次即可。
			if (leftArrowIndicator == null || rightArrowIndicator == null)
			{
				leftArrowIndicator = leftArrow.GetChild(0).gameObject;
				rightArrowIndicator = rightArrow.GetChild(0).gameObject;
			}
			if (leftArrowIndicator.activeSelf != (horizontalAxis == -1f))
			{
				leftArrowIndicator.SetActive(horizontalAxis == -1f);
			}
			if (rightArrowIndicator.activeSelf != (horizontalAxis == 1f))
			{
				rightArrowIndicator.SetActive(horizontalAxis == 1f);
			}
		}
		if (Input.mouseScrollDelta.y != 0f)
		{
			ApplyZoom(1f - Input.mouseScrollDelta.y / 10f);
		}
	}

	public void StartTouchEmpty(Vector2 posWorld, int fingerId)
	{
		switch (Ref.currentScene)
		{
		case Ref.SceneType.Build:
			Build.main.OnTouchStart(fingerId, posWorld);
			break;
		}
	}

	public void TouchStayEmpty(Vector2 posWorld, Vector2 deltaPixel, int fingerId)
	{
		switch (Ref.currentScene)
		{
		case Ref.SceneType.Build:
			Build.main.OnTouchStay(fingerId, posWorld, deltaPixel);
			break;
		}
	}

	public void EndTouchEmpty(Vector2 posWorld, int fingerId, bool click)
	{
		switch (Ref.currentScene)
		{
		case Ref.SceneType.Build:
			Build.main.OnTouchEnd(fingerId, posWorld);
			break;
		case Ref.SceneType.Game:
			if (click)
			{
				ClickEmpty(posWorld);
			}
			break;
		}
	}

	public bool ClickUI(Vector2 clickPosPixel)
	{
		if (Build.main != null)
		{
			Build.main.OnClickUI(clickPosPixel);
		}
		GameObject gameObject = PointCastUI(clickPosPixel, uIColliders);
		if (gameObject == null)
		{
			return false;
		}
		if (gameObject.transform.childCount > 0)
		{
			StartCoroutine(ClickGlow(gameObject.transform.GetChild(0).gameObject));
		}
		CustomEvent component = gameObject.GetComponent<CustomEvent>();
		if (component != null)
		{
			component.customEvent.Invoke();
			return true;
		}
		string text = gameObject.name;
		if (text != null && text == "Menu Back")
		{
			ClickedMenuBack(clickPosPixel);
		}
		return true;
	}

	private void ClickedMenuBack(Vector2 clickPosPixel)
	{
		Ref.saving.OnClickUI(clickPosPixel);
	}

	private void ClickEmpty(Vector2 clickPosWorld)
	{
		Ref.SceneType currentScene = Ref.currentScene;
		if (currentScene != Ref.SceneType.MainMenu && currentScene != Ref.SceneType.Build && currentScene == Ref.SceneType.Game)
		{
			if (Ref.mapView)
			{
				Ref.map.OnClickEmpty(clickPosWorld);
			}
			else if (Ref.mainVessel != null)
			{
				PointCastParts(clickPosWorld);
			}
		}
	}

	public GameObject PointCastUI(Vector2 clickPosPixel, UIColliders[] colliders)
	{
		foreach (UIColliders uIColliders in colliders)
		{
			GameObject gameObject = PointCastUI(clickPosPixel, uIColliders.colliders);
			if (gameObject != null)
			{
				return gameObject;
			}
		}
		return null;
	}

	public GameObject PointCastUI(Vector2 clickPosPixel, BoxCollider2D[] colliders)
	{
		float num = resolutionReference.position.x / 1000f;
		foreach (BoxCollider2D boxCollider2D in colliders)
		{
			if (boxCollider2D == null)
			{
				Debug.Log("No reference to collider");
			}
			else if (boxCollider2D.gameObject.activeInHierarchy)
			{
				Vector2 vector = (Vector2)boxCollider2D.GetComponent<RectTransform>().position + boxCollider2D.offset * num;
				Vector2 vector2 = vector - boxCollider2D.size * 0.5f * num;
				Vector2 vector3 = vector + boxCollider2D.size * 0.5f * num;
				if (clickPosPixel.x >= vector2.x && clickPosPixel.y >= vector2.y && clickPosPixel.x <= vector3.x && clickPosPixel.y <= vector3.y)
				{
					return boxCollider2D.gameObject;
				}
			}
		}
		return null;
	}

	public GameObject PointCastButtons(Vector2 mousePos, BoxCollider2D[] colliders)
	{
		foreach (BoxCollider2D boxCollider2D in colliders)
		{
			if (boxCollider2D.gameObject.activeInHierarchy && boxCollider2D.OverlapPoint(mousePos))
			{
				return boxCollider2D.gameObject;
			}
		}
		return null;
	}

	private void PointCastParts(Vector2 clickPos)
	{
		List<RaycastHit2D> list = new List<RaycastHit2D>(Physics2D.RaycastAll(clickPos, Vector2.up, 0.01f, maskPartHibox));
		if (list.Count == 0)
		{
			return;
		}
		Transform transform = GetTopPart(list);
		while (transform.GetComponent<Part>() == null)
		{
			transform = transform.parent;
		}
		Part component = transform.GetComponent<Part>();
		if (component.vessel != Ref.mainVessel)
		{
			MenuManager.ShowMsg("Cannot use a part on a rocket that you are not controlling");
			return;
		}
		if (!Ref.mainVessel.controlAuthority)
		{
			MenuManager.ShowMsg("No control. A rocket requires a capsule or a probe to be controllable");
			return;
		}
		component.UsePart();
		if (Ref.inputController.instructionsPartsHolder.activeSelf && component.HasEngineModule())
		{
			Ref.inputController.instructionsPartsHolder.SetActive(false);
			Ref.inputController.CheckAllInstructions();
		}
	}

	private Transform GetTopPart(List<RaycastHit2D> hitParts)
	{
		Transform transform = hitParts[0].collider.transform;
		for (int i = 0; i < hitParts.Count; i++)
		{
			if (hitParts[i].collider.transform.localPosition.z > transform.transform.localPosition.z)
			{
				transform = hitParts[i].collider.transform;
			}
		}
		return transform;
	}

	public void ToggleDropdownMenu()
	{
		dropdownMenuAnimation.GetComponent<MoveModule>().SetTargetTime((dropdownMenuAnimation.GetComponent<MoveModule>().targetTime.floatValue != 0f) ? 0f : 1.4f);
	}

	public void CloseDropdownMenu()
	{
		dropdownMenuAnimation.GetComponent<MoveModule>().SetTargetTime(0f);
	}

	public void ApplyZoom(float zoomDelta)
	{
		if (Ref.currentScene == Ref.SceneType.Game)
		{
			if (Ref.mapView)
			{
				Ref.map.UpdateMapZoom(Ref.map.mapPosition.z * (double)zoomDelta);
			}
			else
			{
				Ref.controller.SetCameraDistance(Mathf.Clamp(Ref.controller.cameraDistanceGame * zoomDelta, 8f, 2.5E+10f));
				Ref.planetManager.UpdateFog(Ref.controller.cameraDistanceGame);
			}
		}
		if (Ref.currentScene == Ref.SceneType.Build)
		{
			Build.main.MoveCamera(new Vector3(0f, 0f, (1f - zoomDelta) * 1.2f));
		}
	}

	public void ApplyDraging(Double3 posDeltaPixel)
	{
		if (Ref.currentScene == Ref.SceneType.Game)
		{
			if (Ref.mapView)
			{
				double num = Mathf.Cos(Ref.main.Camera.transform.eulerAngles.z * ((float)Math.PI / 180f));
				double num2 = Mathf.Sin(Ref.main.Camera.transform.eulerAngles.z * ((float)Math.PI / 180f));
				float f = Ref.main.Camera.fieldOfView * ((float)Math.PI / 180f);
				float num3 = Mathf.Sin(f) / ((1f + Mathf.Cos(f)) * 0.5f);
				Ref.map.UpdateMapPosition(Ref.map.mapPosition + new Double3(posDeltaPixel.x * num - posDeltaPixel.y * num2, posDeltaPixel.x * num2 + posDeltaPixel.y * num) * Ref.map.mapPosition.z / Screen.height * num3);
			}
			else
			{
				float f2 = Ref.main.Camera.fieldOfView * ((float)Math.PI / 180f);
				float num4 = Mathf.Sin(f2) / ((1f + Mathf.Cos(f2)) * 0.5f);
				Ref.controller.viewPositons += Utility.RotateZ(posDeltaPixel.toVector2, Ref.main.Camera.transform.eulerAngles.z * ((float)Math.PI / 180f)) * Ref.controller.cameraDistanceGame / Screen.height * num4;
			}
		}
	}

	public IEnumerator ClickGlow(GameObject glow)
	{
		if (glow.name == "Glow")
		{
			glow.SetActive(true);
			yield return new WaitForSeconds(0.2f);
			glow.SetActive(false);
		}
	}

	public void PlayClickSound(float volume)
	{
		clickSound.Play();
		clickSound.volume = volume;
	}
}
