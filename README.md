# Summit VR — A Virtual Reality Bouldering Game

> AI3618 (2025–2026 Spring) course project — **Simulator Track**.
> A 5-person group project: a VR **bouldering** game where the fun is not just grabbing holds but
> keeping your **balance** and using **footwork** to reach the top.

This repository is the **project skeleton + a playable v1**: counter-motion climbing, a lightweight
balance/footwork layer, and a procedural route builder so the game runs in an empty scene without any
art. The full market/literature survey that shaped the design is in
[`docs/RESEARCH.md`](docs/RESEARCH.md).

---

## 1. Game concept

You are on a bouldering wall. Each hand maps to a VR controller — squeeze **grip** near a hold to grab
it, then move your real hand to pull your body along the wall (counter-motion, like *The Climb* /
Unity XRI). What makes it **bouldering** rather than monkey-bars:

- **Balance** — your head is your centre of mass. Reach too far to one side without support and you
  start to **slip** (balance bar drains, turns red); recover by getting balanced again.
- **Footwork** — colour-coded holds suggest hand vs foot placement; **virtual feet** auto-snap to nearby holds below your body (like real rock, any hold can take a hand or a foot — colour is a hint, not a hard rule). Good footwork widens your support base and keeps you balanced through hard moves.
- **Summit & score** — reach the green finish / summit zone to win; a timer and fall count give a run
  to beat. Optional **stamina** and **fragile holds** add challenge.

Why this design and not literal foot tracking: Quest tracks only head + 2 controllers, so feet are an
**abstracted state** and balance uses the **head as a CoM proxy** — an evidence-based choice (Mitsuda
& Kimura 2026; Kosmalla 2020), not a shortcut. Almost no shipped VR climber does CoM balance, so this
is our differentiator. See [`docs/RESEARCH.md`](docs/RESEARCH.md).

### Hold colour legend

Colours suggest intended use (like tape on a real wall); **any hold accepts a hand or a virtual foot**.

| Colour | Suggested use |
|---|---|
| 🟡 Yellow | Hand |
| 🟠 Orange | Foot |
| 🟣 Purple | Either |
| 🟢 Green | Finish |
| 🔴 Red | Fragile (breaks if held too long) |
| 🔵 Blue | Rest (no stamina drain) |

## 2. Locomotion model

Counter-motion (grab-and-pull): while a hand grips a fixed hold, the **XR Origin moves opposite** to
the hand's tracked motion, so the gripped point stays put and your body is dragged along — the de-facto
standard (*The Climb*, *Gorilla Tag*) and what Unity XRI's **Climb Provider** does. We implement the
maths ourselves in [`ClimbController.cs`](Assets/Scripts/Climbing/ClimbController.cs) so it's
transparent and easy to extend with balance/footwork. See the survey for the alternatives we rejected
(teleport, joystick).

## 3. Architecture

```
Input (grip)                         Physics overlap (Hold layer)
     │                                        │
     ▼                                        ▼
ClimbingHand (×2) ──grab/release──►  ClimbHold {role: Hand/Foot/Either, type}
     │                                        ▲
     │                              FootPlacementSystem ──auto-snap "virtual feet" to foot holds
     ▼                                        │
ClimbController ──counter-motion + gravity + fall/respawn──► CharacterController (XR Origin)
     ▲   │ contacts (hands+feet) keep you on the wall                    │
     │   └──────────────► BalanceSystem (head = CoM; lateral support test)
     │ PeelOff (balance==0) ◄───────────────────┘  drains when you lean out
GameManager (state/timer/falls) ──events──► GameHUD (timer, stamina bar, balance bar)
SummitTrigger (win)   Checkpoint (respawn)   StaminaSystem (optional)
RouteBuilder ──spawns wall + holds + summit from primitives (v1, no art needed)
```

| Script | Responsibility |
|---|---|
| `Climbing/ClimbHold` | Climbable hold; **role** (Hand/Foot/Either) + type (Finish/Fragile/Rest); gizmo colours. |
| `Climbing/ClimbingHand` | Per-hand grip + nearest hand-hold search; grab/release; haptics. |
| `Climbing/ClimbController` | Moves the rig: counter-motion while climbing, gravity + fall/respawn; peel-off on balance loss. |
| `Climbing/FootPlacementSystem` | Auto-places 1–2 virtual feet on nearby foot/either holds (no IK); feeds balance. |
| `Climbing/BalanceSystem` | **Headline mechanic.** Head-as-CoM lateral support test → graded balance meter → peel-off. |
| `Gameplay/GameManager` | Singleton state, timer, fall count, events. |
| `Gameplay/SummitTrigger` · `Checkpoint` | Win zone · respawn points. |
| `Gameplay/StaminaSystem` | Optional stamina drain/regen. |
| `Gameplay/RouteDefinition` · `RouteBuilder` | Data + procedural builder for a v1 route from primitives. |
| `Gameplay/PlayerClimberSetup` | One-click wiring of the climber components onto an XR Origin. |
| `UI/GameHUD` · `Util/HapticFeedback` | Timer/stamina/balance HUD · controller rumble. |

> Scripts compile into the default `Assembly-CSharp` (no `.asmdef`). `.meta` files are generated by
> Unity on first open — expected for a fresh scaffold.

## 4. Quick start (playable v1)

1. Install **Unity 2022.3 LTS** (Unity 6 also fine; the repo state is verified compiling on
   Tuanjie 2022.3.62t7 / XRI 3.2.1). Unity Hub → *Add project from disk* → open this
   `VRClimb/` folder; let Package Manager resolve the manifest.
2. *Project Settings → XR Plug-in Management* → enable **OpenXR**; import XRI **Starter Assets** +
   **XR Device Simulator**.
3. Create a layer named **`Hold`** (Edit → Project Settings → Tags and Layers).
4. New scene → add the **XR Origin (XR Rig)** prefab. Add **`PlayerClimberSetup`** to it, assign the
   HMD + both controllers, run its context menu **Set Up Climber**, then in the Inspector set each
   hand's grip action and the **Hold** layer on the hands + `FootPlacementSystem`. (No Player tag
   needed — the summit/checkpoints detect the climber via its `ClimbController`.)
5. Add an empty GameObject + **`RouteBuilder`** (Hold layer name = `Hold`) and a **`GameManager`**.
   Press **Play** with the **XR Device Simulator** — the wall, holds and summit build automatically;
   grab, climb, balance, and top out.

> **Shortcut:** the editor menu **`VRClimb ▸ Set Up Test Scene`** auto-creates the `Hold` layer plus a
> `GameManager` and `RouteBuilder`, and attaches `PlayerClimberSetup` to an XR Origin if present.
> Pick a route with `RouteBuilder.routeIndex` (0 = Warm-up, 1 = Balance Test, 2 = The Arete).

### Automated end-to-end check (no headset needed)

The repo ships a CI-style test that opens a generated scene and lets a **scripted robot climber**
drive the real gameplay stack: it grabs an isolated hold, leans out of support until the balance
meter peels it off the wall, falls, respawns, then climbs Route 0 hand-over-hand to the summit.

- In-editor: menu **`VRClimb ▸ Run Headless Check`** (or open `Assets/Scenes/SimTest.unity` and press
  Play to *watch* the robot climb).
- Command line (what CI / teammates without a headset run):

  ```
  Tuanjie.exe -projectPath . -batchmode -nographics ^
      -executeMethod VRClimb.EditorTools.HeadlessCheck.Run -logFile Logs/e2e.log
  ```

  Exit code 0 = pass; the human-readable report lands in `Logs/headless-check.txt`.
  Current status: **ClimbMath 9/9 + end-to-end sim 10/10 PASS** (summit in ~6 s, 1 scripted fall).

Full step-by-step (including a Quest build) is in [`docs/SETUP.md`](docs/SETUP.md).

## 5. Controls

| Action | Input |
|---|---|
| Grab a hand hold | **Grip** near a yellow/purple hold |
| Move along the wall | Grab + physically move your hand |
| Place a foot | Move your body so an orange/purple hold is below you (auto) |
| Stay balanced | Keep support under you (a foot or an opposite-side hold) when reaching far |
| Let go | Release grip (you fall if you have no contacts or lose balance) |

## 6. Roadmap

- **By 6/18 (optional `pre`, +bonus):** the v1 route end-to-end in the simulator — grab, climb, a
  balance slip you can recover from, fall/respawn, summit + timer. 60–90 s demo video.
- **By 7/2 (submission):** 2–3 hand-authored routes, tuned balance + stamina + fragile holds,
  audio/haptics, HUD, report + demo video, GitHub link.

Docs: [`docs/TESTING.md`](docs/TESTING.md) (**run the sim / record the demo — no headset**) ·
[`docs/DEFENSE_QA.md`](docs/DEFENSE_QA.md) (**presentation Q&A prep**) ·
[`docs/SETUP.md`](docs/SETUP.md) (build the scene) · [`docs/DESIGN.md`](docs/DESIGN.md) (design) ·
[`docs/RESEARCH.md`](docs/RESEARCH.md) (survey) · [`docs/REPORT.md`](docs/REPORT.md) (draft course report) ·
[`docs/DEMO.md`](docs/DEMO.md) (video checklist) · [`docs/TASKS.md`](docs/TASKS.md) (5-person split) ·
[`docs/REMAINING.md`](docs/REMAINING.md) (remaining tasks + presentation plan) ·
[`CONTRIBUTING.md`](CONTRIBUTING.md) (team git workflow).

A pre-rendered demo clip is committed at [`Demo/SummitVR_demo.mp4`](Demo/SummitVR_demo.mp4) (~21 s,
auto-generated by the robot playthrough — see TESTING.md §4 to re-record).

## 7. Tech stack

Unity 2022.3 LTS · URP · OpenXR · XR Interaction Toolkit 3.2 · Input System · TextMeshPro.

## License

Course project — for educational use within AI3618.
