using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Sprites/Shaded shader alpha 验证（批处理/会话内双模式）：
///   批处理: Unity.exe -batchmode -quit -projectPath &lt;proj&gt; -executeMethod ShaderVerify.Run -logFile &lt;log&gt;
///   会话内: 菜单 Tools/Shader Verify（经 Unity MCP bridge 执行），不退出编辑器
///
/// 渲染两个四边形（顶点 alpha = 1.0 / 0.5），shade 贴图左半 alpha=0（无阴影）、右半 alpha=1（全阴影），
/// 断言「阴影只让颜色变深，不改变透明度」且「纹理alpha×顶点alpha 正确输出」。
///   result.a = col.a（= texAlpha * vertexAlpha，不含 shadeFactor）
///   - 旧公式 shadeFactor*col.a：右半阴影处 a≈0 → FAIL
///   - 上一版 i.color.a：忽略纹理alpha，图集透明边距被实心渲染（发动机两侧黑齿）→ 本测试用白纹理无法覆盖，
///     黑齿回归由 engine_render 测试（capture_screenshot + 边缘分析）覆盖。
/// </summary>
public static class ShaderVerify
{
    const string OutDir = @"C:\Users\31005\AppData\Local\Temp\opencode\shader_verify";
    const float Intensity = 1.45f; // 原版 Parts 材质值

    [MenuItem("Tools/Shader Verify")]
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(OutDir);

            Shader sh = Shader.Find("Sprites/Shaded");
            if (sh == null) { Fail("shader-not-found"); return; }
            if (ShaderUtil.ShaderHasError(sh)) { Fail("shader-compile-error"); return; }

            // _MainTex: 8x8 不透明白
            Texture2D main = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var wc = new Color[64];
            for (int i = 0; i < 64; i++) wc[i] = Color.white;
            main.SetPixels(wc);
            main.wrapMode = TextureWrapMode.Clamp;
            main.filterMode = FilterMode.Point;
            main.Apply();

            // _ShadeTex: 左半 alpha=0（无阴影），右半 alpha=1（全阴影）
            Texture2D shade = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var sc = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    sc[y * 64 + x] = new Color(0, 0, 0, x < 32 ? 0f : 1f);
            shade.SetPixels(sc);
            shade.wrapMode = TextureWrapMode.Clamp;
            shade.filterMode = FilterMode.Point;
            shade.Apply();

            Material mat = new Material(sh);
            mat.SetTexture("_MainTex", main);
            mat.SetTexture("_ShadeTex", shade);
            mat.SetFloat("_Intensity", Intensity);

            // 两个四边形：上=顶点alpha1.0，下=顶点alpha0.5
            Mesh mesh = BuildMesh();

            // 隔离到远处，避免与当前场景中的对象重叠（相机视野内只留测试四边形）
            var obj = new GameObject("SV_Obj");
            obj.transform.position = new Vector3(2000f, 2000f, 0f);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = mat;

            var camGo = new GameObject("SV_Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1f;
            cam.transform.position = new Vector3(2000f, 2000f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D read = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            read.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(Path.Combine(OutDir, "render.png"), read.EncodeToPNG());

            // 采样点（3x3 平均）：
            //   上排 y=192：左 (70) 无阴影 / 右 (186) 全阴影，顶点alpha=1
            //   下排 y=64 ：左/右同上，顶点alpha=0.5
            Color topL = Avg(read, 70, 192);
            Color topR = Avg(read, 186, 192);
            Color botL = Avg(read, 70, 64);
            Color botR = Avg(read, 186, 64);

            Debug.Log(string.Format("SHADER_VERIFY topL a={0:0.000} rgb=({1:0.00},{2:0.00},{3:0.00})", topL.a, topL.r, topL.g, topL.b));
            Debug.Log(string.Format("SHADER_VERIFY topR a={0:0.000} rgb=({1:0.00},{2:0.00},{3:0.00})", topR.a, topR.r, topR.g, topR.b));
            Debug.Log(string.Format("SHADER_VERIFY botL a={0:0.000} rgb=({1:0.00},{2:0.00},{3:0.00})", botL.a, botL.r, botL.g, botL.b));
            Debug.Log(string.Format("SHADER_VERIFY botR a={0:0.000} rgb=({1:0.00},{2:0.00},{3:0.00})", botR.a, botR.r, botR.g, botR.b));

            bool ok = true;
            // 对照组：无阴影处正常
            ok &= Check("topL.alpha==1", Mathf.Abs(topL.a - 1f) <= 0.03f);
            // 预乘输出: a=1 时 rgb 不变
            ok &= Check("topL.rgb==1(unshaded bright, a=1)", topL.r >= 0.97f && topL.g >= 0.97f && topL.b >= 0.97f);
            ok &= Check("botL.alpha==0.5", Mathf.Abs(botL.a - 0.5f) <= 0.03f);
            // 预乘: a=0.5 -> rgb 读回 0.5
            ok &= Check("botL.rgb==0.5 (premultiplied by col.a)", Mathf.Abs(botL.r - 0.5f) <= 0.03f && Mathf.Abs(botL.g - 0.5f) <= 0.03f && Mathf.Abs(botL.b - 0.5f) <= 0.03f);
            // 关键断言：全阴影处 alpha 不被阴影压低（col.a 不含 shadeFactor）
            ok &= Check("topR.alpha==1 (shade must NOT lower alpha)", Mathf.Abs(topR.a - 1f) <= 0.03f);
            ok &= Check("botR.alpha==0.5 (shade must NOT lower alpha)", Mathf.Abs(botR.a - 0.5f) <= 0.03f);
            // 阴影仍然生效：颜色变深
            ok &= Check("topR.rgb==0 (shade darkens)", topR.r <= 0.03f && topR.g <= 0.03f && topR.b <= 0.03f);
            ok &= Check("botR.rgb==0 (shade darkens)", botR.r <= 0.03f && botR.g <= 0.03f && botR.b <= 0.03f);

            Debug.Log(ok ? "SHADER_VERIFY: PASS" : "SHADER_VERIFY: FAIL");

            UnityEngine.Object.DestroyImmediate(read);
            UnityEngine.Object.DestroyImmediate(main);
            UnityEngine.Object.DestroyImmediate(shade);
            UnityEngine.Object.DestroyImmediate(mat);
            UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(camGo);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogError("SHADER_VERIFY: FAIL exception " + e);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    static void Fail(string why)
    {
        Debug.LogError("SHADER_VERIFY: FAIL " + why);
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
    }

    static bool Check(string name, bool cond)
    {
        Debug.Log("SHADER_VERIFY " + (cond ? "OK   " : "BAD  ") + name);
        return cond;
    }

    static Color Avg(Texture2D t, int cx, int cy)
    {
        float r = 0, g = 0, b = 0, a = 0;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                Color c = t.GetPixel(cx + dx, cy + dy);
                r += c.r; g += c.g; b += c.b; a += c.a;
            }
        return new Color(r / 9f, g / 9f, b / 9f, a / 9f);
    }

    static Mesh BuildMesh()
    {
        // 上四边形 y 0.05..0.95（顶点alpha 1.0），下四边形 y -0.95..-0.05（顶点alpha 0.5），x -0.9..0.9
        var verts = new List<Vector3>();
        var uvs = new List<Vector3>();   // uv1: xy*2 + z=1，配合 shader 的 /(z+1) 还原为 0..1
        var cols = new List<Color>();
        var tris = new List<int>();

        AddQuad(verts, uvs, cols, tris, -0.9f, 0.9f, 0.05f, 0.95f, new Color(1, 1, 1, 1f));
        AddQuad(verts, uvs, cols, tris, -0.9f, 0.9f, -0.95f, -0.05f, new Color(1, 1, 1, 0.5f));

        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetUVs(1, uvs); // shade 通道同 UV
        m.SetColors(cols);
        m.SetTriangles(tris, 0);
        m.UploadMeshData(false);
        return m;
    }

    static void AddQuad(List<Vector3> v, List<Vector3> uv, List<Color> c, List<int> t,
                        float x0, float x1, float y0, float y1, Color col)
    {
        int b = v.Count;
        // 左下 (u=0,v=0)、右下 (1,0)、右上 (1,1)、左上 (0,1)
        Vector2[] p = { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };
        Vector2[] u = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        for (int i = 0; i < 4; i++)
        {
            v.Add(new Vector3(p[i].x, p[i].y, 0));
            uv.Add(new Vector3(u[i].x * 2f, u[i].y * 2f, 1f)); // / (z+1) = /2 → 采样 0..1
            c.Add(col);
        }
        t.Add(b + 0); t.Add(b + 1); t.Add(b + 2);
        t.Add(b + 0); t.Add(b + 2); t.Add(b + 3);
    }
}
