using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public static class Build64
{
    [MenuItem("Tools/Build 64-bit APK (IL2CPP ARM64)")]
    public static void Build()
    {
        // 注册表已设 JDK/SDK/NDK; 确保用用户 JDK(非内置)
        EditorPrefs.SetString("JdkPath", @"C:\Users\31005\Unity\jdk11\jdk-11.0.32.1+1");
        EditorPrefs.SetString("AndroidSdkRoot", @"C:\Users\31005\Unity\android-sdk");
        EditorPrefs.SetString("AndroidNdkRoot", @"C:\Users\31005\Unity\android-sdk\ndk\23.2.8568313");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.sfs14.pro");
        PlayerSettings.bundleVersion = "1.4.1Pro";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)22;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)30;

        string outDir = "D:/Users/31005/Desktop/SFS/_build64_apk";
        Directory.CreateDirectory(outDir);
        string apk = Path.Combine(outDir, "SFS1.4Pro_64.apk");

        var scenes = new string[] {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Build.unity",
            "Assets/Scenes/Game.unity"
        };

        BuildPlayerOptions bpo = new BuildPlayerOptions();
        bpo.scenes = scenes;
        bpo.locationPathName = apk;
        bpo.target = BuildTarget.Android;
        bpo.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(bpo);
        Debug.Log("Build result: " + report.summary.result + " errors=" + report.summary.totalErrors + " path=" + apk);
    }
}
