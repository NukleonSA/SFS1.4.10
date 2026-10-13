using UnityEngine;
using UnityEngine.SceneManagement;
using NewBuildSystem;

/// <summary>
/// 无限建造区域 —— 移植自 SFS 1.5
///
/// 1.5 的实现（SFS.Builds.qBkCHOmtMAavENkEXj）：
///     Vector2_Local size
///     SpriteRenderer  gridSprite / centerMarkerSprite
///     SpriteRenderer  border_Left / border_Right / border_Bottom / border_Top
///
///   UpdateBuildSpaceSize() @0x1634694:
///       size = GetOwnedGridSize(true); OHNPPBODLPJ()
///   OHNPPBODLPJ() @0x1634AA0 的布局:
///       gridSprite.position    = (0, 0)
///       centerMarker.position  = (size.x * 0.5, 0)
///       border_Left  .position = (-0.09,        size.y * 0.5)
///       border_Right .position = ( size.x + 50, size.y * 0.5)
///       border_Bottom.position = ( size.x * 0.5, -0.09)
///       border_Top   .position = ( size.x * 0.5, size.y + 50)
///   尺寸表 @0x17628A8: (14,35) (20,100) (120,240)
///
/// 网格贴图用的是 1.5 原版的 Build Background（400x400，含 4x4 格 + 虚线），
/// 已提取到 Resources/15build/Build_Background.png。
///
/// 渲染载体的差异：1.5 用 SpriteRenderer(Tiled) 平铺，但 1.4 的图块顶点上限是 65535，
/// 2048x2048 需要 52 万图块会直接崩。所以改成 4 顶点四边形 + 着色器按世界坐标采样，
/// 外观与 1.5 一致，且支持任意尺寸。中心线画在同一材质里，保证与网格同步缩放。
///
/// 不改动任何游戏脚本与场景，全部运行时注入，异常自动回退。
/// </summary>
public class UnlimitedBuildArea : MonoBehaviour
{
	// ---------- 配置 ----------

	/// <summary>总开关（默认开启）</summary>
	public static bool Enabled = true;

	/// <summary>无限模式</summary>
	public static bool Infinite = true;
	/// <summary>无限模式的网格尺寸</summary>
	public static Vector2 InfiniteSize = new Vector2(2048f, 2048f);
	/// <summary>有限模式的网格尺寸</summary>
	public static Vector2 BigSize = new Vector2(120f, 240f);
	/// <summary>每个网格单元的世界尺寸</summary>
	public static float CellSize = 1f;

	/// <summary>中心线颜色（半透明黑，直接叠在蓝图上）</summary>
	public static Color MarkerColor = new Color(0f, 0f, 0f, 0.15f);
	/// <summary>中心线宽度（世界单位）</summary>
	public static float MarkerWidth = 0.1f;

	/// <summary>网格外的边框</summary>
	public static Color BorderColor = new Color(1f, 1f, 1f, 0.22f);
	public static float BorderThickness = 0.08f;

	/// <summary>默认视角是否居中到蓝图中央</summary>
	public static bool CenterCameraOnStart = true;
	/// <summary>默认视角在居中基础上的偏移（单位：格）</summary>
	public static Vector2 CameraOffsetCells = new Vector2(4f, 0f);

	private static Vector2 ActiveSize { get { return Infinite ? InfiniteSize : BigSize; } }

	// ---------- 运行态 ----------

	private static UnlimitedBuildArea inst;
	private PartGrid grid;
	private Vector2 original;
	private bool haveOriginal;
	private bool applied;
	private int pendingCenterCamera;

	private GameObject root;
	private MeshRenderer gridRenderer, markerRenderer, bgRenderer, blR, brR, bbR, btR;
	private Material gridMat15, bgMat, borderMat, markerMat;

	private bool origSprite1, origSprite2;

	// ---------- 自举 ----------

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Bootstrap()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
		Ensure();
	}

	private static void OnSceneLoaded(Scene s, LoadSceneMode m) { Ensure(); }

	public static UnlimitedBuildArea Ensure()
	{
		if (inst != null) return inst;
		Build build = Build.main;
		if (build == null) return null;
		inst = build.gameObject.GetComponent<UnlimitedBuildArea>();
		if (inst == null) inst = build.gameObject.AddComponent<UnlimitedBuildArea>();
		return inst;
	}

	// ---------- 生命周期 ----------

	private void Awake() { if (inst == null) inst = this; }
	private void OnDestroy() { DoRestore(); DestroyVisual(); if (inst == this) inst = null; }
	private void OnApplicationQuit() { DoRestore(); }

	private void OnEnable()
	{
		// 默认开启：物体可用时立即生效
		if (Enabled) DoApply();
	}

	private void Update()
	{
		if (Enabled != applied)
		{
			if (Enabled) DoApply(); else DoRestore();
		}
		if (pendingCenterCamera > 0)
		{
			pendingCenterCamera--;
			if (pendingCenterCamera == 0) CenterCamera();
		}
	}

	// ---------- 核心 ----------

	private bool Ready()
	{
		if (grid == null)
		{
			Build build = Build.main;
			if (build == null) return false;
			grid = build.buildGrid;
		}
		return grid != null && grid.gridSize != null;
	}

	private void DoApply()
	{
		try
		{
			if (!Ready()) return;
			if (!haveOriginal)
			{
				original = grid.gridSize.full_value;
				haveOriginal = true;
				if (grid.gridSprite1 != null) origSprite1 = grid.gridSprite1.enabled;
				if (grid.gridSprite2 != null) origSprite2 = grid.gridSprite2.enabled;
			}
			// 1) 放开建造范围
			Vector2 target = ActiveSize;
			grid.gridSize.full_value = target;
			grid.gridSize.pc_value = target;
			// 2) 关掉 1.4 原来的两个网格精灵
			//    gridSprite1 = 格线（1 单位/格，放大会图块爆炸）
			//    gridSprite2 = 背景纸纹（布局按旧尺寸算，留着会错位）
			if (grid.gridSprite1 != null) grid.gridSprite1.enabled = false;
			if (grid.gridSprite2 != null) grid.gridSprite2.enabled = false;
			// 3) 按 1.5 的结构重建网格
			EnsureVisual();
			Layout(target);
			// 4) 相机远裁剪面要够大
			try
			{
				Camera cam = Camera.main;
				if (cam != null && cam.farClipPlane < 2000f) cam.farClipPlane = 5000f;
			}
			catch { }
			// 5) 默认视角
			if (CenterCameraOnStart) pendingCenterCamera = 3;
			applied = true;
		}
		catch (System.Exception)
		{
			// 视觉部分出错不影响建造范围本身
		}
	}

	private void DoRestore()
	{
		try
		{
			if (grid != null && grid.gridSize != null && haveOriginal)
			{
				grid.gridSize.full_value = original;
				grid.gridSize.pc_value = original;
				if (grid.gridSprite1 != null) grid.gridSprite1.enabled = origSprite1;
				if (grid.gridSprite2 != null) grid.gridSprite2.enabled = origSprite2;
				grid.SetBuildGridSize();
			}
		}
		catch { }
		DestroyVisual();
		applied = false;
	}

	/// <summary>把主相机移到蓝图中央 + 偏移（保留 zoom 距离）</summary>
	private void CenterCamera()
	{
		try
		{
			Camera cam = Camera.main;
			if (cam == null || grid == null || grid.gridSize == null) return;
			Vector2 s = grid.gridSize.Value;
			Vector3 p = cam.transform.position;
			float cell = Mathf.Max(0.0001f, CellSize);
			float cx = Mathf.Clamp(s.x * 0.5f + CameraOffsetCells.x * cell, 0f, s.x);
			float cy = Mathf.Clamp(s.y * 0.5f + CameraOffsetCells.y * cell, 0f, s.y);
			cam.transform.position = new Vector3(cx, cy, p.z);
		}
		catch { }
	}

	// ---------- 视觉 ----------

	private static Shader FindShader(string name)
	{
		Shader sh = Shader.Find(name);
		if (sh != null) return sh;
		Shader[] all = Resources.FindObjectsOfTypeAll<Shader>();
		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] != null && all[i].name == name) return all[i];
		}
		return null;
	}

	/// <summary>
	/// 加载纯色材质。
	/// 关键：优先用 Resources 里的 **材质资源**（Resources/15build/UnlimitedSolid.mat）。
	/// 因为 Unity 打包时会剥离"没有资源引用"的着色器，纯 Shader.Find 在手机上会返回 null
	/// -> 蓝图完全不渲染。材质资源引用了着色器，所以必然一起打包。
	/// </summary>
	private static Material MakeSolidMat(string shaderName, Color c, string matName)
	{
		Material asset = Resources.Load<Material>("15build/UnlimitedSolid");
		if (asset != null)
		{
			Material m = new Material(asset);
			m.name = matName;
			m.hideFlags = HideFlags.HideAndDontSave;
			m.SetColor("_Color", c);
			return m;
		}
		Shader sh = FindShader(shaderName);
		if (sh == null) return null;
		Material m2 = new Material(sh);
		m2.name = matName;
		m2.hideFlags = HideFlags.HideAndDontSave;
		m2.SetColor("_Color", c);
		return m2;
	}

	private MeshRenderer MakeRenderer(string name, Material mat, out Mesh mesh, int orderOffset, float z)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(root.transform, false);
		go.layer = grid.gameObject.layer;
		MeshFilter mf = go.AddComponent<MeshFilter>();
		MeshRenderer mr = go.AddComponent<MeshRenderer>();
		mesh = new Mesh();
		mesh.name = name + "Mesh";
		mf.sharedMesh = mesh;
		mr.sharedMaterial = mat;
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.receiveShadows = false;
		if (grid.gridSprite1 != null)
		{
			mr.sortingLayerID = grid.gridSprite1.sortingLayerID;
			mr.sortingOrder = grid.gridSprite1.sortingOrder + orderOffset;
		}
		return mr;
	}

	private void EnsureVisual()
	{
		if (root != null) return;

		// 背景 / 边框 / 中心线用纯色着色器
		bgMat = MakeSolidMat("SFS/SolidQuad", new Color(0.10f, 0.16f, 0.26f, 0.35f), "UnlimitedBg");
		borderMat = MakeSolidMat("SFS/SolidQuad", BorderColor, "UnlimitedBorder");
		markerMat = MakeSolidMat("SFS/SolidQuad", MarkerColor, "UnlimitedMarker");

		root = new GameObject("UnlimitedBuildSpace");
		root.transform.SetParent(grid.transform, false);
		root.layer = grid.gameObject.layer;
		root.transform.localPosition = Vector3.zero;

		// 背景
		Mesh bgMesh;
		bgRenderer = MakeRenderer("BG", bgMat, out bgMesh, -1, 0.02f);
		SetQuad(bgRenderer, bgMesh);

		// 网格（1.5 原版贴图 + 着色器 UV；中心线也在这个材质里）
		MakeShaderGrid();

		// 边框
		Mesh m1, m2, m3, m4;
		blR = MakeRenderer("Border_Left", borderMat, out m1, 3, 0f);
		brR = MakeRenderer("Border_Right", borderMat, out m2, 3, 0f);
		bbR = MakeRenderer("Border_Bottom", borderMat, out m3, 3, 0f);
		btR = MakeRenderer("Border_Top", borderMat, out m4, 3, 0f);
		SetQuad(blR, m1); SetQuad(brR, m2); SetQuad(bbR, m3); SetQuad(btR, m4);
	}

	/// <summary>
	/// 网格 = 1.5 的 Build Background 贴图，4 顶点四边形 + 着色器按世界坐标采样。
	/// 中心线由同一材质的 _CenterX / _CenterWidth / _CenterColor 绘制，
	/// 保证与网格完全同步（缩放时一起变化）。
	/// </summary>
	private void MakeShaderGrid()
	{
		Texture2D tex = Resources.Load<Texture2D>("15build/Build_Background");
		if (tex == null)
		{
			Texture2D[] all = Resources.FindObjectsOfTypeAll<Texture2D>();
			for (int i = 0; i < all.Length; i++)
			{
				if (all[i] != null && all[i].name == "Build_Background") { tex = all[i]; break; }
			}
		}
		if (tex == null) return;

		// 优先用 Resources 里的材质资源（保证着色器被打包进手机版本）
		Material asset = Resources.Load<Material>("15build/UnlimitedGrid15");
		if (asset != null)
		{
			gridMat15 = new Material(asset);
		}
		else
		{
			Shader sh = FindShader("SFS/BuildGrid15");
			if (sh == null) return;
			gridMat15 = new Material(sh);
			gridMat15.SetTexture("_MainTex", tex);
		}
		tex.wrapMode = TextureWrapMode.Repeat;
		gridMat15.name = "UnlimitedGrid15";
		gridMat15.hideFlags = HideFlags.HideAndDontSave;
		gridMat15.SetTexture("_MainTex", gridMat15.GetTexture("_MainTex") != null ? gridMat15.GetTexture("_MainTex") : tex);
		gridMat15.SetColor("_Tint", Color.white);

		Mesh mesh;
		gridRenderer = MakeRenderer("Grid15", gridMat15, out mesh, 2, 0f);
		SetQuad(gridRenderer, mesh);
	}

	/// <summary>统一用中心枢轴的 1x1 四边形（顶点 -0.5..0.5）</summary>
	private static void SetQuad(MeshRenderer mr, Mesh mesh)
	{
		if (mesh == null || mr == null) return;
		Vector3[] v =
		{
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3( 0.5f, -0.5f, 0f),
			new Vector3( 0.5f,  0.5f, 0f),
			new Vector3(-0.5f,  0.5f, 0f),
		};
		Vector2[] uv =
		{
			new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
		};
		int[] tris = { 0, 1, 2, 0, 2, 3 };
		mesh.Clear();
		mesh.vertices = v;
		mesh.uv = uv;
		mesh.triangles = tris;
		mesh.RecalculateBounds();
	}

	/// <summary>按 1.5 的布局刷新</summary>
	private void Layout(Vector2 size)
	{
		if (root == null) return;
		float half = 0.5f;

		// 网格 + 中心线（同一材质）
		if (gridRenderer != null && gridMat15 != null)
		{
			gridMat15.SetFloat("_TexWorldSize", 4f * Mathf.Max(0.01f, CellSize));
			gridMat15.SetColor("_Tint", Color.white);
			gridMat15.SetFloat("_CenterX", size.x * half);
			gridMat15.SetFloat("_CenterWidth", Mathf.Max(0f, MarkerWidth));
			gridMat15.SetColor("_CenterColor", MarkerColor);
			gridRenderer.transform.localPosition = new Vector3(size.x * half, size.y * half, gridRenderer.transform.localPosition.z);
			gridRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
		}

		// 背景铺满
		SetArea(bgRenderer, size);

		// 边框
		if (borderMat != null) borderMat.SetColor("_Color", BorderColor);
		float t = Mathf.Max(0.01f, BorderThickness);
		SetRect(blR, new Vector2(0f, size.y * half), new Vector2(t, size.y), 0.01f);
		SetRect(brR, new Vector2(size.x, size.y * half), new Vector2(t, size.y), 0.01f);
		SetRect(bbR, new Vector2(size.x * half, 0f), new Vector2(size.x, t), 0.01f);
		SetRect(btR, new Vector2(size.x * half, size.y), new Vector2(size.x, t), 0.01f);
	}

	private static void SetArea(MeshRenderer mr, Vector2 size)
	{
		if (mr == null) return;
		mr.transform.localPosition = new Vector3(size.x * 0.5f, size.y * 0.5f, mr.transform.localPosition.z);
		mr.transform.localScale = new Vector3(size.x, size.y, 1f);
	}

	/// <summary>中心枢轴：localPosition 即几何中心</summary>
	private static void SetRect(MeshRenderer mr, Vector2 center, Vector2 size, float z)
	{
		if (mr == null) return;
		mr.transform.localPosition = new Vector3(center.x, center.y, z);
		mr.transform.localScale = new Vector3(size.x, size.y, 1f);
	}

	private void DestroyVisual()
	{
		if (gridMat15 != null) { DestroyIt(gridMat15); gridMat15 = null; }
		if (bgMat != null) { DestroyIt(bgMat); bgMat = null; }
		if (borderMat != null) { DestroyIt(borderMat); borderMat = null; }
		if (markerMat != null) { DestroyIt(markerMat); markerMat = null; }
		if (root != null) { DestroyImmediate(root); root = null; }
		gridRenderer = markerRenderer = bgRenderer = blR = brR = bbR = btR = null;
	}

	private static void DestroyIt(Object o)
	{
		if (o == null) return;
		if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
	}
}
