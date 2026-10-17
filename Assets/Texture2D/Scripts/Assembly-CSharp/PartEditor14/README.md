# PartEditor14 — 远航深洋 1.4.10 内置移植

从 1.4.06 版 PartEditor 移植而来
（源：`github.com/Arctic-Ice67/SpaceflightSimulator-1406PartEditor`，
1.4.06 是 Unity 2017.4.1f1 + Mono，靠 Mono.Cecil 补 `Assembly-CSharp.dll` + 外挂 `PartEditor14.dll`）。

**本工程有源码，所以不再需要那套补丁/注入机制**：代码直接编进 `Assembly-CSharp`，
两个集成点改成源码改动（见下）。UI 用 Unity 自带的 `OnGUI`，观感与 1.4.06 版一致。

## 文件

| 文件 | 来源 | 改动 |
|---|---|---|
| `PartEditorUI.cs` | 1.4.06 版 | **1 处**：`_target.partData.name` → `displayName`（1.4.10 把该字段改名了）；另加**小数尺寸**一节与 Scale 步进 |
| `TransformEditor.cs` | 1.4.06 版 | 缩放接口由 `SetScaleX/Y(int)` 换成 `SetWidth/SetHeight(float)`（小数尺寸，见下文） |
| `Bootstrap.cs` | 1.4.06 版 | 无 |
| `InputBlocker.cs` | 1.4.06 版 | 无 |

### 为「小数尺寸」额外改动的**游戏源码**（不是 mod 文件）

| 文件 | 改动 |
|---|---|
| `NewBuildSystem/Orientation.cs` | 新增 `sxf/syf` 字段 + `Mx/My/SizeX/SizeY`；`Vector2/Vector3 operator *`、`ApplyOrientation`、`DeepCopy`、`+`/`-` 接入小数倍率 |
| `EngineModule.cs` | 推力面积 / 质心偏移改用 `SizeX/SizeY`（2 处） |
| `SeparatorModule.cs` | 分离力改用 `SizeX/SizeY`（1 处） |

`Mx/My == 1` 时所有改动点的结果与原版**逐位一致**（乘 1f 是精确运算），
所以未缩放零件的物理与渲染行为零回归。

## 两个集成点（= 1.4.06 里 Cecil 补的那两处）

1. `Assets/Texture2D/Scripts/Assembly-CSharp/NewBuildSystem/Build.cs`
   的 `private void Start()` 开头：
   ```csharp
   PartEditor14.Bootstrap.Attach(this);
   ```
   （1.4.06 是 Cecil 往这里注入同一句）

2. `Assets/Texture2D/Scripts/Assembly-CSharp/Touch.cs`
   的 `private void StartTouch(int fingerId, int i, Vector2 posPixel)` 开头：
   ```csharp
   if (PartEditor14.InputBlocker.IsOverUI(posPixel)) {
       touchesInfo[fingerId].touchState = TouchState.NotTouching;
       return;
   }
   ```

## 与 1.4.06 的 API 漂移（已逐条核对本工程源码）

| 1.4.06 用法 | 1.4.10 | 状态 |
|---|---|---|
| `PartData.name` | `PartData.displayName` | ⚠️ 已改 |
| `Touch.StartTouch(int i, int fingerId, Vector2 posPixel)` | `(int fingerId, int i, Vector2)` —— **前两参顺序相反** | 已在源码里适配（补丁写在方法内部，不受影响） |
| `PlacedPart.AppliedPartData.HitboxMesh` | 同（嵌套类） | ✓ |
| `PlacedPart.partIcon / position / orientation / selected / skinId / partData / appliedData / NextSkin()` | 同 | ✓ |
| `Orientation.x / y / z`（int）+ `FlipX/FlipY` + `operator *(Vector2, Orientation)` | 同 | ✓ |
| `PartGrid.GetSelectedParts / CreateUndoStep / changed / parts / gridSize.Value` | 同 | ✓ |
| `PartGrid.LoadIcon(9 参)` | 同序同义 | ✓ |
| `PartData.skins.skins`（`Skin[]`）, `Skin.name` | 同（`Skin : ScriptableObject`，`name` 来自 Object） | ✓ |
| `Utility.ApplySkin(Transform, PartData, int, Orientation)` | 同 | ✓ |
| `Ref.currentScene` / `Ref.SceneType.Build` | 同 | ✓ |

## 小数尺寸缩放（本移植新增）

原 1.4.06 版的 `SetScaleX/Y(int)` 是**直接改 `orientation.x/y`**，只能整数格；
而 `x/y` 是 int，同时承担「整数格数 + 翻转符号 + 角度运算输入」，**不能改成 float**
（会破坏蓝图/存档序列化、`Rotate90`/`FlipX`/`FlipY` 的整数语义、网格吸附）。

做法：`x/y` 保持整数，另开浮点倍率，在**使用点**乘进去。

```
有效尺寸 = |x| * Mx     （Mx = orientation.sxf，1 = 原始尺寸）
```

| 接入点 | 效果 |
|---|---|
| `operator *(Vector2/Vector3, Orientation)` | hitbox 顶点、连接面、旋转中心、开关点、吸附点、`GetWorldPosition` 全部跟着小数缩放 |
| `Orientation.ApplyOrientation` | 建造/飞行两侧的 `localScale`（`PartGrid.LoadIcon`、`CreateRocket`、`GameSaver`、`PartsManager` 都走它） |
| `SizeX/SizeY` | 飞行物理：`EngineModule` 推力面积与质心偏移、`SeparatorModule` 分离力 |
| `DeepCopy` | 撤销步、从零件栏拖出、进世界、`PlacedPart` 拷贝构造 |

**为什么默认 0 而不是 1**：蓝图/存档走 `ObjectFile.FlushJUTILObject` / `CastFromJUTIL`，
底层是 `JsonUtility`——只序列化**字段**（属性不会被写进 `.bp`）。旧蓝图没有 `sxf/syf`，
反序列化后是 `0`；所有读取都经 `Mx/My`（`<=0` / `NaN` / 无穷 一律视为 1），
于是「未设置」和「显式设为 1」走同一条安全路径，**老存档不会因为缺字段把零件缩成 0 而消失**。

UI：`-- Scale 尺寸(小数) --` 一节显示 X/Y 的有效尺寸（2~3 位小数），
`<` `>` 按 `-- Steps --` 里的 `Scale` 步进（默认 0.1，可切 0.05/0.1/0.25/0.5/1），
也可直接输入数值。范围 0.05 ~ 32 格。

实现细节：`sxf = 有效宽度 / |x|`，`x` 本身不动 —— 这样 `FlipX` 后宽度仍在
（只反号），整数语义（推力面积、分离力）也不被扰动。

## 构建

工程要求 **Unity 2022.3.62f3**（`ProjectSettings/ProjectVersion.txt` 写的 2022.3.62f3c1）
+ Android Build Support。本机已验证的工具链：

| 组件 | 版本/路径 | 备注 |
|---|---|---|
| NDK | **r23b**（`Pkg.Revision = 23.1.7779620`）`D:\android-ndk-r23b` | ⚠️ Unity 2022.3 只认 r23b，r26/r27 会被拒 |
| SDK | `D:\android-sdk`，platforms 30/33/34，build-tools 33.0.1+ | |
| JDK | `C:\Program Files\Java\jdk-17` | Gradle 7.5.1 支持 JDK 17，无需 JDK 11 |
| 签名 | `D:\2SFS Project\keys\sfs-release.jks`（alias `sfs-release`） | 两个包共用，可互换升级 |

```powershell
& "<Unity>\Editor\Unity.exe" -quit -batchmode -nographics -accept-apiupdate `
  -projectPath "D:\2SFS Project\SFS1.4.10-main" `
  -executeMethod BuildPartEditor.BuildBothCLI `
  -logFile "build.log"
```

产物（都已用上面的密钥库签名，可直接安装）：

| 文件 | ABI |
|---|---|
| `Builds/yuanhang-1.4.10-parteditor-armv7.apk` | 32 位 `armeabi-v7a` |
| `Builds/yuanhang-1.4.10-parteditor-arm64.apk` | 64 位 `arm64-v8a` |

只要一个架构时：`-executeMethod BuildPartEditor.BuildArmv7CLI` / `BuildArm64CLI`。
编辑器菜单同样可用：`Tools/PartEditor14 — Build ARM64 / ARMv7 APK`。

> `BuildPartEditor.cs` 会在**运行期**把 SDK/NDK/JDK 写进 Unity 首选项、
> 并用自定义密钥库，不改工程文件；同时保持 `com.sfs1410.pro` / `1.4.10` 的原有身份。

### ⚠️ 本机的坑：批处理模式会报「没有许可证」

直接跑 `-batchmode` 会失败退出：

```
[Licensing::Module] Error: 'com.unity.editor.headless' was not found.
No valid Unity Editor license found. Please activate your license.
```

原因：Unity Hub 的许可证客户端协议与编辑器不匹配
（`HandshakeResponse: Unsupported protocol version '1.16.2'`），编辑器只好用自带的
客户端，而它读不到 `C:\ProgramData\Unity\Unity_lic.ulf`。**先离线激活一次**即可：

```powershell
& "<Unity>\Editor\Unity.exe" -batchmode -nographics -quit `
  -manualLicenseFile "C:\ProgramData\Unity\Unity_lic.ulf" -logFile lic.log
# 看到 "Successfully processed offline license activation request" 就好了
```

之后正常构建；若还报错，再在构建命令里加同一个 `-manualLicenseFile`。

## 已知遗留

- `PlayerPrefs` 键仍是 `PartEditor14.*`（沿用 1.4.06），首次运行球/面板用默认位置
- 皮肤切换依赖 `part.partData.skins`；无皮肤的零件显示 `(none)`
- 1.4.10 全库原本零 `OnGUI`；本模块自带一个，Unity 会正常驱动它（无需任何宿主）
