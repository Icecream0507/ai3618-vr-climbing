# 剩余任务与分工 —— 冲刺 6/18 pre · 7/2 提交

> 给全组 5 人看的「还剩什么、谁做什么」。打勾推进。截止：**6/18 自愿 pre（可加分）**、**7/2 提交（GitHub 链接 + Demo 视频）**。

## 一、现状（已完成的，别重复造轮子）

- ✅ **代码 v1 全部写完并已推送**：手抓攀爬（反向位移）+ 平衡系统（头显当质心）+ 脚法（虚拟脚自动吸附）+ 3 条程序化线路 + 音效事件钩子 + HUD + 编辑器一键建场景。
- ✅ **文档齐全**：`README` · `SETUP`（§6 最快跑通）· `DESIGN` · `RESEARCH`（调研）· `REPORT`（报告初稿）· `DEMO`（录制清单）· `CONTRIBUTING`（git 协作）· `TASKS`（角色分工）。
- ✅ 仓库 + 协作者已配置。
- ⚠️ **头号缺口：还没有人在 Unity 里真正打开 / 编译 / 跑过。** 真正的编译错、手感、抖动只有进引擎才暴露——这是现在最该先做的事。

## 二、剩余任务总览（P0 = 6/18 前必须，P1 = 7/2 前）

**P0 — 冲一个能演示的版本**
1. 在 Unity 打开项目、编译通过、跑通最小闭环（抓 → 爬 → 失衡 → 坠落重生 → 登顶计时）。
2. 调平衡 / 脚法参数到手感正常（`BalanceSystem`、`FootPlacementSystem` 上的常量）。
3. 录 60–90s demo 视频（照 `DEMO.md`）。
4. 做 pre 的 PPT（见第四节「项目汇报」）。

**P1 — 提交前**
5. 2–3 条线路打磨 + 难度曲线（`RouteBuilder.routeIndex` 已能切 3 条）。
6. 音效文件 + 接入 `ClimbAudio`（抓握 / 失衡 / 坠落 / 登顶）。
7. 简单美术：材质 / 天空盒 / 握点造型（可选，提升观感）。
8. 跑评测：找 5–8 个同学玩，记录 时间 / 跌落次数（按原因）/ NASA-TLX / SSQ。
9. 把评测结果填进 `REPORT.md`，定稿报告。
10. 最终 demo 视频 + 提交 GitHub 链接。

## 三、按 5 人分工（勾选清单）

> 角色沿用 `TASKS.md`：P1 攀爬系统 / P2 玩法规则 / P3 场景美术 / P4 XR集成与构建 / P5 UX·音频·报告·视频。名字自己认领。

### P1 — 攀爬系统（认领：Claude · 代码部分已完成，引擎内手感调参待 P4 跑通后进行）
- [x] 核心数学抽成纯函数 `ClimbMath`（`StabilityScore` / `ClimbDelta`）；`BalanceSystem`、`ClimbController` 改为调用，逻辑更清晰可测
- [x] 逻辑自测 `ClimbMathSelfTest`（组件加上去点 **Run Self-Test**，控制台出 `9/9 passed`）—— 平衡判定 + 反向位移数学已验证正确
- [~] 反向位移手感（`ClimbController`）—— 默认值已设；**最终手感需进 Unity 实跑再微调**
- [~] 平衡常量 `supportMargin/maxOvershoot/drain/regen/graceTime`（`BalanceSystem`）—— 默认合理，进引擎后按手感调
- [~] 脚法常量 `footReach/stanceHalfWidth/bodyDrop`（`FootPlacementSystem`）—— 同上
- [~] 报告「攀爬 + 平衡」段 —— 初稿已在 `REPORT.md §3–4`，P1 复核润色即可

> 说明：`[x]` 已完成；`[~]` 代码就绪、等进 Unity 实跑后做最终调参（这一步需要 P4 先打开引擎）。

### P2 — 玩法规则（____）
- [ ] 验证 登顶 / 跌落重生 / 计时 / 跌落计数 闭环正确
- [ ] 体力 / 易碎点 / 休息点 参数调校（`StaminaSystem`、`ClimbHold`）
- [ ] 设计第 4 条线路（参考 `RouteCatalog` 写法）
- [ ] 报告里写「玩法与线路设计」段

### P3 — 场景 / 美术（____）
- [ ] 用 `VRClimb ▸ Set Up Test Scene` 建场景并保存为 `Assets/Scenes/ClimbGym.unity`
- [ ] 握点造型 / 材质 / 墙面 / 天空盒（替换原始几何体）
- [ ] 灯光 + 一点环境氛围
- [ ] 截图 / 出镜素材给 P5 做视频和 PPT

### P4 — XR 集成与构建（____）
- [ ] 启用 OpenXR、导入 XRI Starter Assets、建 `Hold` 层（`SETUP §1–2`）
- [ ] 用 `PlayerClimberSetup` 接好 XR 装备，绑 grip action（`SETUP §6`）
- [ ] 决定真机 or 仿真；要真机就出 Quest 构建
- [ ] 测帧率 / 舒适度（晕动），必要时加 vignette

### P5 — UX · 音频 · 报告 · 视频（____）
- [ ] HUD 摆放（计时 + 平衡条）、世界空间 Canvas
- [ ] 找 / 录音效，接 `ClimbAudio`
- [ ] 统筹报告：合各人段落、补实验数据、润色（基于 `REPORT.md`）
- [ ] 录最终 demo 视频（照 `DEMO.md`）
- [ ] 做 pre PPT（见下）

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
