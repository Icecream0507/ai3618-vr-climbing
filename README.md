# Summit VR — A Virtual Reality Rock-Climbing Game

> AI3618 (2025–2026 Spring) course project — **Simulator Track**.
> A 5-person group project: climb a wall by physically reaching for and pulling on holds in VR,
> manage your stamina, and reach the summit.

This repository is the **project skeleton**: the core climbing locomotion and game-flow scripts,
a documented project structure, and step-by-step setup. The Unity scene, art, and tuning are built
on top of it (see `docs/SETUP.md`).

---

## 1. Game concept

You are on a climbing wall. Each hand maps to a VR controller. Squeeze the **grip** button while a
hand is near a **hold** to grab it; then *move your real hand* and your body is pulled along the wall
(you fall if you let go of everything). Reach the **finish hold / summit zone** to win. Optional
twists raise the skill ceiling:

- **Stamina** — holds drain stamina; rest on blue "Rest" holds to recover. Run out and your hands slip.
- **Fragile holds** — red holds crumble a moment after you grab them, forcing momentum.
- **Routes & checkpoints** — multiple colour-coded routes; checkpoints so a fall doesn't reset everything.
- **Timer & falls** — scored run for replayability and a clear demo narrative.

## 2. How VR climbing works (the one design decision that matters)

The heart of any VR climbing game is the **locomotion model**. We surveyed the standard approaches:

| Approach | Idea | Verdict |
|---|---|---|
| **Counter-motion (grab-move)** | While a hand grips a fixed point, move the **XR Origin opposite** to the hand's tracked motion, so the gripped point stays put in the world and the body is dragged along. | **Chosen.** Physically intuitive, the de-facto standard (*Gorilla Tag*, *The Climb*), and exactly what Unity XRI's built-in **Climb Provider** does. |
| Teleport / snap | Point and jump between holds. | Loses the physicality that makes climbing fun; rejected as the core. |
| Joystick locomotion | Thumbstick to fly up the wall. | Not "climbing"; breaks immersion and causes sickness. |

Unity's XR Interaction Toolkit ships a **Climb Provider** + **Climb Interactable** that implement
counter-motion out of the box ("Climb locomotion translates the XR Origin counter to movement of
whichever Interactor is selecting a Climb Interactable"). We implement the same maths ourselves in
[`ClimbController.cs`](Assets/Scripts/Climbing/ClimbController.cs) so the mechanic is **transparent,
version-independent, and easy to extend** with stamina / fragile holds / scoring — and we document
the built-in path as a drop-in alternative in `docs/SETUP.md`.

References:
- [Climb Provider — XRI 3.0 manual](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/climb-provider.html)
- [Climbing — XRI 3.0 manual](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/climbing.html)
- [Climb Interactable — XRI 2.4 manual](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.4/manual/climb-interactable.html)
- [Unity VR Basics 2023 – Climbing (tutorial)](https://fistfullofshrimp.com/unity-vr-basics-2023-climbing/)
- [Climbing in VR with the XR Interaction Toolkit (Medium)](https://medium.com/@dnwesdman/climbing-in-vr-with-the-xr-interaction-toolkit-164f6b381ed9)

## 3. Architecture

```
Input (grip)                 Physics (overlap on Hold layer)
     │                                │
     ▼                                ▼
ClimbingHand (×2) ── grab/release ──► ClimbHold ──► (Finish / Fragile / Rest metadata)
     │  exposes HandPosition, CurrentHold
     ▼
ClimbController ── counter-motion + gravity + fall/respawn ──► CharacterController (XR Origin)
     │                                          │
     │ OnPlayerFell / SetCheckpoint             ▼
     ▼                                     Checkpoint (respawn point)
GameManager (state, timer, falls) ──► events ──► GameHUD (timer/stamina/status)
     ▲                                              ▲
SummitTrigger (win)                          StaminaSystem (drain/regen, force-release)
```

| Script | Responsibility |
|---|---|
| `Climbing/ClimbHold.cs` | Marks a climbable hold; type (Normal/Finish/Fragile/Rest), gizmos, break logic. |
| `Climbing/ClimbingHand.cs` | Per-hand grip input + nearest-hold search; raises grab/release; haptics. |
| `Climbing/ClimbController.cs` | Moves the XR Origin: counter-motion while climbing, gravity + fall/respawn otherwise. |
| `Gameplay/GameManager.cs` | Singleton game state, climb timer, fall count, events. |
| `Gameplay/SummitTrigger.cs` | Win volume at the top of the wall. |
| `Gameplay/Checkpoint.cs` | Updates the respawn point as you ascend. |
| `Gameplay/StaminaSystem.cs` | Optional stamina drain/regen; forces a slip at zero. |
| `UI/GameHUD.cs` | World-space timer / stamina bar / status labels (TextMeshPro). |
| `Util/HapticFeedback.cs` | Version-independent controller rumble helper. |

> Scripts compile into the default `Assembly-CSharp` (no `.asmdef`) so package references resolve
> automatically. `.meta` files are generated by Unity on first open — that's expected for a fresh scaffold.

## 4. Project structure

```
VRClimb/
├── Assets/
│   ├── Scripts/{Climbing,Gameplay,UI,Util}/   # the code in this skeleton
│   ├── Scenes/        # ClimbGym.unity (you build this)
│   ├── Prefabs/       # Hold_*, PlayerRig, HUD_Canvas
│   ├── Materials/  Models/  Audio/  Settings/
├── Packages/manifest.json     # XRI, OpenXR, Input System, XR Hands, URP, TMP
├── ProjectSettings/ProjectVersion.txt
└── docs/{DESIGN,SETUP,TASKS}.md
```

## 5. Quick start

1. Install **Unity 2022.3 LTS** (Unity 6 also fine — let it upgrade the project).
2. Unity Hub → **Add project from disk** → select this `VRClimb/` folder and open.
   Package Manager resolves the manifest on first load (if a version warns, accept the suggested one).
3. **Project Settings → XR Plug-in Management** → install, enable **OpenXR** (PC + Android tabs),
   add the **Meta Quest** / interaction-profile feature.
4. Build the scene and test in-editor with the **XR Device Simulator** (no headset needed), or via
   Quest Link / build to the headset. **Full steps are in [`docs/SETUP.md`](docs/SETUP.md).**

## 6. Controls

| Action | Input |
|---|---|
| Grab a hold | **Grip** (when the hand is within reach of a hold) |
| Move up/along the wall | Grab + physically move your hand |
| Let go | Release grip (you fall if no hand is holding) |
| Reach summit | Touch the green finish hold / summit zone |

## 7. Roadmap

- **By 6/18 (optional `pre`, +bonus):** one playable route end-to-end in the simulator — grab, climb,
  fall/respawn, summit + timer. A 60–90s demo video.
- **By 7/2 (submission):** polished wall + ≥2 routes, stamina + fragile holds, audio/haptics, HUD,
  README + report + demo video, GitHub link.

Team split and a dated schedule are in [`docs/TASKS.md`](docs/TASKS.md); design detail and the report
outline are in [`docs/DESIGN.md`](docs/DESIGN.md).

## 8. Tech stack

Unity 2022.3 LTS · URP · OpenXR · XR Interaction Toolkit 2.5 · Input System · (optional) XR Hands · TextMeshPro.

## License

Course project — for educational use within AI3618.
