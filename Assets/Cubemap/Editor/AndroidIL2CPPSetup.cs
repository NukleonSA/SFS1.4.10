using UnityEngine;
using UnityEditor;

// 用菜单项直接设置 Android 的 IL2CPP + ARM64，绕开会因 Apple/iOS 报错而崩的 Player Settings 界面。
public static class AndroidIL2CPPSetup
{
    [MenuItem("Tools/Set IL2CPP + ARM64 (64-bit)")]
    public static void SetupAndroid64()
    {
        // Android 平台脚本后端 -> IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        // 目标架构 -> 仅 ARM64（如需兼容 32 位可再加 AndroidArchitecture.ARMv7）
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // API 级别（用已装最高的 android-26）
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;

        AssetDatabase.SaveAssets();
        Debug.Log("[AndroidIL2CPPSetup] 已设置: IL2CPP + ARM64 (64-bit)。请回 Build Settings 检查并 Export/Build。");
    }
}
