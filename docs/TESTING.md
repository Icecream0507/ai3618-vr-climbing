# 仿真运行 & 测试文档 —— Summit VR

> 给组内任何人：**不需要 VR 头显、不需要接真机**，在电脑上就能跑通整个游戏、看演示、出 demo 视频。
> 本项目已在 **Tuanjie 2022.3.62t7**（团结引擎，Unity 中国版，与 Unity 2022.3 同源）上验证通过。

---

## 0. 三种用法一览

| 你想干什么 | 用哪个 | 要不要头显 |
|---|---|---|
| 确认代码没坏 / 逻辑对 | **无头自检** `Run Headless Check` | 否 |
| 现场看「机器人爬墙」演示 | 打开 `Demo.unity` 按 Play | 否 |
| 出一段 demo 视频文件 | **自动录制** `Record Demo` + ffmpeg | 否 |

三种都不碰 XR，全部在编辑器里跑。真机/Device Simulator 是可选项，本项目按老师"仿真即可"的口径**不依赖**它。

---

## 1. 打开项目

1. 装 **Tuanjie Hub**，再装编辑器 **2022.3.62t7**（或任意 Unity 2022.3 LTS / Unity 6，引擎同源能开）。
2. Hub → *添加 / Add project from disk* → 选这个仓库根目录（含 `Assets/`、`ProjectSettings/` 的那层）。
3. 首次打开会自动解析 `Packages/manifest.json`（XRI 实际会解析到 3.2.1），等它编译完。
   - 项目里**已提交** `ProjectSettings`、`Hold` 层、URP 管线资产、`packages-lock.json`、所有 `.meta`，所以打开即用，不用再手动配。

> 命令行打开（可选）：编辑器路径一般在
> `D:\Application\tuanjie\2022.3.62t7\Editor\Tuanjie.exe`。

---

## 2. 无头自检（最快，确认没坏）

菜单 **`VRClimb ▸ Run Headless Check`**，或命令行：

```bash
Tuanjie.exe -projectPath . -batchmode -nographics \
    -executeMethod VRClimb.EditorTools.HeadlessCheck.Run -logFile Logs/e2e.log
```

它会：跑 `ClimbMath` 数学自检 → 自动建场景 → 让一个**脚本机器人**驱动真实游戏逻辑跑完
「抓握 → 身体探出失衡 → 脱落坠落 → 重生 → 爬完 Route 0 → 登顶计时」。

- 通过：退出码 **0**；报告写在 `Logs/headless-check.txt`。
- 当前基线：**ClimbMath 9/9 + 端到端 10/10 PASS**（约 6 秒登顶、1 次脚本失衡）。
- 失败：退出码 2，报告里会列出具体哪一条 `FAIL`。

这一步在做 PR / 改完代码后跑一下，能挡住绝大多数回归。

---

## 3. 现场看演示（答辩 / 录屏都用这个）

1. 打开场景 **`Assets/Scenes/Demo.unity`**（若不存在，先点菜单 **`VRClimb ▸ Build Demo Scene`** 生成）。
2. 按 **Play**。

你会看到一个**第三人称视角**：

- 一个**运动学攀岩者**自动爬程序生成的岩壁——身体从手上悬挂、脊柱与头随受力倾斜、转髋贴墙、悬腿随重力摆动（对标 Klifur，见 `docs/avatar-evolution.png` 前后对比、`docs/DESIGN.md` 物理小节）；
- 左下角 **BALANCE 平衡条**：身体探出支撑范围时变红、耗尽 → **脱落坠落**；落地后回到起点重新开始；
- 之后机器人用**手脚配合**保持平衡爬到顶，右上角计时 + 跌落次数，底部字幕解说当前在干嘛；
- 摄像机跟着爬升高度上移。

> 这一幕就是项目的核心卖点：**控制器-only 的 VR 也能做"平衡 + 落脚点"**。
> 想自己录屏：直接用 Windows `Win+G`(Xbox Game Bar) 或 OBS 录这个 Game 视图即可。

如果想看别的线路：选中场景里的 `RouteBuilder`，把 `routeIndex` 改成 `1`(平衡测试) / `2`(The Arete) / `3`(耐力)，再 Play。

---

## 4. 自动出 demo 视频（无人值守）

一条命令直接产出 mp4，不用手动录屏：

```bash
# 1) 让编辑器渲染并把每帧存成图片（注意：不要加 -nographics，要真渲染）
Tuanjie.exe -projectPath . -batchmode \
    -executeMethod VRClimb.EditorTools.DemoBuild.Record -logFile Logs/record.log
# 帧序列输出在 Logs/frames/f_00000.jpg ...

# 2) 用 ffmpeg 合成 mp4（本机 ffmpeg 在 D:\Application\ffmpeg-7.1.1-essentials_build\...\bin）
ffmpeg -y -framerate 30 -i Logs/frames/f_%05d.jpg \
    -c:v libx264 -pix_fmt yuv420p -movflags +faststart \
    Demo/SummitVR_demo.mp4
```

- 第 1 步用 `Time.captureDeltaTime` 锁定 30fps 离线渲染，渲染多慢都不影响成片节奏。
- 也可在编辑器里点菜单 **`VRClimb ▸ Record Demo (success)`** 跑同样的录制。
- **成片**（都已放进 `Demo/`）：
  - `Demo/SummitVR_demo.mp4` —— **完整演示**：开场失衡坠落 → 重生 → 爬 Route 0 到顶（带开场字幕）。
  - `Demo/SummitVR_V1.mp4` … `Demo/SummitVR_V4.mp4` —— **四条难度线路**（V1 Warm-up / V2 Balance Test / V3 The Arete / V4 Endurance），每条都是一次干净完攀（跳过开场坠落，专注线路本身），落脚/平衡/耐力难点各不相同。
  - `Demo/SummitVR_impossible.mp4` —— **线路太难、完攀失败**：Route 4「The Gap」中段有一个约 2m 的空白(超过臂展+锁定的极限)，攀岩者全身伸展也够不到,最后脱手坠落,字幕 *Route unclimbable*。
- 录 V1–V4:菜单 **`VRClimb ▸ Record V1..V4`**,或命令行 `-executeMethod VRClimb.EditorTools.DemoBuild.RecordV1`(`RecordV2`/`RecordV3`/`RecordV4` 同理),再用同样的 ffmpeg 命令(输出名改 `SummitVR_V1.mp4` 等)。
- 重录失败线路:菜单 **`VRClimb ▸ Record Demo (impossible route)`**,或命令行 `-executeMethod VRClimb.EditorTools.DemoBuild.RecordImpossible`,再用同样的 ffmpeg 命令(输出名改 `SummitVR_impossible.mp4`)。

---

## 5. 自己玩（可选，需要 XR）

本项目**不要求**这一步，但若以后想上头显：照 [`SETUP.md`](SETUP.md) §1–2、§6 启用 OpenXR、导入 XRI Starter Assets、
把 `XR Origin` 接上 `PlayerClimberSetup`、绑 grip action、Hold 层（已建好）。Quest 构建见 SETUP 末尾。

---

## 6. 常见问题

| 现象 | 原因 / 处理 |
|---|---|
| 握点 / 物体全是洋红色 | URP 管线资产没生效 → 菜单 `VRClimb ▸ Ensure URP Pipeline Asset`，或重开项目（已提交，正常不会发生） |
| `Record` 出的帧是黑的 | batchmode 下显卡上下文异常 → 去掉 `-batchmode` 用带窗口的编辑器跑同一 `-executeMethod`，必定能渲染 |
| 爬不动 / 原地不动 | 已修：`CharacterController.minMoveDistance` 默认会吞掉慢速位移，代码里已置 0；若自建 rig 记得也置 0 |
| 找不到 ffmpeg | 本机在 `D:\Application\ffmpeg-7.1.1-essentials_build\ffmpeg-7.1.1-essentials_build\bin\ffmpeg.exe`，或自行 `winget install ffmpeg` |
| 想换更长 / 更慢的演示 | `SimulatedClimber` 上的 `demoPullSpeed`、`demoGrabPause` 调慢即可 |

---

## 7. 代码里这些"测试"在哪

- `Assets/Editor/HeadlessCheck.cs` —— 无头自检入口（建场景 + 跑机器人 + 退出码）。
- `Assets/Scripts/Util/SimulatedClimber.cs` —— 脚本机器人，驱动**真实**游戏栈（不是 mock），10 条断言。
- `Assets/Scripts/Util/ClimbMathSelfTest.cs` —— 纯数学自检（9 条），组件上点 *Run Self-Test* 也能单独跑。
- `Assets/Editor/DemoBuild.cs` —— 建/存 `Demo.unity`、录制入口。
- `Assets/Scripts/Util/DemoOverlay.cs` · `DemoVisuals.cs` · `FrameRecorder.cs` —— 演示用的字幕/平衡条、可见化身/跟随相机、逐帧录制。
