# 远航深洋

> [!WARNING]
> **社区版免责声明**
>
> 本工程为**非官方社区版本**，由社区开发者制作，与 Spaceflight Simulator 的开发者
> （Team Curiosity / Stefo Mai Morojna）及任何官方渠道**没有任何关联**，也未获得官方授权或认可。
>
> - **完全免费** —— 仅供学习、研究与个人使用，**严禁任何形式的倒卖、付费销售或二次打包出售**。
> - **版权归属** —— Spaceflight Simulator 及其全部美术、音频、代码资源版权归原作者所有。
> - **请支持正版** —— 如果你喜欢这款游戏，请通过官方渠道购买。
> - **禁止收费与营销** —— 禁止对本项目进行任何形式的收费，禁止在基于本项目的修改中添加任何营销内容。
> - **衍生作品必须免费开源** —— 基于本项目的修改项目必须完全免费发布，并需在本项目下建立分支仓库（Fork）并公开源代码。
> - 使用本工程所产生的一切后果，由使用者自行承担。

**本项目使用 DeepSeek AI 开发。**

Spaceflight Simulator 1.4 的原生 64 位移植和内容扩展。

本项目基于 Spaceflight Simulator 1.4.06 的逆向工程，将工程迁移至 Unity 2022.3.62f3c1，
完成**原生 64 位重建**，并在此之上进行内容扩展：高刷新率适配、着色器还原、界面文本完善
与场景渲染修复。

## 工程信息

| 项 | 值 |
|---|---|
| Unity 版本 | 2022.3.62f3c1 (LTS) |
| 原版基础 | Spaceflight Simulator 1.4.06（原工程 Unity 2017.4.1f1） |
| 目标架构 | **ARM64**（`arm64-v8a`，`AndroidTargetArchitectures: 2`） |
| 原版架构 | `armeabi-v7a` / `x86`（均为 32 位） |
| 产品名 | 远航深洋 |
| 版本号 | 1.4.10 |
| SDK | minSdk 22 / targetSdk 30 |

## 主要改动

### 一、原生 64 位移植

原版 APK 只提供 32 位原生库，**完全没有 `arm64-v8a`**：

| 原版 APK 内的 native 库 | 架构 |
|---|---|
| `lib/armeabi-v7a/libunity.so`、`libmono.so`、`libmain.so` 等 | **ARM 32 位** |
| `lib/x86/libunity.so`、`libmono.so`、`libmain.so` 等 | **x86 32 位** |

也就是说，在 64 位设备上原版始终运行在 32 位兼容层里。本工程将整包重建为 64 位目标
（`AndroidTargetArchitectures: 2` = ARM64），并从 Unity 2017.4.1f1 升级到 2022.3.62f3c1。

**意义**
- 摆脱 32 位兼容层，直接以 arm64 原生运行
- 在**仅支持 64 位**的设备上可以正常安装运行 —— 32 位包在这些机型上会被系统直接拒绝安装
- 64 位进程拥有更大的虚拟地址空间，对零件数量多、结构复杂的存档更友好

### 二、高刷新率适配（核心）

原版只有 30 / 60 两档帧率，且在高刷屏上会被系统的自适应刷新策略压制。本工程实现
**30 / 60 / 90 / 120** 四档可选帧率。

**关键突破**：Unity 的 `AndroidJavaObject.Call<T>()` 会按泛型返回类型去匹配 JNI 签名，返回类型
非 `object` 的方法（返回 `Transaction`、`void` 等）会误报 `NoSuchMethodError`。因此跨版本调用一律
改用 `AndroidJNI` + 显式签名：

```csharp
IntPtr txClass = AndroidJNI.FindClass("android/view/SurfaceControl$Transaction");
IntPtr setMid  = AndroidJNI.GetMethodID(txClass, "setFrameRate",
    "(Landroid/view/SurfaceControl;FII)Landroid/view/SurfaceControl$Transaction;");
jargs[2].i = 1;   // FRAME_RATE_COMPATIBILITY_FIXED_SOURCE —— 告诉系统帧率固定，不要自适应
jargs[3].i = 0;   // CHANGE_FRAME_RATE_ALWAYS
tx = AndroidJNI.CallObjectMethod(tx, setMid, jargs);
AndroidJNI.CallVoidMethod(tx, AndroidJNI.GetMethodID(txClass, "apply", "()V"), new jvalue[0]);
```

配套改动：
- `ProjectSettings.asset`：`androidUseSwappy: 1 → 0`（关闭 Optimized Frame Pacing）
- 以 `FRAME_RATE_COMPATIBILITY_FIXED_SOURCE` 向系统投票，抑制自适应降帧
- 30 / 60 沿用原版机制由 Unity 的 `targetFrameRate` 锁定

**真机验证**（OnePlus PLK110 / Android 17 / SDK 37）

| 目标帧率 | 显示刷新率 | 实测 FPS |
|---|---|---|
| 30 | 60 Hz | 30 |
| 60 | 60 Hz | 60 |
| 90 | 90 Hz | 90 |
| 120 | 120 Hz | 120 |

**已知限制**：该机型面板最低档为 60 Hz，系统不存在 30 Hz 显示模式，30 fps 由 `targetFrameRate`
在 60 Hz 面板上按 60/2 分频实现（与原版机制一致）。

### 三、着色器还原

原版着色器在 APK 中是编译后的 GLSL 二进制。本工程将其提取并逐行还原：

```
APK -> Shader.m_ParsedForm -> m_SubShaders[0].m_Passes[0].progFragment.m_SubPrograms[i].m_BlobIndex
    -> compressedBlob + offsets/compressedLengths/decompressedLengths
    -> 纯 Python LZ4 块解压 -> 可读 GLES GLSL
```

| 着色器 | 关键修正 |
|---|---|
| `Assets/Shader/Sprites_Terrain.shader` | 纹理坐标实为 `(uv.x/(1-uv.y), uv.y)`；星球贴图 P 乘以 A/B/Terrain 混合（原误写为 lerp）；阴影项 `(1 + t2.x*(1-min(1,uv.y*_ShadowSize)))`；`Blend One Zero` / `ZWrite Off` / `ZTest LEqual` |
| `Assets/Shader/Sprites_Atmosphere.shader` | 云坐标为极坐标 `((uv.x/uv.y)*_CloudSizeX + _PosX, ...)`；合成 `grad.rgb*((1-cloud.a)*grad.a) + cloud.rgb*cloud.a`；`Blend One OneMinusSrcAlpha` |

`Sprites_Icon.shader` 的原版 GLSL 已存档，工程内目前仍为模板。

### 四、界面与文本

- 场景内 148 条文本完成校对：英文 0 条、含多余空格的中文 0 条（仅保留 "SFS"）
- 主页文本以 APK 为准；「制作人员」改为「团队」，内容取官方团队名单
- 修正 Unity YAML 单引号不解析换行转义的问题（相关文本改用双引号）
- 占位文本按原版置空；Text Shade 隐藏（`m_IsActive: 0`）

### 五、场景与渲染修复

- **星球贴图透明/可见背景**：修正 mesh UV（`GenerateMesh` 的 uv/uv2/uv3 语义）、
  `LowRezPlanet.localScale`、相机 `cullingMask`、渲染队列与深度状态
- **帧率依赖修正**：`PlanetManager.totalDistanceMoved += GetMaxVesselsVelocity()*Time.deltaTime + 6.0*Time.deltaTime`
  （原为每帧固定 +0.1，与帧率绑定，高刷下会导致物理表现异常）

## 工程结构

```
Assets/
  Texture2D/Scripts/Assembly-CSharp/   游戏逻辑（反编译源码 + 本工程改动）
  Shader/                              着色器（含还原后的 Sprites_Terrain / Sprites_Atmosphere）
  Scenes/                              MainMenu / Game / Build
  Resources/                           部件、星球、贴图资源
  Mod Data/                            星球数据
ProjectSettings/                       Unity 工程设置
Packages/                              包依赖清单
```

## 构建

1. 安装 **Unity 2022.3.62f3c1**（含 Android Build Support 与 SDK/NDK）
2. 用 Unity Hub 打开本目录
3. `File → Build Settings → Android`，目标架构选 `ARM64`
4. 构建 APK 后自行签名安装

> 首次打开会重新生成 `Library/`（约 3.9 GB），耗时较长。该目录已在 `.gitignore` 中排除。

## 已知限制

- 30 fps 档在无 30 Hz 显示模式的设备上依赖 60/2 分频实现
- `Sprites_Icon.shader` 尚未还原
- 内购（IAP）功能不可用，详见下节

## 关于 IAP

工程中的 `Assets/Texture2D/Scripts/Assembly-CSharp/IAP.cs` 是**最小桩类**，不是原版实现。

原因：反编译工程迁移到新版 Unity 时，内购相关类型（`com.unity.purchasing`）无法完整解析，
保留原实现会阻断编译。IAP 仅用于售卖部件扩展包，与颜色 / 皮肤 / 高刷改动无关，因此改为
保留类名与序列化字段的桩类，以保证工程可编译。**购买与 DLC 功能在本工程中不可用。**

## 经验记录

1. **Unity 的 `AndroidJavaObject.Call<T>()` 会按泛型返回类型匹配 JNI 签名** —— 返回类型非 `object`
   的方法会误报 `NoSuchMethodError`，跨版本调用一律用 `AndroidJNI` + 显式签名
2. **`LayoutParams` 的刷新率偏好是粘性的** —— 从高档位切回低档必须显式覆盖，否则屏幕停在
   120 Hz 会导致 30 / 60 锁不住
3. **Android 的显示模式决策主要在屏幕点亮时重新评估** —— 熄屏 / 亮屏可触发重新评估
4. **Unity 的 `Time.smoothDeltaTime` 有约 1 秒滞后** —— 用它判断帧率会滞后一档，容易误判
5. 本机 Unity 的资源变更监听不灵敏，改动后可能需要重新 Build 或重启编辑器

## 使用与分发条款

**1. 禁止收费与营销**

禁止对本项目进行任何形式的收费。包括但不限于：直接售卖、付费下载、捐赠门槛、会员制、
以及将其打包进任何付费产品或服务中。

禁止在基于本项目的修改中添加任何营销内容。包括但不限于：推广链接、引流二维码、
广告 SDK、赞助商信息、以及与本项目无关的商业宣传。

**2. 衍生作品必须免费开源**

基于本项目进行的任何修改或再发布，必须满足以下全部条件：

- **完全免费** —— 不得以任何形式向使用者收取费用
- **在本项目下建立分支仓库（Fork）** —— 以便追溯来源与变更
- **公开源代码** —— 修改后的完整源代码必须开源，供任何人获取
- **保留本条款与上述免责声明** —— 不得移除或弱化

**3. 违反条款**

任何违反上述条款的使用、修改或分发，均不被授权。如不同意本条款，请勿使用本项目的任何内容。

上述条款的完整法律文本见仓库根目录的 [LICENSE](LICENSE)。

## 声明

- 本项目为**非官方社区项目**，与 Spaceflight Simulator 的开发者（Team Curiosity / Stefo Mai Morojna）无任何关联
- 本工程**完全免费**，严禁倒卖、付费销售或二次打包出售
- Spaceflight Simulator 及其美术、音频、代码资源版权归原作者所有
- 本工程基于对游戏资源的逆向分析，仅供学习、研究与 mod 开发使用
- 请支持正版
