using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 反编译工程移植到 Unity 2018.4 时，com.unity.purchasing@2.0.3 包解析为不完整 stub，
// 缺少 UnityEngine.Purchasing 核心类型。IAP 仅用于售卖 DLC（与颜色/皮肤 mod 无关），
// 故改为最小桩类，保留类名与序列化字段，避免编译失败。购买/DLC 功能在 mod 中关闭。
public class IAP : MonoBehaviour
{
	public static IAP main;

	public static string PARTS_EXPANSION_ID = "parts_expansion";

	public UnityEvent onComplete;

	public bool invoke;

	public Text msgText;
}
