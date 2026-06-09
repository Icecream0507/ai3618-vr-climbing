# 团队协作指南 — Summit VR (AI3618)

> 给队友的上手 + 协作手册。**一句话规矩：`main` 永远能跑；每个功能开分支；小步多提交多 PR；改场景前先在群里喊一声。**

仓库：https://github.com/Icecream0507/ai3618-vr-climbing
（Unity 2022.3 LTS · URP · OpenXR · XR Interaction Toolkit）

---

## 0. 目录
1. [第一次上手](#1-第一次上手-onboarding) · 2. [分工](#2-分工) · 3. [Git 工作流](#3-git-工作流) ·
4. [Unity + Git 的坑（必读）](#4-unity--git-的坑必读) · 5. [配置场景智能合并](#5-配置场景智能合并每人本地做一次) ·
6. [大文件](#6-大文件模型音频贴图) · 7. [冲突了怎么办](#7-冲突了怎么办) · 8. [节奏与里程碑](#8-节奏与里程碑) ·
9. [命令速查](#9-命令速查) · 10. [文档导航](#10-项目文档导航)

## 1. 第一次上手 (onboarding)

1. **装 Unity**：Unity Hub + **Unity 2022.3 LTS**（要上 Quest 真机就勾 *Android Build Support*：SDK/NDK/OpenJDK）。
2. **装 Git**（[git-scm.com](https://git-scm.com)）；可选装 Git LFS（见 §6）。
3. **拿写权限**：让仓库 owner（**Icecream0507**）在 GitHub *Settings → Collaborators* 把你的账号加进来；
   或者 Fork 一份、改完发 Pull Request。
4. **克隆**：
   ```bash
   git clone https://github.com/Icecream0507/ai3618-vr-climbing.git
   cd ai3618-vr-climbing
   ```
   > 项目在 `VRClimb/` 子目录里（仓库根目录就是它）。
5. **设置 git 身份**（第一次用 git 的话）：
   ```bash
   git config --global user.name "你的名字"
   git config --global user.email "你的GitHub邮箱"
   ```
6. **打开项目**：Unity Hub → *Add project from disk* → 选这个文件夹。首次打开 Unity 会生成 `Library/` 和大量 `.meta`
   文件，**这是正常的**（`Library/` 不进 git，`.meta` 要进，见 §4）。
7. **跑通 v1**：照 [`docs/SETUP.md`](docs/SETUP.md) **§6 快速路径** —— 建 `Hold` 层、给 XR Origin 挂
   `PlayerClimberSetup` 一键接线、挂 `RouteBuilder` + `GameManager`，用 **XR Device Simulator** 按 Play 即可，
   不需要头显、不需要美术。
8. **配置场景智能合并**：照 §5 本地跑一次（强烈建议，省掉后面场景冲突的痛苦）。

## 2. 分工

详见 [`docs/TASKS.md`](docs/TASKS.md)（P1–P5：攀爬系统 / 玩法规则 / 场景美术 / XR集成与构建 / UX音频报告视频）。
**原则：谁负责的模块，改它的 PR 就由谁 review。**

## 3. Git 工作流

我们用「**功能分支 + Pull Request**」，不直接往 `main` 推（除了改文档的小修）。

- **分支命名**：`feat/功能名`、`fix/bug名`、`docs/...`，或 `名字/功能名`。例：`feat/stamina-tune`、`fix/foot-snap`。
- **永远从最新 main 开分支**：
  ```bash
  git switch main && git pull            # 先同步
  git switch -c feat/your-feature        # 再开分支
  ```
- **小步提交**，提交信息用祈使句、动词开头（英文优先，中文也行）：
  ```bash
  git add -A
  git commit -m "Add foot marker prefab and wire to FootPlacementSystem"
  ```
- **推分支并开 PR**：
  ```bash
  git push -u origin feat/your-feature
  ```
  到 GitHub 点 *Compare & pull request* → 目标 `main` → 指派一位 reviewer → 合并。
- **每天开工先 `git pull`，收工前把分支推上去**，别攒一大坨再合（冲突会爆炸）。
- **`main` 必须保持能编译、能跑**。合并前确认你的分支在 Unity 里不报错。

## 4. Unity + Git 的坑（必读）

这是团队做 Unity 最容易翻车的地方，务必看完：

1. **`.meta` 文件必须和资源一起提交。** 你新建/删除/重命名/移动任何文件，Unity 都会生成或改对应的 `.meta`
   （里面是资源的唯一 ID 和引用）。**少提交 `.meta` → 别人那边引用全丢、组件脱挂。** 永远用 `git add -A` 一起加。
2. **不要两个人同时改同一个 `.unity` 场景或 `.prefab`。** 它们是 YAML 文本，但结构复杂，合并冲突极难手解。约定：
   - 场景文件由 **P3（场景/美术）统一保管**，别人要改先在群里喊、错开时间；
   - 尽量把功能做成**独立 prefab**，各自改各自的 prefab，最后再拖进场景，减少对大场景文件的争抢。
3. **确认 Unity 协作设置**（*Edit → Project Settings → Editor*）：
   - **Asset Serialization → Mode = Force Text**（便于 diff/合并；2022.3 默认就是）
   - **Version Control → Mode = Visible Meta Files**（默认就是）
4. **这些目录不进 git**（`.gitignore` 已忽略）：`Library/`、`Temp/`、`Obj/`、`Build(s)/`、`Logs/`、`UserSettings/`、
   `*.csproj`、`*.sln`。**千万别手动 `git add` 这些**——它们是本地生成的，提交了会互相覆盖、撑爆仓库。
5. **切分支后 Unity 可能要重新导入资源**，让它自己跑完即可；不要因为 `Library/` 变了就去提交它。
6. **脚本无 `.asmdef`**，全部编进默认 `Assembly-CSharp`，新加 `.cs` 不用改工程配置。

## 5. 配置场景智能合并（每人本地做一次）

项目根的 `.gitattributes` 已声明 `.unity/.prefab/.asset/.mat` 等用 Unity 自带的 **UnityYAMLMerge** 来合并。
每人在**本地** git 里把这个合并工具指过去（路径按你装的 Unity 版本改）：

**Windows（示例，Unity 2022.3.40f1）：**
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/2022.3.40f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```
**macOS（示例）：**
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/2022.3.40f1/Unity.app/Contents/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
```
> 没配也不会坏，只是场景/prefab 冲突时要手解；配了之后 `git merge` 会自动调用它。

## 6. 大文件（模型/音频/贴图）

- 小素材（几百 KB 的图/音效）直接进 git 没问题。
- 有**大文件**（几十 MB 的 `.fbx` / `.wav` / 高清贴图）时，建议用 **Git LFS**：
  ```bash
  git lfs install
  git lfs track "*.fbx" "*.wav" "*.psd"
  git add .gitattributes
  ```
  `.gitattributes` 里已经留了注释模板，取消注释即可。
- ⚠️ **全队要么都用 LFS、要么都不用**，否则有人拉下来是指针文件、有人是真文件，会乱。统一后再用。

## 7. 冲突了怎么办

- **`.cs` 代码冲突**：正常手动解（看 `<<<<<<<` / `=======` / `>>>>>>>` 标记）。
- **场景 / prefab 冲突**：配了 §5 的话 `git merge` 会自动用 SmartMerge；解不了就**找改动的那两人当面合**，或一方
  redo（在最新 main 上重做改动）。预防永远比解决便宜——回看 §4 第 2 条。
- **本地有改动又想先拉**：`git stash` 暂存 → `git pull` → `git stash pop` 取回。
- **彻底乱了别硬刚**：先 `git status` 看清楚，拿不准就群里问，别 `--force` 推 `main`。

## 8. 节奏与里程碑

- **6/18（16 周四）**：自愿 pre，争取出一个可演示的 v1（抓→爬→失衡→坠落重生→登顶计时）。
- **7/2（18 周日）**：提交 GitHub 链接 + Demo 视频。
- 建议**每周固定同步一次进度**，对齐分工和合并计划。详细排期见 [`docs/TASKS.md`](docs/TASKS.md)。

## 9. 命令速查

```bash
# 开新功能
git switch main && git pull
git switch -c feat/my-feature

# 日常提交
git add -A
git commit -m "Describe what you did"
git push -u origin feat/my-feature      # 第一次推；之后直接 git push

# 同步别人的改动到自己分支
git switch main && git pull
git switch feat/my-feature
git merge main                          # 或 git rebase main

# 看状态 / 历史
git status
git log --oneline -10

# 暂存当前改动去做别的
git stash            # 存
git stash pop        # 取回
```

## 10. 项目文档导航

| 文档 | 内容 |
|---|---|
| [`README.md`](README.md) | 项目总览、玩法、架构、颜色图例 |
| [`docs/SETUP.md`](docs/SETUP.md) | 搭场景步骤（**§6 = 最快跑通 v1**） |
| [`docs/DESIGN.md`](docs/DESIGN.md) | 设计细节 + **报告大纲** |
| [`docs/RESEARCH.md`](docs/RESEARCH.md) | 市场/文献调研（写报告可直接引用） |
| [`docs/TASKS.md`](docs/TASKS.md) | 5 人分工 + 排期 |

有问题先看上面文档，再群里问。Happy climbing 🧗
