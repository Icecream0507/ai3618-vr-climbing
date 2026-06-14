# Design Notes & Report Outline

## 1. Pillars

1. **Physicality** — climbing must feel like *you* moving your body, not pressing a button.
   This is why locomotion is counter-motion (grab-and-pull), not teleport or stick.
2. **Readable challenge** — colour-coded holds communicate rules at a glance
   (yellow normal, green finish, red fragile, blue rest).
3. **Short, repeatable runs** — a route is 60–120 s; a timer + fall count gives a score to beat,
   which also makes for a tight demo video.

## 2. Mechanics detail

### Reach limit (where the difficulty comes from)
- Hands and feet have a finite reach: a hold can only be **grabbed when it's within arm's length of
  the shoulder** (`ClimbingHand.armReach`, ~0.88 m = bone length `BodyMetrics.ArmReach` + shoulder
  mobility / a committing lunge). Feet only snap to foot-holds within a hip-relative `footReach`.
- This is what makes the wall a *puzzle* rather than a free teleport: to reach a far hold you must
  first move your body into range (shift weight, step a foot up to widen support, then extend). On a
  real headset the player's physical arm enforces the same limit; we add it in code so the simulation
  and the on-wall difficulty match. A held hold is dropped only on an absurd over-reach (safety net).

### Gravity & "weight" (kinematic, not a rigidbody sim — deliberate)
- We do **not** run a Rigidbody/ragdoll physics sim (high-constraint climbing poses make it jitter,
  and it's orthogonal to the balance/footwork core — see the "keep it simple" scope). Gravity shows
  up two honest ways: (1) off the wall → you fall and respawn; (2) on the wall → you don't float for
  free, you *fight* gravity via **stamina** (grip fatigue) and **balance** (lean past support → peel
  off). That's the real climbing struggle, modelled without a physics engine.
- The demo avatar's sense of weight is a damped spring-pendulum on the hips + 2-bone IK with joint
  limits (`HumanoidRig`) — a kinematic *look* of gravity for the spectator video, not used in play.
  It is built to read as a real climber rather than a stiff mannequin, benchmarked against the Klifur
  reference (`Demo/climbing.gif`, a physics-based climbing game whose torso is a dynamic rigidbody
  with hands/feet jointed to holds). Our kinematic approximation of that feel:
  - **The body hangs from the hands, not from an upright head.** The pelvis is the pendulum; the
    spine is *derived* from it, aiming pelvis → toward whatever bears load (grips, else feet). So a
    one-arm hang tilts the whole torso under that arm — the diagonal body tension of real climbing.
  - **The head faces the wall almost all the time** (a climber reads the holds in front of and above
    them). It only *glances* partway toward the next hold (or down at the feet on an overhead match),
    clamped to a neck range-of-motion cone so it never swivels away from the wall, and rate-damped so it
    eases rather than snaps. Never bolt upright — it tips gently with the body, visible face toward the wall.
  - **Spinal torsion / hip-twist (constrained):** only a genuine *one-arm* hang blades the torso — the
    reaching-side hip turns into the wall and the shoulders counter-rotate (real technique); on two hands
    or in the air the body relaxes back to square with the wall, so its forward orientation stays honest.
    The twist is rate-limited, a damped rotational constraint rather than a free spin.
  - **Support-aware pose:** standing over planted feet (upright, stiffer spring) vs. dangling below
    the grips (saggier, swingier); dangling legs trail the hip swing as pendulum secondary motion.
  - **Joint limits everywhere:** elbows/knees never hyperextend; a hold beyond reach leaves the limb
    visibly taut and short of it (this is what makes the "impossible route" demo read as unclimbable).
  - **Hands, feet & face are real, and stay attached:** gripping fists and dark climbing shoes sit at
    the *IK end-effector* (the actual limb tip), so they never float off the arm/leg even at a full
    out-of-reach stretch — the hand simply stops short of the hold. The head carries a simple face
    (eyes + a hair cap) so it reads as a person, not a blank egg, watching the next hold.
  - **Clean, simple silhouette (matched to the Klifur figure):** one rounded torso (tank top) over
    shorts, smooth bare-skin arms and lower legs, and a plain head — no chunky joint balls, no extra
    belly/nose primitives. The earlier build stacked ~10 cosmetic spheres (8 joint balls + belly +
    nose) that made the climber read as a segmented mannequin; stripping them to flat-shaded tubes
    gives the minimal, readable look of the reference GIF. `docs/avatar-evolution.png` shows the
    before/after side by side.
  - **Legible human motion (recorded demo):** each reach eases in/out (accelerates off the body,
    decelerates into the hold) instead of sliding at constant velocity; on an overhead hand-match the
    head drops its gaze to scan footwork (what a climber actually does) rather than burying straight up
    between the two arms. The spectator camera looks slightly down onto the head so it stays seated on
    the shoulders. All of this is **demo-only** — the headless test drives the same stack with the fast
    linear motion, so the 10/10 end-to-end assertions are unaffected.
  - **Contact highlight:** a hold lights up bright (emissive) green the instant a hand grips it or a
    foot is planted on it, and reverts to its resting colour the moment that contact leaves — so the
    spectator can read *which* four points are currently loaded at a glance. Implemented as a shared
    material swap on `ClimbHold` (hand grippers + a foot count from `FootPlacementSystem`), guarded so
    it's a no-op when there's no render pipeline (headless), leaving gameplay/e2e untouched.

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

---

## 7. v1 bouldering pivot — balance + footwork (implemented)

The v1 reframes the game from generic hand-climbing to **bouldering whose core skill is balance and
footwork**, on the honest constraint that Quest tracks only head + 2 controllers. Full survey and
citations: [`RESEARCH.md`](RESEARCH.md).

**What's implemented**

- **Balance** (`BalanceSystem`): the HMD is a centre-of-mass proxy; contacts = gripped hand-holds +
  auto-placed virtual feet. Stability = whether the CoM's lateral position sits within the contacts'
  lateral span (a 1-D support interval / simplified barn-door). Leaning out drains a balance meter
  (with hysteresis + grace time to absorb head jitter); at zero you **peel off** and fall. Grounded
  in Mitsuda & Kimura (Frontiers in VR 2026) head-as-CoM and the climbing barn-door concept.
- **Footwork** (`FootPlacementSystem`): 1–2 virtual feet auto-snap to the nearest **foot/either**
  holds in a stance zone below the body and stay glued until you climb past them. Feet are an
  abstracted *state* (not IK legs / not Generative Legs, which fail in climbing poses) — they exist
  to widen your support base, so footwork is what gets you through hard moves.
- **Hold roles + colour legend:** `ClimbHold.role` ∈ {Hand, Foot, Either} is now only a **visual
  hint** of intended use, *not* a restriction — like real rock, **any hold takes a hand or a foot**
  (the old hand-only / foot-only gates in `ClimbingHand`/`FootPlacementSystem` were removed). Legend
  **yellow=hand-ish, orange=foot-ish, purple=either, green=finish, red=fragile, blue=rest** (chosen to
  avoid clashing with the research's green=hand on our existing green=finish); colours hint, they don't gate.
- **v1 route** (`RouteBuilder`): builds a wall + colour-coded holds + summit from primitives so the
  game runs with no art. The baked route deliberately includes a **same-side stretch** (two right-hand
  holds in a row) that forces a left foot/flag — so balance + footwork are provably load-bearing, not
  decorative.

**Tuning knobs** (all empirical, expect to tweak): `BalanceSystem.supportMargin / maxOvershoot /
drain / regen / graceTime`; `FootPlacementSystem.bodyDrop / stanceHalfWidth / footReach`.

**Deferred stretch goals:** torso-lean "flag" input (STRIDE shows it's comfortable), hips-to-wall
penalty, ZMP/momentum so dynos destabilise, capture-point "save-yourself" hold highlight, cosmetic IK
legs, procedural route generation, hand-tracking-vs-controller comparison study.

**Report angle this enables:** "adding an HMD-CoM balance + abstracted footwork layer onto
counter-motion VR climbing increases perceived realism/challenge (cite Kosmalla 2020 for feet-matter,
Mitsuda & Kimura 2026 for the model) without raising sickness" — a defensible, citable contribution,
since no shipped commercial VR climber does CoM-based balance. Instrument: time-to-summit, falls split
by **cause** (let-go vs balance-slip vs stamina), grab success rate, balance-margin over time.
