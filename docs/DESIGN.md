# Design Notes & Report Outline

## 1. Pillars

1. **Physicality** — climbing must feel like *you* moving your body, not pressing a button.
   This is why locomotion is counter-motion (grab-and-pull), not teleport or stick.
2. **Readable challenge** — colour-coded holds communicate rules at a glance
   (yellow normal, green finish, red fragile, blue rest).
3. **Short, repeatable runs** — a route is 60–120 s; a timer + fall count gives a score to beat,
   which also makes for a tight demo video.

## 2. Mechanics detail

### Grab
- A hand grabs the **nearest** `ClimbHold` within `grabRadius` when grip crosses `gripThreshold`.
- "Nearest within reach" (overlap sphere) is more forgiving than "must intersect collider", which
  matters a lot in VR where depth perception of small holds is hard.

### Move (counter-motion)
- The active hand pins an anchor in world space; each frame the rig moves by `anchor − handNow`,
  then re-pins. Net effect: the gripped point stays fixed, the body is dragged. See
  `ClimbController.ClimbStep()`.
- **Two hands:** the most recently grabbed hand drives (matches Unity XRI). On release, control
  passes to the other hand if still held. This avoids fighting between two simultaneous anchors and
  gives natural hand-over-hand climbing.

### Fall & respawn
- No hand holding → gravity via `CharacterController`. Below `fallResetY` → respawn at last
  `Checkpoint` (or spawn), `FallCount++`. The CharacterController is toggled off/on around the
  teleport so the move isn't blocked by collision.

### Stamina (optional)
- Drain while gripping non-Rest holds; regen otherwise; zero → forced slip. Turns a traversal into a
  resource-management puzzle and is a clean knob for difficulty.

## 3. Stretch goals (pick by remaining time)
- Dynamic holds (swinging ropes, moving holds), wind gusts, multiple routes with a route selector.
- Hand-tracking control (XR Hands) as a second interface to **compare** against controllers — this
  doubles as a small HCI study and connects to the course's "VR dexterous interface" theme.
- Leaderboard / ghost replay of your best run.

## 4. Risks
| Risk | Mitigation |
|---|---|
| Motion sickness from counter-motion | Keep movement 1:1 with the hand (no amplification); vignette on fast moves; comfort options. |
| Grabbing feels finicky | Tune `grabRadius`, add a hover highlight + haptic on grab. |
| No headset for some members | XR Device Simulator lets everyone develop/test logic without hardware. |
| Scope creep | Lock the 6/18 vertical slice first (one route, full loop), then add depth. |

## 5. Evaluation we can report on
- **Quantitative:** time-to-summit, fall count, average grab success rate (grabs that found a hold ÷
  grip presses), frame rate on device.
- **Qualitative / HCI:** short playtest with classmates (n≈5–8): perceived effort (NASA-TLX), comfort
  (SSQ), preference between controller vs hand-tracking if both are implemented.

## 6. Report outline (maps to the academic format the team wants)
1. **Abstract** — what we built and the main finding (e.g. counter-motion + stamina makes for an
   immersive, low-sickness climbing loop; controllers beat hand-tracking on grab reliability).
2. **Introduction** — VR locomotion problem, why climbing, goals.
3. **Related Work** — VR locomotion taxonomies; commercial climbing VR (*The Climb*, *Gorilla Tag*);
   Unity XRI climbing; (if used) hand-tracking interaction literature. *Cite 2021–2026.*
4. **Method** — system architecture (this repo), counter-motion maths, gameplay systems.
5. **Implementation** — Unity/XRI setup, scene, tuning values.
6. **Experiment** — playtest protocol, metrics above.
7. **Results & Discussion** — numbers + plots, what worked, comfort trade-offs, limitations.
8. **Conclusion & Future Work**.
9. **References** · **Contributions** (who did what) · **Demo video link**.
