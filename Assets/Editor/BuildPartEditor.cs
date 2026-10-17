using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 出包入口：一次会话连出 armv7/arm64 两个 APK，保持 com.sfs1410.pro / 1.4.10 身份（CLI 用法见 work/rv-yuanhang/run-build.ps1）。
public static class BuildPartEditor
{
    private const string OutputDir = "Builds";
    private const string BundleId = "com.sfs1410.pro";
    private const string BundleVersion = "1.4.10";

    // 外部工具链。Unity 2022.3 要求 NDK r23b + OpenJDK 11（JDK 17 会被判成 invalid）。
    private const string SdkRoot = @"D:\android-sdk";
    private const string NdkRoot = @"D:\android-ndk-r23b";
    private const string JdkRoot = @"D:\jdk-11";

    // 发布签名：两个包共用同一把 key，装一个换另一个无需先卸载。
    private const string KeystorePath = @"D:\2SFS Project\keys\sfs-release.jks";
    private const string KeystorePass = "SfsRelease2026!";
    private const string KeyAlias = "sfs-release";
    private const string KeyAliasPass = "SfsRelease2026!";

    // ------------------------------- 入口 -------------------------------

    /// <summary>CI 入口：连出 32 位与 64 位两个 APK。</summary>
    public static void BuildBothCLI()
    {
        int failures = 0;
        if (!Build(AndroidArchitecture.ARMv7, "armv7", "32-bit / armeabi-v7a"))
            failures++;
        if (!Build(AndroidArchitecture.ARM64, "arm64", "64-bit / arm64-v8a"))
            failures++;
        Debug.Log("[PartEditor14] === ALL DONE, failed builds = " + failures + " ===");
        if (failures > 0 && Application.isBatchMode)
            EditorApplication.Exit(1);
    }

    public static void BuildArmv7CLI() { Build(AndroidArchitecture.ARMv7, "armv7", "32-bit / armeabi-v7a"); }

    public static void BuildArm64CLI() { Build(AndroidArchitecture.ARM64, "arm64", "64-bit / arm64-v8a"); }

    [MenuItem("Tools/PartEditor14 — Build ARM64 APK")]
    public static void BuildArm64Menu() { Build(AndroidArchitecture.ARM64, "arm64", "64-bit / arm64-v8a"); }

    [MenuItem("Tools/PartEditor14 — Build ARMv7 APK")]
    public static void BuildArmv7Menu() { Build(AndroidArchitecture.ARMv7, "armv7", "32-bit / armeabi-v7a"); }

    // ------------------------------- 实现 -------------------------------

    private static bool Build(AndroidArchitecture arch, string tag, string human)
    {
        Debug.Log("[PartEditor14] ===== build " + human + " =====");
        try
        {
            ApplyExternalTools();
            ApplyPlayerSettings(arch);

            var scenes = CollectScenes();
            if (scenes == null)
                return false;

            string projRoot = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(projRoot, OutputDir);
            Directory.CreateDirectory(outDir);
            string apk = Path.Combine(outDir, "yuanhang-1.4.10-parteditor-" + tag + ".apk");
            if (File.Exists(apk))
                File.Delete(apk);

            Debug.Log("[PartEditor14] " + scenes.Length + " scenes -> " + apk);

            var bpo = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(bpo);
            BuildSummary s = report.summary;
            Debug.Log("[PartEditor14] " + tag + " result=" + s.result +
                      " errors=" + s.totalErrors +
                      " warnings=" + s.totalWarnings +
                      " size=" + s.totalSize +
                      " output=" + apk);
            return s.result == BuildResult.Succeeded;
        }
        catch (Exception e)
        {
            Debug.LogError("[PartEditor14] " + tag + " build threw: " + e);
            return false;
        }
    }

    /// <summary>写入 Unity 2022.3 的 Android 工具链首选项（AndroidSdkRoot / AndroidNdkRootR23B / AndroidJavaRoot），须在编辑器启动前生效，否则报 JDK not found。</summary>
    private static void ApplyExternalTools()
    {
        EditorPrefs.SetString("AndroidSdkRoot", SdkRoot);
        EditorPrefs.SetString("AndroidNdkRootR23B", NdkRoot);
        EditorPrefs.SetString("AndroidJavaRoot", JdkRoot);
        Debug.Log("[PartEditor14] tools: sdk=" + SdkRoot + " ndk=" + NdkRoot + " jdk=" + JdkRoot +
                  " (exists: " + Directory.Exists(SdkRoot) + "/" +
                  Directory.Exists(NdkRoot) + "/" + Directory.Exists(JdkRoot) + ")");
    }

    private static void ApplyPlayerSettings(AndroidArchitecture arch)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // 保持游戏原有身份，做出来就是可以直接替换的远航深洋 1.4.10
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleId);
        PlayerSettings.bundleVersion = BundleVersion;
        PlayerSettings.Android.bundleVersionCode = 1;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
        PlayerSettings.Android.targetArchitectures = arch;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)22;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)30;

        EditorUserBuildSettings.buildAppBundle = false;   // APK，不是 AAB
        // 不改纹理压缩子目标，沿用工程里各 TextureImporter 的原始设置。

        // 自定义密钥库：保证产物一定是已签名的可安装包（不依赖 Unity 的 debug 兜底）。
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = KeystorePath;
        PlayerSettings.Android.keystorePass = KeystorePass;
        PlayerSettings.Android.keyaliasName = KeyAlias;
        PlayerSettings.Android.keyaliasPass = KeyAliasPass;

        Debug.Log("[PartEditor14] arch=" + arch +
                  " scripting=" + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) +
                  " bundle=" + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
    }

    private static string[] CollectScenes()
    {
        var scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled)
                scenes.Add(s.path);
        if (scenes.Count == 0)
        {
            Debug.LogError("[PartEditor14] EditorBuildSettings has no enabled scenes.");
            return null;
        }
        return scenes.ToArray();
    }
}
