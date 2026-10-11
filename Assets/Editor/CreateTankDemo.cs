using UnityEngine;
using UnityEditor;

public static class CreateTankDemo
{
    [MenuItem("Tools/Create TankDemo (self-made tank)")]
    public static void Create()
    {
        // 找 TankDemo 精灵
        string[] guids = AssetDatabase.FindAssets("TankDemo t:Sprite");
        if (guids.Length == 0) { Debug.LogWarning("未找到 TankDemo Sprite"); return; }
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        var go = new GameObject("TankDemo (selfmade)");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.color = Color.white;
        go.transform.position = Vector3.zero;
        go.transform.localScale = new Vector3(2, 2, 1);
        Selection.activeGameObject = go;
        Debug.Log("已创建自搓油箱 TankDemo, 大小 " + spr.rect.width + "x" + spr.rect.height);
    }
}
