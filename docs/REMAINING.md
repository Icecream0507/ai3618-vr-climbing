# 剩余任务与分工 —— 冲刺 6/18 pre · 7/2 提交

> 给全组 5 人看的「还剩什么、谁做什么」。打勾推进。截止：**6/18 自愿 pre（可加分）**、**7/2 提交（GitHub 链接 + Demo 视频）**。

## 一、现状（已完成的，别重复造轮子）

- ✅ **代码 v1 全部写完并已推送**：手抓攀爬（反向位移）+ 平衡系统（头显当质心）+ 脚法（虚拟脚自动吸附）+ 3 条程序化线路 + 音效事件钩子 + HUD + 编辑器一键建场景。
- ✅ **文档齐全**：`README` · `SETUP`（§6 最快跑通）· `DESIGN` · `RESEARCH`（调研）· `REPORT`（报告初稿）· `DEMO`（录制清单）· `CONTRIBUTING`（git 协作）· `TASKS`（角色分工）。
- ✅ 仓库 + 协作者已配置。
- ✅ **项目已在引擎里编译 + 跑通**（Tuanjie 2022.3.62t7 无头模式）：`ProjectSettings`/`packages-lock`/`Hold` 层/URP 管线资产已生成并提交；**自动化端到端测试通过** —— 机器人爬手依次完成「抓→失衡→脱落→坠落→重生→爬完 Route 0 登顶计时」，`ClimbMath 9/9 + 端到端 10/10 PASS`。复现：菜单 `VRClimb ▸ Run Headless Check`，或打开 `Assets/Scenes/SimTest.unity` 直接 Play 围观机器人爬墙。
- ⚠️ 仍待真人验证：**XR 真机/Device Simulator 的手感**（仿真机器人 ≠ 真手柄输入），这是 P0-2 调参的前提。

## 二、剩余任务总览（P0 = 6/18 前必须，P1 = 7/2 前）

> **范围调整（按老师口径"仿真验证即可，不必接真机"）**：**不做 XR 真机/手柄接入**。演示 = 仿真机器人自动跑通 + 录屏成片。原 P4 的"接 XR Origin / 绑 grip / Quest 构建"**整体降级为可选**，关键路径上不再依赖它。

**P0 — 冲一个能演示的版本**
1. ~~在 Unity 打开项目、编译通过、跑通最小闭环（抓 → 爬 → 失衡 → 坠落重生 → 登顶计时）~~ ✅ 已由无头端到端测试完成并提交（见上）。
2. ~~调平衡 / 脚法参数手感~~ → 机制已验证；演示用机器人节奏已调好（`SimulatedClimber.demoMode`）。真人手感调参降级为可选。
3. ~~录 demo 视频~~ ✅ **可自动出片**：`VRClimb ▸ Record Demo` 渲染帧 + ffmpeg 合成 `Demo/SummitVR_demo.mp4`（流程见 `TESTING.md §4`）。剩：定稿剪辑 / 配字幕（可选）。
4. 做 pre 的 PPT（见第四节「项目汇报」）。**答辩问答已备**：见 `docs/DEFENSE_QA.md`。

**P1 — 提交前**
5. 2–3 条线路打磨 + 难度曲线（`RouteBuilder.routeIndex` 已能切 3 条）。
6. 音效文件 + 接入 `ClimbAudio`（抓握 / 失衡 / 坠落 / 登顶）。
7. 简单美术：材质 / 天空盒 / 握点造型（可选，提升观感）。
8. 跑评测：找 5–8 个同学玩，记录 时间 / 跌落次数（按原因）/ NASA-TLX / SSQ。
9. 把评测结果填进 `REPORT.md`，定稿报告。
10. 最终 demo 视频 + 提交 GitHub 链接。

## 三、按 5 人分工（勾选清单）

> 角色沿用 `TASKS.md`：P1 攀爬系统 / P2 玩法规则 / P3 场景美术 / P4 XR集成与构建 / P5 UX·音频·报告·视频。名字自己认领。

### P1 — 攀爬系统（认领：Claude · 代码 + 引擎内自动化验证已完成）
- [x] 核心数学抽成纯函数 `ClimbMath`（`StabilityScore` / `ClimbDelta`）；`BalanceSystem`、`ClimbController` 改为调用，逻辑更清晰可测
- [x] 逻辑自测 `ClimbMathSelfTest`（组件加上去点 **Run Self-Test**，控制台出 `9/9 passed`）—— 平衡判定 + 反向位移数学已验证正确
- [x] **引擎内端到端验证**：`SimulatedClimber` 机器人驱动真实 gameplay 栈跑通 失衡脱落→坠落重生→登顶计时 全闭环（`VRClimb ▸ Run Headless Check`，10/10 PASS）
- [x] 修复实跑暴露的真 bug：`CharacterController.minMoveDistance` 默认 0.001 会**吞掉慢速攀爬位移**（锚点每帧重置导致丢失不可恢复）→ 置 0；脱落后落地即重生（原先要掉到 y<-10 才触发）
- [~] 反向位移 / 平衡 / 脚法常量的**真人手感**微调 —— 机器人验证了机制正确，手感要等 XR Device Simulator / 真机实玩（P4）
- [~] 报告「攀爬 + 平衡」段 —— 初稿已在 `REPORT.md §3–4`，P1 复核润色即可

> 说明：`[x]` 已完成；`[~]` 代码就绪、等真人进 XR 实玩后做最终调参。

### P2 — 玩法规则（____）
- [x] 验证 登顶 / 跌落重生 / 计时 / 跌落计数 闭环正确 —— 已被无头端到端测试覆盖（10/10 PASS）
- [x] 设计第 4 条线路 —— Endurance 已合并（`RouteCatalog` index 3）
- [ ] 体力 / 易碎点 / 休息点 参数调校（`StaminaSystem`、`ClimbHold`）—— 可在 `Demo.unity` 里切 `routeIndex` 跑着看
- [ ] 报告里写「玩法与线路设计」段（4 条线路各自意图）

### P3 — 场景 / 美术（____）
- [x] 把 `Demo.unity` 当基础升级观感：天空盒 / 更好的墙面 + 握点材质 / 灯光（当前是程序化几何体，能看但朴素）
- [x] （可选）正式场景另存为 `Assets/Scenes/ClimbGym.unity`
- [x] 截图 / 出镜素材给 P5 做视频和 PPT
> 注：演示**不卡美术**——程序化方块墙 + 可见化身已经能录出能看的 demo，美术属加分项。

### P4 — XR 集成与构建（整体可选，按"仿真即可"已降级）
- [x] `Hold` 层 / URP 管线资产 / `ProjectSettings`（Input System 后端）/ packages-lock 已生成并提交
- [ ] （可选）启用 OpenXR、导入 XRI Starter Assets、`PlayerClimberSetup` 接 XR Origin 真机玩（`SETUP §1–2、§6`）
- [ ] （可选）Quest 构建 / 帧率 / 舒适度 vignette
> 关键路径不再依赖本节；仿真演示与录制已覆盖"能演示"的目标。

### P5 — UX · 音频 · 报告 · 视频（部分认领：syh886）
- [x] HUD（计时 + 平衡条 + 状态）`GameHUD` + 一键接线 `VRClimb ▸ Set Up HUD + Audio`
- [x] 占位音效 + `ClimbAudio` 接线（抓握/失衡/坠落/登顶）
- [x] demo 视频可自动产出（`Record Demo` + ffmpeg，见 `TESTING.md`）—— 剩：替占位音效、定稿剪辑（可选）
- [ ] 统筹报告：合各人段落、补**评测真实数据**、润色（基于 `REPORT.md`）
- [ ] 做 pre PPT（`SLIDES.md` 已有大纲；答辩问答见 `DEFENSE_QA.md`）

## 四、项目汇报（6/18 pre）

- **形式**：自愿报名，8 组名额，**可加分**。建议报名。
- **要准备**：PPT（约 8–10 页）+ demo 视频 + 现场讲（每组几分钟，按课堂安排）。
- **PPT 大纲**：
  1. 标题 + 团队成员
  2. 背景与问题：VR 攀岩很火，但**几乎都是纯手、没脚、没平衡**
  3. 我们的点子：在 Quest（仅头 + 两手柄）上加**平衡 + 脚法**层；头显当质心、脚做成抽象状态
  4. 调研支撑：引 Kosmalla (CHI 2020「脚比手更重要」) + Mitsuda & Kimura (Frontiers in VR 2026 头质心模型)
  5. 系统架构：一张数据流图（README 里有现成的）
  6. **Demo**（放视频，重点是失衡变红→踩脚回正那一下）
  7. 评测计划 / 初步结果
  8. 分工与未来工作
- **谁讲**：P5 主讲串场，各模块由对应负责人补 1–2 句技术细节。讲解要点见 `DEMO.md`「talking points」。

## 五、书面报告（7/2）

- 初稿已在 [`REPORT.md`](REPORT.md)（Abstract → Conclusion + References）。
- **待补**：实验真实数据 + 图表、各人贡献声明、整体润色。
- **分工**：各人先写自己模块的 Method / Implementation 段（见上面每人清单最后一条），**P5 合稿定稿**。

## 六、建议时间线

| 时间 | 目标 |
|---|---|
| 本周内 | 进 Unity 跑通 + 调参（P0-1、P0-2）—— **最优先** |
| 6/15 前 | demo 视频雏形 + PPT 雏形 |
| **6/18** | pre（自愿，争取报名加分） |
| 6/19 – 6/25 | 线路 / 音效 / 美术 + 跑评测 |
| 6/26 – 7/2 | 报告定稿 + 最终视频 + **提交** |

---
有疑问先看对应文档（`SETUP / DESIGN / RESEARCH / REPORT / DEMO / CONTRIBUTING`），再群里问。
