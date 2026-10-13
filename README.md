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
完成**原生 64 位重建**，并在此之上进行内容扩展：移除广告与内购模块、移植 1.5 建造网格与
再入燃烧效果、高刷新率适配、着色器还原、界面文本完善与场景渲染修复。

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

### 六、移植 1.5 内容

SFS 1.5 是完全重写的新代码，无法整体移植，因此按模块从其中提取资源与机制：

**已生效**

- **建造网格** —— `Assets/Resources/15build/`（`Build_Background` / `Dots` / `Pattern_Squares`
  贴图，`UnlimitedGrid15` / `UnlimitedSolid` 材质），配合新增的 `SFS_BuildGrid15.shader`
  与 `SFS_SolidQuad.shader`
- **无限建造区域** —— `UnlimitedBuildArea.cs`

**资源已导入，效果尚未生效**

- **再入燃烧效果** —— `Burn Gradient.png`、`Burnmark.png` 贴图与音效 `AirBurn.wav`、
  `AirFlow #207.wav`、`Airflow #196.wav` 已导入工程，但燃烧效果**尚未在实际运行中生效**，
  暂不计入本版本的移植成果。

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

## 付费与内购内容（已全部移除）

本工程**不含任何付费内容**：原本需要「完整版」才能使用的零件、皮肤、作弊选项、星球与更大
建造空间均已**无条件开放**；工程内不存在购买入口、价格、商店页、评分引导，也没有内购或广告 SDK。

移除清单：

| 类别 | 移除内容 |
|---|---|
| 场景对象 | `MainMenu.unity` 中的 `Purchase Screen`（含 FAQ / Buy / Restore / No ads / Cheats 等全部子页）、`Thanks Page`、`Full Version Button`、`Events/Buy Menu Mover`、`Events Holder Old/Close Parts Expansion Page`、`Purchaser`，连同 `Game.unity` 的广告占位符 `Banner`，共 **391 + 4 个序列化对象** |
| 内购 / 广告脚本 | `IAP.cs`、`AdManager.cs`、`GoogleMobileAds/`（32 个 .cs）、`Plugins/Assembly-CSharp-firstpass/admob/`（7 个 .cs） |
| 评分引导 | `Controller.AskRate()` / `Controller.OpenRating()`、`CompleteRecovery()` 中的两次评分触发分支，以及指向 Play 商店的内购页链接 |
| DLC / 完整版残留 | `PartData.inFull`（「完整版专属」标记）字段 + 69 个零件 `.asset` 中的 `inFull` 数据；`UploadRocketRequest.Premium` 字段；仅含 DLC 常量的 `SharingConfiguration.cs`；`Saving.SettingKey` 中的 `hasPartDLC` / `hasHackedDLC` / `askedToRate1` / `askedToRate2` / `hasRated` |
| 配置资产 | `Resources/BillingMode.json`、`Resources/UIFakeStoreCanvas.prefab` |
| 包依赖 | `com.unity.purchasing`、`com.unity.ads`（`Packages/manifest.json`） |
| 工程设置 | `UnityConnectSettings.asset` 移除 `UnityPurchasingSettings` 段、`UnityAdsSettings` 关闭；`RuntimeInitializeOnLoadManager.asset` 移除 Advertisements 初始化项 |
| 完整版门禁 | `Ref.hasPartsExpansion` 整体删除，随之移除 `Build` 的皮肤 / 缩放 / 建造范围门禁、`PartGrid` / `PickPartGrid` / `CreateRocket` / `AssetLoader` / `Utility` / `MainMenu` 的完整版分支，以及 `Controller` / `Saving` / `MenuManager` / `Ref` 中的全部广告调用 |

仅剩的「免费版」痕迹是两处**已无代码读取**的序列化调参值，保留只为不破坏已有数据：
`Build.unity` 的 `maxZoomBasic` / `camPosBasic`、`Utility.Specefier.base_value`。

## 与游戏无关的内容（联网 / 社交 / 统计 / 成就）已全部移除

| 类别 | 移除内容 |
|---|---|
| Play 游戏成就 | `GPGSIds.cs`（9 个 `CgkI...` 成就 ID，全工程无人调用） |
| 论坛新闻 | `NewsManager.cs`（抓取 jmnet.one 论坛新闻）+ 主菜单 `News Header` 浮层及其关闭按钮 |
| 社区按钮 | 主菜单 `Discord` / `Forum` / `Reddit` / `Youtube` 四个按钮、图标引用与 `MainMenu.OpenLink*` 处理函数 |
| 统计上报 | `MyAnalytics.cs`、`SessionDisplayer.cs` + `UserID Text` 调试文本、`JMtech/Main/GameTracker.cs`；`Vessel.AddArchivment` 与 `InputController.ID` 中的 `UnityEngine.Analytics` 调用、`Build.LateUpdate` 的 M 键埋点 |
| 远程配置 | `Assets/MasterInfo.cs`（pastebin）、`SharingInfo.cs`、`VersionHolder.cs`、`VersionInfo.cs` |
| 联机框架 | `ServerControlFramework/`(6) + `ServerControlSoftware/`(21) + `ServerTransferProgram/`(19) = **46 个文件** |
| 火箭分享 | `Assets/Scripts/Addons/ShareScript/`(4) + `JMtech/ShareController/PageContainer.cs` |
| 包依赖 | `com.unity.analytics`、`com.unity.modules.unityanalytics`（连带 `services.analytics` / `services.core` 一并退出，APK 里 `UnityServicesProjectConfiguration.json` 消失） |

共删除 **61 个 .cs**（含对应 `.meta`），场景侧删除 **6 个 UI 对象（99 个序列化对象）**。

**保留的「游戏本体」内容**（容易被误判为上面这一类）：

- `Ref.SaveTranfer` —— 旧存档格式迁移，不是联网传输
- `Vessel.vesselAchievements` —— 任务里程碑列表，用于任务结算面板且是存档字段（只删掉了其中的统计上报调用）
- `KeyBinding.cs` —— 按键封装工具类

## 删除元素后的 UI 布局

主菜单按钮列是**固定坐标逐行排布**（全场景无 LayoutGroup，共 0 个）。删掉 `Full Version Button`
后，「继续游戏」与「教程」之间会留下它的空位；原版空位是 128 单位（按钮高 60、普通间距仅 8），
**现按需求收窄为 90 单位**（≈1.5 个按钮高），整列保持「上 2 下 4」的分组观感：

| 按钮 | 原版 y | 当前 y | 与上一项的间距 |
|---|---|---|---|
| 创建新火箭 | 68 | 68 | — |
| 继续游戏 | 0 | 0 | 8 |
| *（原 Full Version Button 的位置，现为空位）* | *-120* | *空* | **90** |
| 教程 | -188 | **-150** | 8 |
| 设置 | -256 | **-218** | 8 |
| 团队 | -324 | **-286** | 8 |
| 退出 | -392 | **-354** | 8 |

> 调空档的方法：把 教程 / 设置 / 团队 / 退出 四个 RectTransform 的 `m_AnchoredPosition.y` **同步加减同一个值**
> 即可整体上移/下移这一组。例如各 +82（→ -68 / -136 / -204 / -272）就是取消空档、恢复均匀 68 间距；
> 各 +38（→ -112 / -180 / -248 / -316）则空档收窄为 52。

其余被删元素删除前均为 `m_IsActive: 0`（Discord / 论坛 / Reddit / Youtube / News Header / UserID Text）
或高度为 0（Game 场景的广告条 `Banner`），删除后不产生可见空档。`InputController` 的按键导航组
同步收缩为 6 个按钮，空的 `Community` 组整体移除。

## 主页教程（视频教程移除 + 图文教程补全）

### 一、移除视频教程

主菜单「教程」页签（`2 Instructions Menu UI / Instructions List Holder`）原为两段：
`Videos Title`（视频教程:）+ 3 个视频按钮，`Text Tutorials`（图文教程:）+ 5 个图文按钮。
视频按钮的 `onClick` 指向 `MainMenu.OpenLinkLiftoff` / `OpenLink`（跳转 YouTube），这两个方法
已在「与游戏无关的内容」清理中删除，按钮因此既无文字也无功能，本次整体移除：

| 项 | 内容 |
|---|---|
| 删除的 GameObject | `Videos Title`、`Video: Orbit`、`Video: Docking`、`Video: Earth to Moon`（含其子物体 `Text` / `Glow`）|
| 序列化对象 | 10 个 Transform（含 RectTransform）+ 50 个对象（GameObject / CanvasRenderer / Image / BoxCollider2D / Button·CustomEvent / Shadow / Text）|
| 引用清理 | `Instructions List Holder(1751)` 的 `m_Children`；`InputController(2311)` 的 `Instructions Buttons` 按键导航组 9 → 6 |
| 代码 | `MainMenu.OpenLinkLiftoff` / `OpenLinkMoonLanding` / `OpenLink(string)` 已删除；全工程已无 `Application.OpenURL` |

### 二、图文教程列表重新居中

删掉视频段后 `Instructions List Holder` 只剩「图文教程:」标题 + 5 个按钮（共 6 项、间距仍为 64），
整组上移 **145** 单位，使其中心与原整列中心一致：

| 项 | 原 y | 当前 y |
|---|---|---|
| 图文教程: | 161 | **306** |
| 起飞 | 97 | **242** |
| 进入轨道 | 33 | **178** |
| 轨道机动 | -31 | **114** |
| 月球交会 | -95 | **50** |
| 月球着陆 | -159 | **-14** |

### 三、图文教程正文补全

5 个教学页（`3 Page UI`）的 `Introduction Text` 与 `Steps Holder` 文本，按同一底包的
`颜色修改_正式版.apk` → `assets/bin/Data/level0`（Unity 2017.4.1f1 二进制场景）为准逐条补全，
并去掉官方中文里的逐字空格（与工程内其余文本的排版一致）。原工程有 **18 处**文本为空或被
截断（例如 Liftoff 页只到「…打开它们.单」、轨道机动弹 1 只到「…加速(增」），现已补成完整句：

| 页面 | 文本节点 | 当前内容 |
|---|---|---|
| 起飞 | Introduction Text | 起飞: / 点击引擎打开它们.单击油门杆，关闭油门. / 滑动油门杆来调整油门. |
| 进入轨道 | Introduction Text | 无论你要去的目的地是哪里，进入一个稳定的轨道始终是任何任务的第一步。 |
| 进入轨道 | Text 1 | 直线飞行到达2-3km的高度，然后旋转20°左右。 |
| 进入轨道 | Text 2 | 保持缓慢旋转。在你到达10km的时候，你的倾斜角度应该在45°左右，在你到达15km的时候，你的倾斜角度应该在60°左右。 |
| 进入轨道 | Text 3 | 燃烧直到大气层以上抛物线轨迹的最高点，然后切断油门。 |
| 进入轨道 | Text 4 | 当你到达你的轨道的最高点的时候，然后燃烧正转，直到你的整个轨道环绕星球为止。 |
| 轨道机动 | Introduction Text | 在轨道上，你可以做很多动作来改变轨道的形状。 |
| 轨道机动 | Text 1 | 在你前进的方向上加速(增加你的速度)会提高另一边的轨道。/ 这叫做进击燃烧。 |
| 轨道机动 | Text 2 | 向相反的方向加速(降低速度)会降低轨道的另一边。/ 这叫做逆行燃烧。 |
| 月球交会 | Text 1 | 在地图视图中点击月亮作为目标。 |
| 月球交会 | Text 2 | 在你的轨道上会出现一个转移窗口标记。/ 这表明是执行地月转移轨道喷射燃烧的最佳时间了。 |
| 月球交会 | Text 3 | 在转移窗口中进行燃烧正转，直到你将 / 一次月球邂逅存档。虚线表示接近你的 / 目标的最近的距离 |
| 月球交会 | Img 1 / Img 3 · Moon Text | 月球 |
| 月球着陆 | Text 1 | 在你最接近进入月球轨道的时候进行燃烧逆行。 |
| 月球着陆 | Text 2 | 再进行一次逆行燃烧以脱离轨道。 |
| 月球着陆 | Text 3 | 当你接近月球表面时减速。 |
| 月球着陆 | Text 4 | 尽可能减少水平速度，这样更容易降落,然后试着轻轻地降落在月球表面。 |

> 两个 `Introduction Text`（月球交会 / 月球着陆）在 APK 里同样为空，保持为空；
> 5 个页面的 `Title` 在原版与原工程里都是 `m_IsActive: 0`（不显示），故同样保持为空。
> 列表按钮文字（起飞 / 进入轨道 / 轨道机动 / 月球交会 / 月球着陆）沿用工程现有译名，未改回 APK 的「发射 / 到达轨道 / 轨道腾挪 / 月球邂逅 / 月球着陆」。

### 四、图文教程正文适配屏幕

补全后的长句会**跑出屏幕**：教学页的 `Text` 原本是 `m_HorizontalOverflow: 1`（不换行、允许溢出），
而文本框宽达 **1250**（导语）/ **800**（步骤），画布参考宽只有 **750**（`m_ReferenceResolution: 750x1334`，
`ScreenMatchMode = Expand` + 按宽匹配，手机竖屏下画布宽恒为 750 → 左右各 375）。于是单行长句
（最长 55 字 ≈ 2800 单位）直接越过两侧边缘。

修法（只改文字排版，不动图片与页面结构）：

| 项 | 原值 | 当前值 |
|---|---|---|
| `m_HorizontalOverflow` | 1（溢出） | **0（换行）** |
| `m_BestFit` / `m_MinSize` / `m_MaxSize` | 0 / 0 / 65 | **1 / 32 / 原字号**（放不下时自动缩小）|
| 步骤文本框宽 | 800 | **510**（中心 x=110 不变 → 占 −145…365，屏内留 10 单位余量）|
| 导语文本框宽 | 1250 | **686**（中心 x=−22）/ 起飞速 **730**（中心 x=0）|
| 导语字号上限 | 64 / 50 | **44**，并把 进入轨道 导语 y 800→**766**、轨道机动 导语 y 612→**600**，使换行后的文字顶边与原来单行时一致，不会顶出屏幕上沿 |

改写后估算（按 CJK 字宽≈字号、ASCII≈0.55×字号，`m_LineSpacing` 计入）：18 处文本全部满足
「文本框在 ±375 内」且「最小字号 32 时高度 ≤ 文本框高」，即 `BestFit` 一定能在框内完整显示；
步骤文字实际取 40–64 号，导语取 44 号。

## APK 审计结论（IL2CPP / ARM64 出包后实测）

- 权限只剩 `android.permission.INTERNET`，**没有 `com.android.vending.BILLING`**
- 构建程序集清单 `ScriptingAssemblies.json` 从 98 个降到 **81 个**，其中**没有** `UnityEngine.Purchasing.dll` /
  `UnityEngine.Advertisements.dll` / `Unity.Analytics.*` / `Purchasing.Common`
- IL2CPP 元数据中 `Purchase` / `Billing` / `IAP` / `AdMob` / `inFull` / `Premium` / `DLC` / `play.google` /
  `jmnet.one` / `pastebin` / `CgkIpOK5` / `AnalyticsSessionInfo` / `ServerControl` 等字符串**全部不存在**，
  也查不到任何内购类型（`IStoreListener`、`ProductDefinition` 等）
- 元数据里仍会出现 `UnityEngine.Purchasing` / `UnityEngine.Advertisements` 两个**程序集名表条目**：这是
  Unity IL2CPP 固定写死的已知程序集名单（同表里还有本工程根本没用的 `UnityEngine.Cloud`、
  `Assembly-CSharp-testable`），两个程序集都不在构建清单里，并非工程代码
- 仍保留的 `UnityEngine.UnityAnalyticsModule.dll` / `UnityAnalyticsCommonModule.dll` 是 Unity 引擎自身
  的底层模块（工程已无任何调用），随引擎固定带入，无法通过 manifest 去除


## 无用代码清理（2026-10）

在**不改变任何运行时行为**的前提下，按「代码 / 字符串 / 场景与预制体 YAML / ProjectSettings」四路交叉验证，
删除了确认无引用的代码。判定时特别注意了三类容易误删的情形，全部保留：

| 易误删情形 | 例子 | 处置 |
|---|---|---|
| 扩展方法类（无需写出类名即可调用） | `CasterUtility`（`"name".Planet()`）、`SetTypeUtility`、`ObjectFileUtility.Escape` | **保留** |
| Unity / Odin 属性触发的入口 | `[RuntimeInitializeOnLoadMethod]` 的 `InputController.InitFrameRateEarly`（高刷启动引导）、`[Button]` 的开发工具（`Blur`、`AssetLoader.LoadAssets`） | **保留** |
| 脚本 GUID 被场景 / 预制体引用 | `FlameRandomizer`、`LandingLegModule`、`PartChildCollider`、`SortingLayerExposer`（零件预制体在用） | **保留** |

**删除的类文件（9 个 .cs）**：`LinkModule`、`RocketPost`、`StarGenerator`、`TranslationRefrence`，
以及整个未被引用的 `Exposure/` 小框架（`ExposureModule` / `ErrorLog` / `ExposureBaseError` / `VersionParseError` / `Version`）。

**删除的无用成员（39 个）**：散落在 `ClosestApproach`、`Controller`、`FlowModule`、`InputController`、
`Kepler`、`Map`、`MenuManager`、`MoveModule`、`OrbitLines`、`Parser`、`Part`、`PartsManager`、
`PlanetManager`、`PostProcessingManager`、`Ref`、`Saving`、`SeparatorModule`、`Utility` 等 19 个文件中，
例如 `Ref.GetTimeString` / `Ref.DeleteFile`、`Utility.DrawCross` 等 8 个绘图与几何辅助、
`Saving.HasSetting`、`Saving.ResourcesLocation`、`PlanetManager.launchPadPositionX` 等。

**同时清理的死代码与告警**：

- `Build.FlipToStick` 整体是 `if (false)` 包裹的死块，方法等同于空操作 —— 连同其唯一调用点一起删除
- `InputController.sHighRefreshRequested`（只写不读）、`InputController.oldPos`、`Reentry.num`（未使用局部变量）
- `ObjectFileUtility` 的 `Path.InvalidPathChars` → `Path.GetInvalidPathChars()`（行为等价）

编译告警从 **8 条降到 3 条**，剩下 3 条都是**刻意保留**的过时 API：
`AssetLoader` 的 `WWW`（读取本地贴图文件）与 `InputController` 的
`Screen.SetResolution(int,int,FullScreenMode,int)`（高刷适配依赖这个重载，新版 API 语义不同，不能盲改）。

### 第二轮：仍未引用的内容也已移除

第一轮保留下来的「可挂载组件 / 工具入口」，经再次确认**从未被任何代码、场景、预制体引用**后一并删除：

| 删除内容 | 说明 |
|---|---|
| `Reentry.cs` | `[ExecuteInEditMode]` 的空壳组件（`Update` 里只有空循环），未被任何对象挂载 |
| `PlanetEditor.cs` | `[CreateAssetMenu]` 的空 `ScriptableObject`，全工程无实例 |
| `Blur.cs` | 贴图生成工具（`[Button]` 写 PNG），未被任何对象挂载、无代码引用 |
| `KeyBinding.cs` | 无人调用的按键封装类；连同已空的 `Addons/JMtech`、`Addons/Translations` 目录一起清理 |
| `PartData.flip2stickX` / `flip2stickY` | 「完整版」时代翻转吸附的零件标记，唯一读取点就是上一轮删掉的 `if (false)` 死块；字段与 **69 个零件 `.asset` 里共 138 行**数据一并移除 |

**行尾规范化**：`MainMenu.cs` / `Vessel.cs` / `PartsManager.cs` / `Saving.cs` 这 4 个文件曾变成 LF，
已统一回工程与 git HEAD 的 **CRLF**；现在 `Assembly-CSharp` 下 76 个 .cs **全部 CRLF**。

最终代码规模：**76 个 .cs / 18242 行**（清理前 89 个 / 19088 行），编译 **0 error**、**3 warning**（均为上表刻意保留项）。

## 性能优化（2026-10）

在**输出完全一致**的前提下优化了每帧热路径。所有改动都用「静态扫描热路径 → 逐处核对等价性 → 编译验证」的流程处理。

### 一、飞行 HUD：消除每帧字符串垃圾（主要收益）

`Controller.UpdateUI()` 由 `LateUpdate` **每帧**调用，原实现每次要拼接 5~8 个临时字符串：

```csharp
string text = ((int)velocity).ToString();
...
topRightText.text = "高度:" + RoundUI(height) + "\n速度:" + text + "m/s" + cheatsText;
```

在 60/90/120 fps 下这是持续的小对象分配，移动端会表现为周期性掉帧。改为：

1. **按「显示精度」缓存**：用 `(int)(velocity * 10)` 与按 `RoundUI` 各单位档位换算出的高度键判断显示内容是否真的变化，未变化直接 return；
2. **复用 `StringBuilder`**：消除 `+` 拼接产生的中间字符串。

两处都**不改变输出的文本**（逐字符一致）。极端情况下（高速连变）每次刷新只产生 1 个最终字符串，
常态下每帧 0 分配。

### 二、地图视图：缓存每帧的 GetComponent

`Map.DrawDropdownMenu()` 在 `LateUpdate` 里每帧执行两次
`transform.GetChild(0).GetComponent<TextMesh>()`（Unity 的 `GetComponent` 带类型检查与数组遍历）。
这两个镜像 TextMesh 是固定子物体，已改为**惰性缓存**（为空时自动重取，物体被销毁也能自愈）。

### 三、过时 API 现代化

| 位置 | 旧写法 | 新写法 |
|---|---|---|
| `InputController.ApplyFrameRate` | `Screen.SetResolution(w, h, FullScreenMode, int fps)`（已过时，CS0618） | `Screen.SetResolution(w, h, FullScreenMode, new RefreshRate { numerator = (uint)fps, denominator = 1 })` |

这是 Unity 官方给出的等价重载（旧重载内部就是构造 `RefreshRate`），行为不变，同时消掉一条编译告警。

### 四、顺手清掉的空调用

`Utility.DrawCircle` 是**空实现**，却在 `Controller.PositionCameraGame()` 里每帧被调用一次。
已删除该空方法与其唯一调用点（不产生任何行为变化）。

### 五、地图视图：去掉同一帧的重复轨道解算

`Map.LateUpdate()` 与 `Map.DrawOrbitLines()`（由 `Controller` 调用）原本各自**对主火箭调用两次**
`GetVesselOrbits(Ref.mainVessel)`：先算给 `vesselOrbits`，紧接着又把同一个表达式再算一次传给 `OrbitLines.DrawOrbitLines`。

当主火箭处于 `RealTime` 且速度 > 2 m/s 时，`GetVesselOrbits` 会走 `Orbit.CalculateOrbits(...)` ——
一次**完整的轨道解算 + List 分配**。同一帧、同一输入，两次结果必然相同，因此第二处改为复用第一处的 `vesselOrbits`。
→ 地图视图下每帧的轨道解算次数 **减半**。

### 六、地图标记：值未变化时不写 Transform

`Map.UpdateVesselsMapIcons()` 在捏合缩放时每帧都会被调用，原实现无条件给每艘火箭的
`mapIcon.localScale` 赋值。Transform 写入会标脏层级并向下传播，即使新值与旧值完全相同。
已改为**先比较再写**（`Vector3` 的 `!=` 是近似比较，缩放抖动不会导致反复写入）。

### 七、删除两个"永远不该被调用"的死方法（IMGUI）

| 位置 | 说明 |
|---|---|
| `ValuesModule.Draw(int, GUIContent)` | 私有方法、全工程无调用者，方法体是脱离 `OnGUI` 的 `GUILayout.BeginHorizontal()`（一旦被调用会直接抛异常） |
| `Ref.Label` / `Ref.Box` | 无调用者的 `GUI.Label` / `GUI.Box` 辅助方法 |

工程里**没有任何 `OnGUI`**，这两处 IMGUI 代码属反编译残留，删除后既少了两条死分支，也消除了误用风险。

### 已排查但确认无需改动的部分

`Soundtrack.Update` / `MoveModule.Update` / `Wheel.Update` / `PlanetManager.Update` 等每帧方法中**没有任何堆分配**；
`Vessel.GetGlobalPosition` / `GetGlobalVelocity` 是纯结构体运算；地形分块更新是**里程阈值驱动**（不是每帧重建），
均已保持原样。

### 八、全量逐方法扫描（76 个 .cs 全覆盖）

之前几轮偏重"每帧方法"，这次对**全部 76 个文件**做了逐方法扫描：容器分配 / 对象 `new` / `ToArray`·LINQ /
`GetComponent`·`Find` / `Instantiate`·`Destroy` / 字符串操作 / 数学调用 / 嵌套循环，
并对 `Update`·`LateUpdate`·`FixedUpdate`·`OnCollision*` 等每帧方法乘 3 倍权重。
共评分 84 个方法，本轮新增修掉 5 处：

| 位置 | 问题 | 修法 |
|---|---|---|
| `OrbitLines.DrawDottedLine` | **每次调用**都新建 `Vector3[4]` / `int[6]` / `Vector2[4]` / `Color[4]`；地图视图下（近拱点/远拱点/交会点标记）每帧最多调用十余次 | 顶点·索引·UV·颜色改为**静态复用缓冲**（mesh 属性赋值会把数据拷进网格原生缓冲，复用安全） |
| `SpriteMesh.Generate` | 每次调用 `new Mesh()` 并赋给 `sharedMesh`，**旧 Mesh 从不销毁**；切皮肤、拖动零件开关时会反复调用 → 原生内存持续增长到 `UnloadUnusedAssets` 才回收 | 改为**复用同一个 Mesh**（`Clear()` 后重填），并缓存 `MeshFilter`/`MeshRenderer`。已确认 `sharedMesh` 只在该方法写入、别处从不读取，因此复用不会影响其他系统 |
| `Touch.GetTouchInputs` | 每帧对同一根手指调用 3~5 次原生 `Input.GetTouch()`；缩放判定里还重复 4 次 `GetTouchIdFromFingerId`（每次都遍历触点） | 帧内取一次 `UnityEngine.Touch` 存下来；缩放判定改为只查 2 次；`StayTouch` 改为直接接收触点位置（签名私有，安全） |
| `Map.GetEllipsePoints` | `GetChild(0).GetComponent<LineRenderer>()` 写在**循环体内**，每个顶点查一次 | 提到循环外只取一次 |
| `InputController.LateUpdate` | 每帧各访问两次 `leftArrow/rightArrow.GetChild(0).gameObject` | 子物体引用改为**惰性缓存**（为空自动重取） |

> 修 `Touch.cs` 时踩到一个坑：工程自己定义了 `public class Touch : MonoBehaviour`，
> 会遮蔽 `UnityEngine.Touch`，必须写全名 `UnityEngine.Touch` 才能通过编译。

### 九、扫描到但**判断不值得改**的其余项

| 位置 | 情况 | 结论 |
|---|---|---|
| `Controller.StartRecovery`（13 处 `GetChild/GetComponent`） | 回收火箭时的**一次性弹窗** | 低频，改缓存收益极小 |
| `MenuManager.ShowWarning`（12 处） | 每次弹确认框调用一次 | 同上 |
| `PartsManager.UpdatePartsGruping`（2 个 List + 2 层循环） | 只在**关节断裂/分离**时触发 | 语义上必须重建分组，优化空间有限 |
| `FastNoise.SingleCellular*`（9~12 层循环） | 地形/贴图生成时的噪声采样 | 循环是算法本身，容器分配为扫描误报（`new Vector3` 等结构体） |
| `Kepler.GetVelocity` / `GetTrueAnomaly`（各约 11 次数学调用） | 轨道解算核心 | 数值算法，`Math.Sqrt/Pow` 无法安全替换 |

### 发现了但**故意没改**的项（风险高于收益）

| 项 | 现状 | 为什么不动 |
|---|---|---|
| `AssetLoader` 的 `WWW`（2 条 CS0618） | 同步读取 `WWW.texture` | 换 `UnityWebRequest` 必须改成异步/协程，语义会从「基本拿不到贴图」变成「真的加载贴图」——那是行为变更，不是性能优化 |
| `Physics2D.m_AutoSyncTransforms: 1` | 物理查询前自动同步 Transform | 关闭它能省掉每次查询的同步开销（Unity 官方推荐的性能开关），但本作在 `Update/LateUpdate` 里大量移动碰撞体后**立即**做 `Physics2D` 查询（建造放置、触摸拾取），关掉后会读到旧位置 → **有破坏玩法的风险**，保持开启 |
| `Physics2D` 迭代次数 5 / 3 | Unity 默认值 | 调低会牺牲物理稳定性（火箭对接/着陆手感），不划算 |
| `managedStrippingLevel` | 默认 | 提高可减包体，但工程大量使用 Odin/Sirenix 的**反射**序列化，剥离有崩溃风险 |
| `Camera.main`（`Build`/`Touch`/`Utility`） | 属性调用 | Unity 2022 内部已缓存主相机查找，收益很小；自己缓存反而有「相机切换后引用失效」的风险 |

> 关于已排查项：`Soundtrack.Update` / `MoveModule.Update` / `Wheel.Update` / `PlanetManager.Update`
> 等每帧方法**没有堆分配**；`Vessel.GetGlobalPosition` / `GetGlobalVelocity` 是纯结构体运算；
> 地形分块更新是**里程阈值驱动**。这些都确认无需改动。

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
