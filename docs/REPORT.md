# Summit VR: A Balance-and-Footwork Layer for Controller-Only VR Bouldering

**Course:** AI3618

**Author:** Group N — \<names\>

## Abstract

Virtual reality (VR) climbing is a popular and comfortable genre, but shipped titles such as *The Climb* and *Gorilla Tag* treat the climber as a pair of disembodied hands: the body follows the arms, and the player never has feet to place or weight to balance. This omits the two skills that define real bouldering. We present **Summit VR**, a Unity bouldering game for the Meta Quest that adds a lightweight **balance and footwork layer** on top of standard counter-motion grab-and-pull climbing, using only the 3-point tracking (head and two controllers) available on stock hardware, with no foot trackers. We treat the head-mounted display as a centre-of-mass proxy and test whether its lateral position lies within the lateral support span of the active contacts — gripped hand-holds plus 1–2 abstracted "virtual feet" that auto-snap to foot-holds below the body. Leaning beyond the support span drains a graded balance meter (with hysteresis and a grace time to absorb head jitter) until the climber peels off. Routes include a forced same-side reach that makes footwork load-bearing rather than decorative. The system is implemented but **not yet playtested**; we therefore report a study design with planned metrics (time-to-summit, falls split by cause, balance margin, NASA-TLX, SSQ) targeting the hypothesis that this layer raises perceived realism and challenge without raising simulator sickness.

## 1. Introduction

VR climbing is one of the more successful applications of room-scale VR: it maps a strenuous real-world activity onto a comfortable, low-sickness interaction, because the dominant locomotion technique — **counter-motion grab-and-pull** — keeps virtual motion tightly coupled to the player's own arm movement. When a hand grips a hold, the play space is translated opposite to the hand's tracked motion so that the gripped point stays fixed in the world, dragging the body upward. This is the principle behind Unity's XR Interaction Toolkit Climb Provider [11], *The Climb* and *The Climb 2* (Crytek) [3], and the arm-based traversal of *Gorilla Tag* [1]. It is proven, intuitive, and rarely nauseating.

It is also, almost universally, **hands-only**. In the commercial canon the climber has no feet and no balance: you ascend purely by reaching and pulling, and you fall only by letting go or failing to reach. *The Climb* renders disembodied hands with no legs; *Gorilla Tag* is explicitly legless; *To The Top* substitutes arm-swing momentum for footwork; *Climbey* reads optional Vive foot trackers only to calibrate floor height, not for gameplay. Yet in real bouldering, **feet and balance are the core skill**, not an afterthought — body position over the feet, flagging, and managing the "barn-door" swing that opens up when your holds are all on one side are what separate a clean send from a fall. The recent flatscreen title *New Heights: Realistic Climbing and Bouldering* (2026) [2] advertises exactly these mechanics as its selling point, evidence of player appetite, but it is third-person flatscreen and its balance is abstracted rather than driven by the player's own body.

**This is the gap we target.** Two findings frame the opportunity. Kosmalla et al. (2020) [5] show, in an instrumented real-wall study, that *showing virtual feet matters more than showing virtual hands* for perceived movement accuracy and enjoyment — so feet are worth representing even when they are inferred rather than tracked. Mitsuda and Kimura (2026) [6] demonstrate a comfortable in-place VR climber that uses the **head as a centre-of-mass (CoM) proxy** and triggers a fall when the head leans over an unsupported foot, providing a validated, lightweight balance model (n=24). Together they suggest that a credible balance-and-footwork experience does not require expensive tracking or full-body inverse kinematics — which, in any case, fail in climbing poses (the documented worst case for Meta Generative Legs and similar IK solvers).

The hard constraint is hardware. A stock Quest tracks only the **head and two controllers**; it cannot see the player's feet. Real footwork and one-foot balance are therefore out of scope, and cosmetic IK legs would slide and float in exactly the poses climbing produces. Our design responds by making feet an **abstracted state** rather than a rendered limb: 1–2 virtual feet auto-snap to the nearest foot-eligible holds in a stance zone below the body and stay glued until climbed past, widening the balance support base without pretending to track real legs. Balance itself is a graded meter — a 1-D lateral support-interval (simplified barn-door) test of the HMD-as-CoM against the lateral span of the active contacts — rather than a binary fall, with hysteresis and a grace time to keep head jitter from causing spurious slips, and with clear counterplay (place a foot, or add an opposite-side hold) so that staying on the wall is a skill rather than a dice roll.

The result is **Summit VR**: counter-motion climbing as the safe, proven baseline, with balance and footwork added as *additive, optional* layers that never put the hands-only experience at risk. Routes are built procedurally from primitives (wall, colour-coded holds, summit) and deliberately include a forced same-side stretch that cannot be climbed by reaching alone, making the new layer load-bearing rather than decorative. We are honest about status: the system is fully implemented, but it has **not yet been playtested**, so no results exist. We therefore present our evaluation as a *study design* — planned metrics and protocol (n≈5–8 classmates), instrumenting time-to-summit, fall count split by cause (let-go vs balance-slip vs stamina), grab success rate, balance margin over time, and on-device frame rate, alongside NASA-TLX, SSQ, and realism/preference Likert items. Our target finding, to be tested rather than claimed, is that the balance-and-footwork layer raises perceived realism and challenge *without* raising simulator sickness — a combination no shipped commercial VR climber currently offers.

### Contributions

- **A balance-and-footwork layer for 3-point VR climbing.** An HMD-as-CoM lateral support-span balance model plus auto-snapping abstracted "virtual feet", running on stock Quest head+controller tracking with no foot trackers, IK legs, or generative legs — adapting the head-as-CoM model of Mitsuda and Kimura (2026) [6] and the feet-matter finding of Kosmalla et al. (2020) [5] to commodity hardware.
- **A graded, comfort-aware fall mechanic.** A drain/regen balance meter with hysteresis and a grace time that tolerates head jitter, exposes explicit counterplay (foot placement, opposite-side holds), and telegraphs slips, so the new failure mode adds challenge without introducing involuntary camera motion or sickness.
- **A full, runnable Unity vertical slice.** Counter-motion grab-and-pull locomotion, role- and type-tagged colour-coded holds, a procedural route builder whose routes force a same-side sequence that makes footwork provably load-bearing, and supporting systems (stamina, checkpoints, summit trigger, HUD, haptics) — all from primitives, requiring no art assets.
- **A pre-registered-style evaluation design.** A playtest protocol and planned metric set (time-to-summit, cause-split falls, grab success, balance margin, frame rate, NASA-TLX, SSQ, realism/preference) and a falsifiable hypothesis — realism/challenge up, sickness flat — clearly marked as **planned**, not measured.

## 2. Related Work

Our system sits at the intersection of four bodies of work: VR climbing locomotion and the commercial titles that popularised it; the question of how to represent feet and the lower body when no foot tracking is available; balance and centre-of-mass (CoM) modelling drawn from both games and humanoid robotics; and the broader literature on embodiment and exertion in VR. We review each in turn and, at the end of each subsection, position *Summit VR* relative to it.

### 2.1 VR climbing locomotion and commercial titles

The dominant locomotion technique for VR climbing is *counter-motion grab-and-pull*: while a hand grips a hold, the player's viewpoint (the XR Origin / camera rig) is translated opposite to the hand's tracked motion, so the gripped point appears fixed in the world and the body is pulled toward it. This is the principle behind Unity's XR Interaction Toolkit Climb Provider [11], and it underlies essentially every arm-based climbing or traversal game. Crytek's *The Climb* and *The Climb 2* [3] are the canonical hands-only examples: a polished, vertigo-inducing experience in which the player grabs ledges and pulls, but the avatar is effectively legless and there is no balance or body-position model — falling is triggered only by letting go or by stamina ("grip") depletion. The same arm-driven core appears in *Gorilla Tag* [1] (locomotion by pushing off surfaces with the arms, no legs), *To The Top* (arm-swing propulsion), and *Climbey* (grab-and-pull, with optional Vive foot trackers used only for floor calibration rather than for any balance mechanic). *STRIDE* takes a step toward whole-body input by reading the player's real torso lean and crouch, but does so for parkour movement rather than for a climbing-specific support-base model.

A parallel research thread instruments a *real* climbing wall and registers it to the virtual environment, so that physical holds provide genuine haptics and proprioception. *InfinityWall* (Kim et al. 2022) [4] couples a rotating rock-climbing treadmill to VR to allow unbounded vertical locomotion, and the broader DFKI line of work (see §2.2) places climbers on registered physical walls. These systems achieve high fidelity at the cost of dedicated hardware and space. By contrast, our target platform is a stock Meta Quest with only the head-mounted display and two controllers — no instrumented wall, no foot trackers — which is the same minimal-hardware regime as *The Climb* and *Gorilla Tag*.

*Positioning.* Our `ClimbController` reproduces the standard most-recent-hand-drives counter-motion locomotion used by the titles above and by the XRI Climb Provider, so that our contribution is *additive* rather than a competing locomotion scheme. Where we depart from every shipped commercial VR climber is in layering an explicit balance-and-footwork model on top of that locomotion (§2.3), turning lean and stance into load-bearing mechanics rather than cosmetic flourish. The recently announced *New Heights: Realistic Climbing and Bouldering* (2026) [2] advertises balance, body position, and feet as core mechanics, but it is a flatscreen title and therefore does not face the VR-specific constraints (no foot tracking, head-as-only-torso-proxy, simulator sickness) that motivate our design.

### 2.2 Footwork, feet, and lower-body representation without trackers

A central, and somewhat counter-intuitive, finding motivates our footwork layer. Kosmalla et al. (2020) [5], in a within-subjects study on a registered physical climbing wall, manipulated whether the climber's virtual hands and/or feet were rendered and found that **including the feet mattered more than including the hands** for perceived hand- and foot-movement accuracy and for climbing enjoyment. In real climbing, footwork and weight transfer are at least as important as hand placement, so a VR climber that ignores the lower body discards much of the activity's character. This directly justifies making feet a first-class element of our design.

The difficulty on consumer hardware is that the lower body is *unobserved*: a Quest tracks only the head and two hands. Inverse-kinematics approaches that infer leg pose from these three points (e.g., Unity's Animation Rigging TwoBoneIK [10]) degrade badly in climbing postures, where legs are splayed, high-stepping, or flagged far from any plausible neutral pose; Meta's Generative Legs similarly target upright locomotion and fail for the extreme stances of bouldering. Mitsuda and Kimura (2026) [6] sidestep pose estimation entirely in their "climb in place with four limbs" system: feet are placed by a downward gesture and released by lifting, are treated as discrete *contact state* rather than rendered limbs, and a feet-only hang is limited by a roughly six-second timeout. Their fall check is itself a body-position model — a fall is triggered when the head-as-CoM leans out over an unsupported foot — which we adopt and adapt in §2.3.

*Positioning.* Following Kosmalla et al. (2020) [5] for the *why* and Mitsuda and Kimura (2026) [6] for the *how*, our `FootPlacementSystem` represents one or two virtual feet as abstract **state, not IK legs**: each foot auto-snaps to the nearest foot- or either-role hold within a stance zone below the body and stays "glued" until the climber moves past it. We deliberately avoid rendering articulated legs, both because the IK is unreliable in climbing poses and because the feet's mechanical purpose in our system is to *widen the lateral support base* used by the balance check, not to be looked at. Our `RouteBuilder` then makes this load-bearing by inserting a forced same-side stretch that cannot be completed without placing a foot or flagging, so footwork is required rather than optional.

### 2.3 Balance and centre-of-mass models (games and robotics)

The biomechanical phenomenon we model is the climbing *barn-door effect*: when a climber's holds and CoM are poorly aligned laterally, the body swings open like a door on a hinge, and the standard correction is to *flag* — extend a leg to one side to shift the CoM back over the support base. This is fundamentally a statics question about whether the CoM projects within a region of support, which connects climbing directly to the balance literature in legged robotics.

That robotics lineage provides the formal tools. The **support polygon / support base** is the convex hull of the contact points; classical static balance requires the CoM ground projection to lie inside it. Vukobratović's **Zero-Moment Point (ZMP)** (Vukobratović and Borovac 2004) [12] generalises this to dynamic gait by tracking the point where ground-reaction moments vanish, requiring it to stay within the support polygon. Pratt et al. (2006) [8] introduce the **capture point** — the point on the ground where a biped would have to step to come to a complete stop — and show that when the capture point lies inside the base of support the robot can recover in place, whereas if it falls outside, a step (a change of the support base) is required. The decision logic "is my CoM-related point inside the current support region, and if not, where must I place a contact to restore it?" maps almost exactly onto a climber deciding whether to flag.

Within games, by contrast, this kind of explicit support-base reasoning is rare: as noted in §2.1, *The Climb* and *Gorilla Tag* have no balance model at all, and even systems that read real torso lean (*STRIDE*) use it as locomotion input rather than as a fall condition. Mitsuda and Kimura (2026) [6] are the closest precedent — their lean-over-unsupported-foot fall check is a one-dimensional special case of the support-polygon idea applied to a VR climber.

*Positioning.* Our `BalanceSystem` is a deliberately *lightweight, one-dimensional* realisation of this lineage suited to consumer VR. We treat the HMD position as a CoM proxy (as in Mitsuda and Kimura 2026 [6]), define the set of contacts as the currently gripped hand-holds plus the auto-placed virtual feet, and reduce the support polygon to a **lateral support interval** — the span of the contacts' horizontal positions (a simplified barn-door / support-interval model). Stability is the test of whether the CoM's lateral coordinate lies within that span; leaning outside it drains a graded balance meter, and exhaustion of the meter triggers a "peel-off" fall. To keep this robust against head jitter and the noisiness of a single tracked point we add **hysteresis and a grace time** before draining begins, conceptually analogous to the recovery margin that the capture point affords a robot before a step becomes mandatory. Crucially, balance is only evaluated while at least one hand grips, so the system never penalises the player during ballistic moves or falls. This is, to our knowledge, the first consumer-VR climbing game to make a support-base balance check a primary fall cause.

### 2.4 Embodiment and exertion

Two further literatures inform our evaluation rather than our mechanics. First, **embodiment**: the sense of owning and controlling a virtual body is a well-studied construct in VR, traceable to the rubber-hand illusion and extended to full-body avatars. The systematic review and meta-analysis of body-ownership illusions by Mottelson et al. (2023) [7] reports that virtual embodiment reliably produces a modest sense of ownership and a somewhat stronger sense of *agency* (control), and — importantly for us — that congruent visuo-motor behaviour matters more than photorealistic limbs. This supports our choice to invest in *behaviourally correct* feet (correctly placed, mechanically consequential contact state) rather than in rendered IK legs that would look plausible but behave wrongly in climbing poses; an avatar limb that moves incorrectly can actively damage the ownership and agency it is meant to create.

Second, **exertion**. VR climbing is inherently an exergame, and a consistent finding in the VR exergaming literature is that immersion and embodiment raise motivation, presence, and enjoyment largely *independently* of perceived physical exertion (Born et al. 2019) [13], and that players frequently *under-report* exertion relative to physiological measures during VR exercise (Stewart et al. 2022) [9]. Two consequences follow for our study design. (i) We cannot assume that adding a balance/footwork layer changes felt effort simply because it changes physical movement, so perceived effort must be measured directly — we plan to use NASA-TLX. (ii) Because immersion and enjoyment can move independently of exertion, our planned realism/challenge and preference measures should be interpreted alongside, not collapsed into, effort. These exergaming results, together with the embodiment review, frame the central planned hypothesis of this project: that the balance-and-footwork layer raises perceived realism and challenge (per Kosmalla et al. 2020 [5] on feet, via the Mitsuda and Kimura 2026 [6] model) **without** a corresponding increase in simulator sickness (SSQ) — a combination that, as §2.1–§2.3 establish, no shipped commercial VR climber currently offers. *(All results are PLANNED; the system has not yet been playtested.)*

## 3. Method

This section sets out the design rationale and the formal model behind *Summit VR*. We first state the constraints that shape every decision (§3.1), then describe the locomotion baseline we build on (§3.2), the balance model that is our headline contribution (§3.3), the footwork abstraction that feeds it (§3.4), and finally how holds and routes are authored so that footwork and balance become load-bearing rather than decorative (§3.5).

### 3.1 Design constraints and rationale

Our target platform is a standalone Quest headset with **three tracked points only**: the HMD and two controllers. There is no foot tracking, no torso tracking, and no reliable lower-body pose. This is the dominant consumer VR configuration, and it is exactly the configuration under which Meta's Generative Legs and full-body IK are known to degrade, because climbing poses (high steps, drop-knees, flags) lie far outside the locomotion distribution those systems are trained or tuned for. We therefore adopt a deliberate principle: **abstract what we cannot sense, and only simulate what changes play.** Rather than synthesise plausible-looking legs, we model the *functional consequence* of footwork — a wider base of support — and leave the visual leg out entirely.

This is consistent with the empirical literature. Kosmalla et al. (2020) [5] found that virtual *feet* mattered more than virtual *hands* for perceived movement accuracy and enjoyment in VR climbing, which motivates treating feet as first-class even when they cannot be tracked. Mitsuda and Kimura (2026) [6] demonstrate that a head-as-centre-of-mass proxy, combined with a lean-over-unsupported-foot fall check, is sufficient to make in-place four-limb climbing feel grounded — without any physics solver. We adopt both findings directly: feet are first-class *state*, and the head is our centre-of-mass proxy.

A second principle is **additivity**: the balance and footwork layers are optional components that sit on top of a working hands-only baseline. If `BalanceSystem` or `FootPlacementSystem` is absent, the game degrades cleanly to standard counter-motion climbing (cf. *The Climb* [3], *Gorilla Tag* [1]). This keeps the contribution isolated and testable, and it lets the planned study (§5) compare *with-layer* against *without-layer* on the same locomotion core.

A third principle is that **all thresholds are tuning constants, not derived physics.** We borrow the *vocabulary* of legged-robot balance — support polygon, zero-moment point (Vukobratović and Borovac 2004) [12], capture point (Pratt et al. 2006) [8] — but we do not compute dynamics. Our margins are tuned by feel against comfort and challenge, and we mark them as such throughout.

### 3.2 Counter-motion grab-and-pull locomotion

Locomotion uses the now-standard *counter-motion* (grab-and-pull) scheme, identical in principle to the Unity XR Interaction Toolkit Climb Provider [11] and to *The Climb* / *Gorilla Tag*. While a hand grips a hold, the gripped world point is treated as fixed; as the tracked controller moves, the XR Origin (player rig) is translated by the *opposite* of the hand's per-frame displacement, so the body is pulled along the wall while the grip stays anchored in the world. Concretely, on each frame the controller moves the rig by

```
delta      = anchorWorld − handPositionNow      // residual the hand drifted from its anchor
rig.Move(delta)                                 // pull the body to cancel that drift
anchorWorld = handPositionNow                   // re-pin (the hand is a child of the rig)
```

Two design rules govern multi-hand use:

- **Most-recent-hand-drives.** When a second hand grabs, it becomes the active driver; when the active hand releases, control passes back to the other hand if it is still gripping, otherwise to nobody. This avoids averaging two hands (which feels mushy) and matches how climbers actually shift weight onto a fresh hold before releasing the old one.
- **Gravity and fall/respawn.** With *no* contact at all (no hand gripping, no foot planted), gravity accumulates through a `CharacterController` and the body falls; dropping below a reset plane respawns at the last checkpoint. Crucially, when the climber is supported by **feet only** (a hand released but a virtual foot still planted), the body *hangs in place* — neither climbing nor falling — which is what allows a foot stance to be a genuine resting/standing state rather than a brief animation.

This baseline is intentionally conventional; it is the substrate onto which the balance layer is added.

### 3.3 The balance model

#### 3.3.1 Head as centre-of-mass proxy

We approximate the climber's centre of mass (CoM) by the HMD position. This is cheap, always available on a 3-point rig, and well-motivated: the head sits high on the body and its lateral sway tracks gross upper-body lean, which is the failure mode bouldering punishes most — the **barn-door**, where a climber on same-side holds with no opposing foot swings open like a door on its hinges. We do not claim the head *is* the CoM; we claim its **lateral** coordinate is a usable proxy for *whether the climber is leaning outside their base of support*, which is all the model needs.

#### 3.3.2 Lateral support interval (simplified barn-door)

Rather than a 2-D support polygon, we collapse the problem to **one dimension: the lateral (left–right) axis of the wall**, given by the rig's local right vector. The reasons are (a) the dominant climbing-balance failure is lateral barn-dooring, (b) a 1-D support *interval* is trivial to compute and to display as a meter, and (c) with only 1–2 hand contacts plus 1–2 foot contacts, a full polygon would be degenerate or near-collinear much of the time anyway.

The contact set on a given frame is

```
C = { gripped hand-hold points } ∪ { planted virtual-foot points }
```

We project each contact and the CoM onto the lateral axis. Let `xᵢ` be the lateral coordinate of contact *i* measured **relative to the head** (so the CoM's own lateral coordinate is `0` by construction). The bare support interval is `[min xᵢ, max xᵢ]`; we widen it by a tuning slack `m = supportMargin` (default **0.06 m**) on each side, modelling the small amount of lean a real stance tolerates before it becomes a fall:

```
low  = min(xᵢ) − m
high = max(xᵢ) + m
```

#### 3.3.3 Signed stability margin

We define a **signed stability** `s ∈ [−1, +1]` that is positive when the CoM is comfortably inside the support interval and negative when it has leaned outside:

- **Inside the interval** (`low ≤ 0 ≤ high`). Let the distance to the nearer edge be `margin = min(0 − low, high − 0)` and let `halfSpan = (high − low)/2`. Then

  ```
  s = clamp01( margin / halfSpan )            ∈ (0, 1]
  ```

  so `s → 1` when the CoM sits dead-centre over a wide stance, and `s → 0` as it nears an edge. This is what makes a planted foot *feel* stabilising: adding a foot contact widens `[low, high]`, which raises `halfSpan` and pushes `s` up.

- **Outside the interval.** Let `overshoot` be how far the CoM has passed the nearer edge, and normalise it by `maxOvershoot` (default **0.30 m**), the lean at which we call the climber fully off-balance:

  ```
  s = − clamp01( overshoot / maxOvershoot )   ∈ [−1, 0)
  ```

The sign of `s` is therefore the *stable/unstable* verdict, and its magnitude is a *graded* severity — not a binary trip. This grading is what drives a smooth meter rather than an instantaneous fall.

#### 3.3.4 Hysteresis, grace, and the balance meter

A single signed margin would be jittery: the HMD signal contains real head bob and tracking noise, and a hard threshold would chatter between stable and unstable many times per second. We therefore integrate `s` into a **balance meter** `B ∈ [0, 1]` with two stabilising devices:

- **Grace time.** Instability must persist for `graceTime` (default **0.35 s**) before it begins to drain the meter. Brief excursions — a glance, a noise spike — are absorbed and never cost balance. This is a debounce in the time domain, the temporal analogue of the spatial `supportMargin`.

- **Asymmetric drain/regen (hysteresis in rate).** Once draining, the meter falls at a rate proportional to severity (`drainPerSecond = 0.6`, scaled by `s`); when stable it refills (`regenPerSecond = 0.7`, scaled by `s`). Because draining requires *both* `s < 0` *and* the grace window to elapse, but regeneration begins immediately on returning inside support, the system has a directional bias toward recovery — you fall off only from sustained, committed over-lean, and you are rewarded quickly for re-centring (e.g. by flagging or placing a foot).

In pseudocode, the per-frame update is:

```
engaged ← (leftHand gripping) OR (rightHand gripping)
if not engaged:                      # off the wall (pre-climb or mid-fall)
    B ← min(1, B + regen·dt);  reset timers;  return    # recover quietly, never churn

s ← ComputeStability(C, head, lateralAxis)
if s < 0:
    unstableTimer += dt
    if unstableTimer ≥ graceTime:    # grace elapsed → commit to draining
        B += s · drain · dt          # s<0 ⇒ B decreases, faster the further out
else:
    unstableTimer ← 0
    B += s · regen · dt              # s>0 ⇒ B refills toward 1
B ← clamp01(B)
if B ≤ 0:  PeelOff()
```

A key gating rule, visible above: **balance is only a threat while at least one hand grips.** Before the route starts and during a fall, the meter recovers silently. This prevents the meter — and the peel-off event — from churning when the player is not actually load-bearing on the wall.

#### 3.3.5 Peel-off

When `B` reaches zero the climber **peels off**: a `PeelOff` event fires, the locomotion layer force-releases both hands and drops all feet, and the ordinary gravity → fall → checkpoint loop takes over. This is the balance layer's only hard outcome; everything before it is graded and recoverable. After a respawn the meter is reset to full so the climber does not immediately re-slip at the checkpoint.

We summarise the balance loop as a small diagram:

```
   HMD (CoM proxy, lateral x = 0)
            │
            ▼
   project contacts onto lateral axis  ──►  [low, high] support interval (+ margin m)
            │
            ▼
   signed margin  s ∈ [−1,+1]
            │
     ┌──────┴───────┐
   s>0             s<0
  refill B      (grace?) → drain B
     └──────┬───────┘
            ▼
        B ≤ 0 ?  ── yes ──►  PeelOff → release all → fall → checkpoint
```

### 3.4 Footwork abstraction

#### 3.4.1 Virtual feet as state

Because the platform cannot track feet, *Summit VR* maintains up to **two virtual feet** as derived state. Each frame we estimate a stance zone a fixed `bodyDrop` (default **1.15 m**) below the HMD, offset left and right by a `stanceHalfWidth` (default **0.22 m**). Each foot searches within a `footReach` radius (default **0.45 m**) for the nearest usable hold (a *Foot* or *Either* hold — never a hand-only hold) and, if one is found, plants on it. A planted foot contributes its hold's grip point to the contact set `C`, which — as §3.3.3 shows — widens the support interval and raises stability. Planted feet are shown only as simple markers on the held hold; there is no leg geometry.

#### 3.4.2 Foot-gluing

Feet are *sticky*. Once planted, a foot **keeps its current hold** as long as that hold remains within `footReach` of the stance zone, even if some other hold becomes momentarily nearer. Only when the held hold leaves reach — typically because the climber has moved up past it — does the foot re-snap to a new, lower hold. This **foot-gluing** mirrors how real climbers commit weight to a foothold and leave it until the move is done, and it prevents the support base from flickering between holds (which would make `B` noisy). It is the spatial counterpart to the temporal grace time in the balance meter: both exist to keep the support estimate stable under noise.

#### 3.4.3 Why not IK or ML legs

We deliberately avoid both inverse-kinematics legs (e.g. Animation Rigging TwoBoneIK [10]) and learned full-body legs (Meta Generative Legs) for the *mechanic*. Three reasons:

1. **Validity.** With only head + hands tracked, the true lower-body pose is unobservable; an IK/ML leg would be a *guess*, and in climbing poses (high steps, flags, drop-knees) those guesses are visibly and confidently wrong — precisely the regime where Generative Legs is reported to break.
2. **Relevance.** What changes gameplay is the *support base*, not the leg mesh. Modelling feet as contact points captures the entire mechanical consequence (a wider `[low, high]`) at a fraction of the cost and with none of the failure modes.
3. **Honesty of feedback.** Kosmalla et al. (2020) [5] show feet improve perceived accuracy — but a *wrong* leg can break presence. An abstract foot marker on the actual foothold makes a true, checkable claim ("your weight is here"); a synthesised leg risks an uncanny, false one.

A purely **cosmetic** TwoBoneIK leg remains compatible as an optional visual layer, but it is explicitly *not* part of the balance computation and carries no gameplay weight.

### 3.5 Hold roles and route design

#### 3.5.1 Hold roles and types

Every hold carries two pieces of metadata that drive the model. A **role** restricts who may use it — `Hand`, `Foot`, or `Either` — so that hand-grab queries skip foot-only holds and the foot solver skips hand-only holds; this is what lets the route author *force* a foot to be used at a particular place. A **type** gives behaviour: `Normal`, `Finish` (completes the route), `Fragile` (breaks after a short hold time, forcing quick, balanced moves), and `Rest` (no stamina cost). A colour legend makes the metadata legible at a glance — yellow = hand, orange = foot, purple = either, green = finish, red = fragile, blue = rest — so a player reads the route the way a real boulderer reads tape.

#### 3.5.2 Procedural route building

To avoid any art dependency, `RouteBuilder` constructs a wall, colour-coded holds, a finish hold, and a summit trigger entirely from primitives, driven by an optional `RouteDefinition` or a baked default. This makes the system runnable in an empty scene and makes routes cheap to author and to vary for the study.

#### 3.5.3 The forced same-side stretch

The crucial design move is that the default route is **not balanceable hands-only.** Its hand holds zig-zag upward but include a deliberate **same-side (right) stretch** — two consecutive hand holds both on the right of centre. Reaching the upper hold on hand contacts alone places the CoM outside the (now right-skewed) support interval: `0 > high`, `s < 0`, and after the grace window the meter drains and the climber barn-doors off. The only way through is to widen the base on the left — by stepping a virtual foot onto a left foot-hold (threaded below the hands for exactly this purpose) or by flagging — which extends `low` leftward, brings `0` back inside `[low, high]`, and restores `s > 0`.

This single piece of route geometry is what converts footwork and balance from decoration into a **load-bearing** mechanic: the route cannot be completed without using them, and that is the behaviour our planned evaluation is designed to detect.

## 4. Implementation

### 4.1 Engine and package stack

Summit VR is built in **Unity 2022.3.40f1 LTS** with the **Universal Render Pipeline (URP 14.0.11)**, chosen because URP is the recommended mobile-class pipeline for standalone Quest hardware (single-pass instanced rendering, low draw-call budget). Tracking and input run on the modern XR stack: **OpenXR 1.10** as the runtime backend, **XR Interaction Toolkit (XRI) 2.5.4** for the rig and interactor scaffolding, and the **Input System 1.7.0** for all action bindings. `com.unity.xr.hands 1.4.0` and `com.unity.xr.management 4.4.0` are present for the rig and loader, and TextMeshPro 3.0.6 / uGUI drive the world-space HUD. The target device is **Meta Quest (head + two controllers only, no foot tracking)**, which is the constraint the entire balance-and-footwork design is written around.

A deliberate decision in the code is that climbing input is read through an `InputActionProperty` (the grip / *Select Value* action) rather than through a specific XRI interactor class. This decouples our logic from the exact XRI version: a controller's grip value is read each frame and thresholded, so the same scripts run unchanged across XRI point releases and in the simulator. The only XRI-version-independent extras we use directly are the legacy `UnityEngine.XR.InputDevices` haptics API (for the rumble pulse on grab) and the OpenXR pose data exposed on the rig's camera and controller transforms.

### 4.2 Script architecture overview

The codebase is fourteen C# scripts split into three assemblies-by-namespace: `VRClimb.Climbing` (the locomotion and balance core), `VRClimb.Gameplay` (route, flow, and challenge layers), and `VRClimb.UI` / `VRClimb.Util` (HUD and haptics). The design principle throughout is that the **hands-only climbing baseline must always work on its own**, and balance and feet are *additive, optional* layers — every reference to `BalanceSystem` and `FootPlacementSystem` is null-checked, so the game degrades to a standard counter-motion VR climber (in the lineage of Unity's XRI Climb Provider [11], *The Climb* [3], and *Gorilla Tag* [1]) if those components are absent.

Components communicate by two mechanisms, which keeps coupling low:

- **C# events** for discrete state changes. `ClimbingHand` raises `Grabbed`/`Released`; `ClimbHold` raises `Grabbed`/`Released`; `BalanceSystem` raises `PeelOff`; `GameManager` raises `StateChanged`, `PlayerFell`, and `Finished`. Subscribers (the `ClimbController`, the `GameHUD`) attach in `OnEnable` and detach in `OnDisable`.
- **A shared "contacts" abstraction** for the continuous balance computation. Every frame, the set of support points — gripped hand-holds plus planted virtual feet — is collected into a single `List<Vector3>` of world positions that `BalanceSystem` consumes. This is the seam that lets footwork feed balance without either system knowing the other's internals.

```
                        InputActionProperty (grip)
                                  │
                          ClimbingHand ×2  ──Grabbed/Released──► ClimbController ──► CharacterController.Move
                                  │                                   ▲  │
                            ClimbHold (role/type)                     │  └─ PeelOff ◄── BalanceSystem
                                  │                                   │                     ▲
                          (GripPoint contacts) ───────────────────────┼─── contacts ───────┤
                                                                       │                     │
                        FootPlacementSystem ──(virtual-foot contacts)──┘─────────────────────┘
                                  │
   GameManager ◄── StartClimb / OnPlayerFell / OnSummitReached ──► StateChanged / Finished ──► GameHUD
        ▲                                                                                         ▲
   SummitTrigger / Checkpoint / StaminaSystem                                        StaminaSystem, BalanceSystem (bars)
```

### 4.3 Locomotion core: `ClimbController` and `ClimbingHand`

**`ClimbingHand`** (one per controller) is the input and reach layer. Each frame it reads the grip action's float value and compares it to `gripThreshold` (0.6). On a rising edge it runs a non-allocating `Physics.OverlapSphereNonAlloc` of radius `grabRadius` (0.12 m) on the *Hold* layer, picks the nearest valid `ClimbHold` (rejecting `Foot`-role holds and broken holds), latches it as `CurrentHold`, increments the hold's gripper count, fires `Grabbed`, and pulses haptics. On the falling edge — or if the held hold breaks or is disabled mid-grip — it releases. It exposes `IsGripping`, `CurrentHold`, and `HandPosition` (the tracked controller transform), which is all the controller needs. A static eight-slot `Collider[]` buffer is reused for the overlap query so grabbing allocates no garbage.

**`ClimbController`** drives the **XR Origin** through a `CharacterController` and implements the counter-motion principle. The rule set is:

- **Most-recent-hand-drives.** On `Grabbed`, the grabbing hand becomes `_activeHand` and its current world position is stored as `_anchorWorld`. Each frame while that hand grips, the rig is moved by `delta = _anchorWorld − handNow`, i.e. *opposite* to the hand's tracked motion, so the gripped point stays fixed in the world and the body is pulled along the wall. Because the hand is a child of the rig, it moves with the rig, so the anchor is re-pinned to the hand's new position each frame. On release, control hands back to the other hand if it is still gripping, otherwise `_activeHand` becomes null.
- **Gravity and falling.** With *no* contact at all (no hand gripping and zero planted feet), a simple Euler gravity step (`gravity = −9.81`) is integrated through the `CharacterController`. Dropping below `fallResetY` (−10 m) triggers `Respawn`. Catching any hold zeroes accumulated velocity immediately.
- **Feet-only hang.** If no hand grips but feet are planted, the rig neither climbs nor falls — it simply hangs in place. This is the hook for the feet-only-support case discussed in the balance literature (Mitsuda and Kimura 2026 [6]).
- **Peel-off.** `ClimbController` subscribes to `BalanceSystem.PeelOff`; when balance is exhausted it force-releases both hands and drops all feet, so the normal gravity/respawn loop takes over and the climber falls to the last checkpoint.

`Respawn` disables the `CharacterController` to teleport the rig reliably to the spawn/checkpoint point, restores balance, and notifies the `GameManager`.

### 4.4 The headline mechanic: `BalanceSystem`

`BalanceSystem` is the project's core contribution and is **explicitly not a physics simulation**; it is a one-dimensional support-interval test — a simplified "barn-door" model. The HMD (main camera) position is used as a cheap **centre-of-mass (CoM) proxy**, following the head-as-CoM, lean-over-unsupported-foot fall check of Mitsuda and Kimura (2026) [6].

Each frame, `ComputeStability` collects the contact points (gripped hand-holds' `GripPoint` plus the feet's contacts) and projects each onto the wall's **lateral axis** (the rig's local right vector), measured relative to the head. By construction the CoM's lateral coordinate is 0, so stability reduces to a scalar interval test: take the min and max lateral coordinates of the contacts, widen the interval by `supportMargin` (0.06 m) on each side, and check whether 0 lies inside. The function returns a **signed stability** in [−1, +1]: positive (scaled by how centred you are within the span) when supported, and negative (scaled by overshoot past the edge, clamped at `maxOvershoot` = 0.30 m) when leaning out.

That signed value feeds a **graded balance meter** with two robustness features required by raw HMD jitter:

- **Hysteresis via a grace timer.** Instability must persist for `graceTime` (0.35 s) before the meter begins to drain, absorbing transient head wobble. While unstable past the grace window, `Balance` drains at up to `drainPerSecond` (0.6 /s) scaled by overshoot; while supported it regenerates at up to `regenPerSecond` (0.7 /s) scaled by how centred you are.
- **Engagement gating.** Balance is *only* a threat while at least one hand grips. Before the climb and during a fall it quietly recovers, so the meter and `PeelOff` never churn off-wall.

When `Balance` reaches 0, `PeelOff` fires once; the meter is then reset to 0.4 to prevent it re-firing every frame during the ensuing fall. This graded, hysteretic design is what lets us claim the balance layer can raise perceived challenge without inducing the abrupt, sickness-prone snaps that a hard fall threshold would produce.

### 4.5 Abstracted footwork: `FootPlacementSystem`

Because Quest provides no foot tracking, feet are **abstracted state derived from holds**, not IK legs and not Meta Generative Legs (which fail in climbing poses). This follows the evidence that virtual feet matter more than hands for perceived movement accuracy and enjoyment (Kosmalla et al. 2020 [5]), while sidestepping the failure modes of full-body inference.

Two "virtual feet" are solved each frame. A stance base is estimated `bodyDrop` (1.15 m) below the HMD, offset left/right by `stanceHalfWidth` (0.22 m) along the lateral axis. Each foot runs an overlap query of radius `footReach` (0.45 m) on the Hold layer for the nearest `Foot`- or `Either`-role hold. The key behaviour is **foot-gluing**: a planted foot keeps its current hold as long as that hold stays within `footReach` of the rest position, and only re-snaps to a lower hold once the body climbs past it. Planted feet optionally drive simple visual markers, and — critically — their `GripPoint`s are appended to the balance contacts via `CollectContacts`, so placing a foot literally widens the lateral support span and re-centres the climber. `PlantedCount` is what tells `ClimbController` that a feet-only hang is valid, and `DropAll` clears both feet on a peel-off or respawn.

### 4.6 Holds: `ClimbHold` and roles/types

`ClimbHold` tags a GameObject as climbable and carries the gameplay metadata. **Roles** {`Hand`, `Foot`, `Either`} gate who may use a hold (the hand grab query rejects `Foot` holds; the foot solver rejects `Hand` holds). **Types** {`Normal`, `Finish`, `Fragile`, `Rest`} drive behaviour: `Fragile` holds track cumulative held-time via a gripper count and `Break` (deactivate) after `breakAfterSeconds` (1.5 s); `Rest` holds suppress stamina drain; `Finish` marks the topping-out hold. The colour legend (yellow = hand, orange = foot, purple = either, green = finish, red = fragile, blue = rest) is rendered both in editor gizmos and by the route builder's material assignment. `GripPoint` (an optional anchor transform, else the object's position) is the single world point every other system anchors and measures against.

### 4.7 Procedural route generation: `RouteBuilder` and `RouteDefinition`

So that v1 needs **no art assets**, `RouteBuilder` constructs an entire playable boulder from Unity primitives on `Awake`. It spawns a cube **wall** (default 3.5 × 6 m, front face at local z = 0), a list of **sphere holds**, and a **summit trigger** box spanning the top. Holds are placed on a named *Hold* layer (with a warning if the layer is missing), have their colliders set to trigger (so they register in overlap queries but never block the `CharacterController`), and receive a `ClimbHold` plus a URP-*Lit* material tinted per the colour legend. Materials are cached one-per-colour and reused across rebuilds to avoid leaking a material per hold.

Routes come from a serializable **`RouteDefinition`** ScriptableObject (wall size plus a list of `HoldSpec` records: local position, role, type, size), so routes can be hand-authored as assets via the *VRClimb → Route Definition* menu. If no definition is assigned, `RouteBuilder.DefaultRoute` bakes a short beginner route. That default is designed to make footwork **load-bearing rather than decorative**: the hand holds zig-zag upward but include a deliberate **same-side (right) stretch** (`Hand(0.6, 3.2)` → `Hand(0.7, 3.7)`) where, with both hands on the right, the CoM falls outside the hand-only support span — the climber must place a left/lower foot (or flag) to restore balance and pass. Foot holds are threaded below the hand line throughout, and a green `Either`/`Finish` hold sits near the top.

### 4.8 Game flow and challenge layers

**`GameManager`** is a lightweight singleton holding the state machine ({`Ready`, `Climbing`, `Summit`, `Fell`}), the run timer, and the fall counter. The run and timer start on the *first* hand grab (`ClimbController` calls `StartClimb` while still `Ready`). It guards against edge cases — a stray fall after topping out cannot undo a win — and raises `StateChanged`/`PlayerFell`/`Finished` for the UI and (future) audio layers.

**`SummitTrigger`** and **`Checkpoint`** both detect the player by the presence of a `ClimbController` in the entering collider's parents — no manual "Player" tag is needed. The summit trigger calls `OnSummitReached`; checkpoints update the controller's respawn point so a fall returns the climber there rather than to the bottom.

**`StaminaSystem`** is an optional challenge layer: gripping a hold drains stamina at the hold's `staminaCostPerSecond`, releasing or holding a `Rest` hold regenerates it (`regenPerSecond` = 20 /s, `maxStamina` = 100), and exhaustion force-releases both hands. Removing the component yields a relaxed mode. This is an independent fall *cause* from balance-slip and let-go, which is exactly the three-way split (let-go vs balance-slip vs stamina) the planned evaluation will instrument.

**`GameHUD`** is a world-space canvas that subscribes to `GameManager` events and polls `StaminaSystem.Normalized` and `BalanceSystem.Normalized` each frame to drive a timer label, a status label, and two fill bars. The **balance bar turns red while `IsSlipping`** — direct visual feedback of the headline mechanic — and a transient "you fell" message clears itself after `fellMessageSeconds`. **`HapticFeedback`** is a small static helper firing a controller rumble on grab. **`PlayerClimberSetup`** is an editor convenience that wires the whole climber (CharacterController, ClimbController, two ClimbingHands, FootPlacementSystem, BalanceSystem) from one context-menu click, leaving only the per-hand grip action binding and the Hold LayerMask for the inspector.

### 4.9 Key tuning constants

All thresholds are empirical tuning constants set by feel, not derived physics; they are exposed in the inspector so they can be swept during the planned playtest.

| System | Constant | Value | Role |
|---|---|---|---|
| ClimbingHand | `gripThreshold` | 0.6 | Grip value above which a grab is attempted |
| ClimbingHand | `grabRadius` | 0.12 m | Reach radius searched for a hold |
| ClimbController | `gravity` | −9.81 m/s² | Free-fall acceleration |
| ClimbController | `fallResetY` | −10 m | World Y below which the climber respawns |
| BalanceSystem | `supportMargin` | 0.06 m | Lateral slack outside the contact span before counting as unstable |
| BalanceSystem | `maxOvershoot` | 0.30 m | Lateral overshoot mapped to fully unstable (−1) |
| BalanceSystem | `drainPerSecond` | 0.6 /s | Max balance drain rate when leaning out |
| BalanceSystem | `regenPerSecond` | 0.7 /s | Max balance recovery rate when supported |
| BalanceSystem | `graceTime` | 0.35 s | Instability tolerated before draining (anti-jitter) |
| FootPlacementSystem | `bodyDrop` | 1.15 m | Head-to-feet vertical estimate |
| FootPlacementSystem | `stanceHalfWidth` | 0.22 m | Lateral offset of each foot from centre |
| FootPlacementSystem | `footReach` | 0.45 m | Radius a foot searches for / keeps a hold |
| ClimbHold | `staminaCostPerSecond` | 5 /s | Default stamina drain while gripped |
| ClimbHold | `breakAfterSeconds` | 1.5 s | Fragile-hold lifetime under load |
| StaminaSystem | `maxStamina` / `regenPerSecond` | 100 / 20 /s | Stamina capacity and recovery |

### 4.10 Headless testing in the XR Device Simulator

Because input is read through Input System actions and the rig is a standard XR Origin, the entire game **runs in Unity's XR Device Simulator without a physical headset**. Dropping the simulator prefab into the scene and pressing Play lets a developer move the simulated head and controllers with the keyboard/mouse and trigger the grip action to exercise the full climbing, balance, footwork, and fall/respawn loops on a flatscreen. The same project also runs on hardware over Quest Link or as a standalone Android build, but simulator support means the locomotion and balance logic can be iterated and demonstrated without a device — useful for a five-person course group sharing one headset.

*Note: this section describes implemented systems only. No results are reported here; per-constant tuning sweeps and the fall-cause instrumentation referenced in §4.8 are PLANNED for the evaluation (§5).*

## 5. Evaluation

> **Status: PLANNED — no results have been collected yet.** This section specifies the evaluation *protocol* we intend to run, the hypotheses it tests, and the exact metrics and instruments. Every results table and figure below is a labelled placeholder ("to be collected"). No number in this section should be read as a measured outcome; the cells are intentionally empty. We describe the design now so that (a) the instrumentation can be built into the build before playtesting, and (b) the analysis is pre-registered rather than chosen after seeing the data.

### 5.1 Goals and hypotheses

The central claim of *Summit VR* is that a **lightweight balance-and-footwork layer**, driven only by the HMD-as-centre-of-mass proxy and two abstracted virtual feet (Sections 3–4), can make controller-only Quest climbing feel more like real bouldering — more challenging and more bodily — **without** the comfort cost (nausea, fatigue, frustration) that usually accompanies added VR locomotion complexity. This mirrors the finding of Kosmalla et al. (2020) [5] that virtual *feet* matter more than hands for perceived movement accuracy and enjoyment, and operationalises the head-as-CoM fall check of Mitsuda and Kimura (2026) [6] in a hands-only, foot-tracker-free setting.

We therefore pre-register the following hypotheses. The primary contrast is **within-subject**, between two builds of the same game:

- **Condition A — Baseline ("hands-only"):** counter-motion grab-and-pull locomotion only (the `ClimbController`), with the balance meter and `FootPlacementSystem` disabled. This approximates the design space of shipped commercial VR climbers — *The Climb / The Climb 2* (Crytek) [3], *Gorilla Tag* [1], *To The Top* — which are legless and have no balance model.
- **Condition B — Full ("balance + footwork"):** the same locomotion plus the `BalanceSystem` (graded meter, hysteresis, grace time, peel-off) and the `FootPlacementSystem` (1–2 auto-snapped virtual feet that widen the lateral support span).

Hypotheses:

- **H1 (Realism / immersion).** Condition B is rated higher on perceived climbing realism and on a sense of full-body involvement than Condition A. *(Rationale: Kosmalla et al. 2020 [5].)*
- **H2 (Challenge / engagement).** Condition B is rated as more challenging and at least as enjoyable as Condition A; the forced same-side stretch on the route (which requires a foot/flag to stay balanced) is solvable in B but feels arbitrary or trivial in A.
- **H3 (Comfort — the load-bearing claim).** Condition B does **not** significantly increase simulator sickness (SSQ) or self-reported physical/temporal demand (NASA-TLX) relative to Condition A. The balance layer adds *decision* difficulty, not *vection* or motion conflict, so we predict no comfort penalty. This is the differentiator no shipped commercial VR climber currently offers.
- **H4 (Behavioural signature).** The fall-cause distribution differs between conditions: in A essentially all falls are "let-go" (loss of grip) or stamina; in B a measurable fraction are **balance-slips** (peel-offs), and players exhibit *foot-seeking* behaviour (placing/keeping a virtual foot before committing to a reach) that has no analogue in A.

**Future / out-of-scope contrast (not run for the course report).** A secondary axis we flag for future work is **controller grip vs. Quest hand-tracking** as the grab input. We do not test it now: hand-tracking loses tracking under the body and during fast counter-motion pulls, which would confound the balance manipulation. We note it as planned future work rather than a condition here.

### 5.2 Participants

Target **n ≈ 5–8** classmates from AI3618 (convenience sample; this is a course project, not a powered user study). We will record VR experience (none / casual / regular) and real-world climbing experience (none / gym / outdoor) as covariates, because both plausibly move the realism and challenge ratings. We will **not** make strong inferential claims from this n; the study is powered to surface large within-subject effects and qualitative patterns, not subtle ones. All measures are reported with individual data points overlaid, not means alone, given the sample size.

### 5.3 Design and counterbalancing

Within-subject, two conditions (A, B), **order counterbalanced** across participants (half A→B, half B→A) to control for learning and fatigue. Each participant climbs the **same route** in both conditions so that route difficulty is held constant; the route is the procedurally built wall from `RouteBuilder`, including the forced same-side stretch that makes footwork load-bearing. A short standardised tutorial wall (not analysed) precedes the first condition so participants learn the grab/pull and, in B, the balance meter, before any data is logged.

### 5.4 Objective metrics (logged on-device)

All objective metrics are emitted by lightweight instrumentation already supported by the existing systems (`GameManager.ElapsedTime`, `GameManager.FallCount`, `BalanceSystem.Normalized` / `IsSlipping`, the hand grab events, and the stamina/peel-off/let-go fall causes). Logging is per-frame to a CSV on the headset, then aggregated offline.

| Metric | Definition | Source signal | Direction of interest |
|---|---|---|---|
| **Time-to-summit** | Seconds from first grab to `SummitTrigger`; runs that end in giving up are right-censored and reported separately | `GameManager.ElapsedTime` | Lower = faster, but not strictly "better" |
| **Fall count by cause** | Falls split into **let-go** (no hand in contact), **balance-slip** (`BalanceSystem` peel-off), **stamina** (`StaminaSystem` depletion) | Fall-cause tag at each `OnPlayerFell` | Pattern, esp. presence of balance-slips in B |
| **Grab success rate** | Successful grips ÷ grip attempts (button/trigger pressed within reach of a hold but no grab vs. grab) | `ClimbingHand` grip attempts vs. grips | Higher = better targeting |
| **Balance margin over time** | Time series of `BalanceSystem.Normalized` (1 = centred, 0 = peel-off) and fraction of climb time with `IsSlipping == true` | `BalanceSystem` | B only; characterises *how close to the edge* players climb |
| **On-device frame rate** | Mean / 1%-low FPS on Quest during each condition | Unity frame timing | Must hold target refresh; comfort prerequisite |

Frame rate is treated as a **comfort prerequisite, not an outcome**: if the balance + footwork layer drops the headset below its target refresh, any sickness result is confounded, so we verify FPS parity between A and B first.

#### Placeholder — Table 5.1: Objective results *(to be collected)*

| Metric | Condition A (hands-only) | Condition B (balance + footwork) | Notes |
|---|---|---|---|
| Time-to-summit (s), median [range] | — *to be collected* | — *to be collected* | per-participant points overlaid |
| Falls — let-go (mean/run) | — | — | |
| Falls — balance-slip (mean/run) | — *(expected ≈ 0 in A)* | — | H4 |
| Falls — stamina (mean/run) | — | — | |
| Grab success rate (%) | — | — | expect parity (same grab system) |
| Time spent slipping (% of climb) | n/a (no balance model) | — | B only |
| Mean FPS / 1%-low | — | — | parity check, H3 prerequisite |

#### Placeholder — Figure 5.1 *(to be collected)*
Balance-margin trace for a representative Condition-B run: `BalanceSystem.Normalized` (y, 0–1) vs. climb time (x), with peel-off events and the forced same-side stretch annotated. *No data yet — schematic only.*

#### Placeholder — Figure 5.2 *(to be collected)*
Stacked bar of fall causes (let-go / balance-slip / stamina) per condition, aggregated across participants. *No data yet.*

### 5.5 Subjective measures

Administered **after each condition** (so each participant rates both A and B):

- **NASA-TLX** (Hart and Staveland 1988) [14] — six subscales (mental, physical, temporal demand, performance, effort, frustration). Primary use here: detect whether B raises *physical* and *temporal* demand and *frustration* (H3). We report raw (un-weighted) TLX subscales given the small n.
- **Simulator Sickness Questionnaire (SSQ)** (Kennedy et al. 1993) [15] — nausea, oculomotor, disorientation subscales plus total. Core to H3: we predict **no significant increase** from A to B.
- **Realism / preference Likert items** (7-point), custom, including: "This felt like real climbing/bouldering," "I felt I was using my whole body, not just my arms," "The route felt challenging," "I enjoyed this," plus a forced-choice overall preference (A vs. B) and free-text comments. The whole-body and realism items directly test H1.

#### Placeholder — Table 5.2: Subjective results *(to be collected)*

| Measure | Condition A | Condition B | Predicted direction |
|---|---|---|---|
| NASA-TLX total (raw) | — *to be collected* | — | B ≈ A or slightly ↑ effort, **no ↑ frustration** |
| NASA-TLX physical demand | — | — | B somewhat ↑ (intended) |
| SSQ total | — | — | **B ≈ A (no increase)** — H3 |
| Realism Likert (1–7) | — | — | **B > A** — H1 |
| "Whole-body" Likert (1–7) | — | — | **B > A** — H1 |
| Challenge Likert (1–7) | — | — | **B > A** — H2 |
| Enjoyment Likert (1–7) | — | — | B ≥ A — H2 |
| Overall preference (A / B count) | — | — | majority B |

### 5.6 Procedure

1. **Consent and brief.** Explain the task ("climb to the summit"), seated/standing safety, and the option to stop at any time for discomfort. Collect VR and climbing experience.
2. **Fit and baseline SSQ.** Adjust the headset; administer a pre-exposure SSQ baseline.
3. **Tutorial wall** (not logged) in the participant's first condition until they have grabbed, pulled, and (in B) recovered balance at least once.
4. **Condition 1** (A or B per counterbalancing): climb the full route. On-device logging on. Up to a fixed time cap; record summit or give-up.
5. **Post-condition questionnaires:** NASA-TLX, SSQ, Likert + free text.
6. **Short break** (remove headset, ≥2 min) to let any sickness settle and to avoid carry-over.
7. **Condition 2** (the other build): repeat steps 4–5.
8. **Debrief:** forced-choice preference and open-ended feedback ("when did you feel most/least balanced?", "did the feet matter?").

Total session target ≈ 20–30 min/participant to limit cumulative fatigue and sickness exposure.

### 5.7 Planned analysis

Given **n ≈ 5–8**, analysis is primarily **descriptive and within-subject**: per-participant paired differences (B − A) for each measure, plotted with individual points. Where a test is reported we use a paired non-parametric test (Wilcoxon signed-rank) for the within-subject Likert/TLX/SSQ contrasts, treating results as **exploratory** and reporting effect sizes and raw differences rather than relying on p-values at this sample size. Objective metrics (time, fall causes, slip-time, FPS) are summarised per condition; the fall-cause breakdown (H4) and the balance-margin traces (Fig. 5.1–5.2) are analysed qualitatively for the *foot-seeking* signature.

### 5.8 Threats to validity (acknowledged in advance)

- **Sample.** n ≈ 5–8 classmates is a convenience sample; results are indicative, not generalisable, and several participants will have prior exposure to the project. We do not claim statistical power for H3's *null* (no sickness increase) — absence of a detected increase at this n is weak evidence, and we say so.
- **Novelty / order effects.** Counterbalancing mitigates but does not remove learning across the two climbs on the same route; the tutorial wall and the break are intended to dampen this.
- **Experimenter bias.** The team built the balance layer it is evaluating; the realism/preference items are self-report and susceptible to demand characteristics. The forced-choice preference and free-text are reported verbatim to expose this.
- **Construct validity of the CoM proxy.** Balance is judged from the HMD, not a true centre of mass; a participant who holds their head unusually still or leans from the hips may be mis-scored. The grace time and support margin are tuned by feel, not measured biomechanics (Section 4) — so "balance-slip" falls measure *our model's* notion of imbalance, not ground truth.
- **Comfort confound.** If FPS is not matched between A and B, any SSQ difference is uninterpretable; hence the explicit FPS parity check (§5.4) precedes the comfort claim.

## 6. Discussion & Limitations

### 6.1 What the design buys

Taken together, Sections 3 and 4 show that the two skills missing from commercial VR climbing — footwork and balance — can be reintroduced on commodity hardware *without* adding any tracking, any physics solver, or any rendered legs. The lever is a deliberate reframing: we model the **mechanical consequence** of a foot (a wider base of support) rather than the foot itself, and we judge balance from the one tracked point that already correlates with gross lean (the HMD). This keeps the whole layer cheap enough to run inside the standalone Quest frame budget (§4.10) while still being load-bearing — the forced same-side stretch (§3.5.3) cannot be passed without it. The graded, hysteretic meter (§3.3.4) is what makes the layer *additive* rather than punitive: it telegraphs a slip, exposes explicit counterplay (place a foot, flag, add an opposite-side hold), and only commits to a fall after sustained over-lean, so the new failure mode is a skill check rather than a dice roll. This is the property we expect to be decisive for H3 — challenge can rise while comfort holds — and it is the property no shipped commercial VR climber currently offers.

### 6.2 Limitations of the model

The model's strengths and its limitations are the same simplifications, and we state them plainly.

- **The CoM proxy is the head, not the centre of mass.** A climber who leans from the hips while keeping the head still, or who tucks the head during a move, will be mis-scored — the model measures *head lean*, which we argue tracks gross balance failure (barn-dooring) but is not biomechanically faithful. "Balance-slip" falls therefore measure *our model's* notion of imbalance, not ground truth.
- **Balance is one-dimensional.** Collapsing the support polygon to a single lateral interval (§3.3.2) captures barn-dooring, the dominant climbing-balance failure, but ignores fore–aft balance, rotational moments, and the vertical component entirely. Routes that fail in those axes will not register.
- **Feet are inferred, not sensed.** Virtual feet auto-snap to the nearest eligible hold (§3.4); the player cannot deliberately *choose* a foothold, place a foot precisely, or smear on a blank wall. The abstraction delivers the support-base consequence of footwork but not its dexterity, and it presumes a foothold exists where the solver looks.
- **Thresholds are tuned by feel.** Every constant in §4.9 — support margin, max overshoot, grace time, drain/regen rates — was set by playtesting feel against comfort and challenge, not derived. They are almost certainly not optimal, and they may not generalise across body sizes, arm spans, or play styles; the inspector-exposed constants are designed precisely so the planned study can sweep them.
- **No real legs, by choice.** We render foot markers, not articulated legs. This is a deliberate trade (§3.4.3, §2.4) — a wrong leg can damage embodiment more than a missing one — but it does mean the lower body is visually absent, and some players may find that less immersive than a (incorrect) leg would have been. This is itself an empirical question the realism Likert items are meant to probe.

### 6.3 The unavoidable caveat: no results yet

The largest limitation is one of status, not design: **the system is implemented but has not been playtested.** Everything in Section 5 is a protocol, and every hypothesis (H1–H4) is a prediction. We have been careful to mark this throughout — the placeholder tables are intentionally empty — because the honest contribution of this report is a *runnable system plus a falsifiable, pre-registered-style evaluation*, not an empirical claim. It remains entirely possible that the layer raises sickness (failing H3), that players find the auto-snapped feet illegible or arbitrary (failing H1/H4), or that the forced same-side stretch reads as a gimmick rather than a skill (failing H2). The design is built to make those outcomes *detectable* — particularly the fall-cause split and the balance-margin traces (§5.4) — rather than to hide them.

## 7. Conclusion & Future Work

We presented **Summit VR**, a Unity bouldering game for the Meta Quest that reintroduces the two skills commercial VR climbing omits — footwork and balance — using only the three tracked points (head and two controllers) available on stock hardware. The core idea is to abstract what the headset cannot sense and simulate only what changes play: feet become auto-snapping *contact state* that widens a base of support, and balance becomes a one-dimensional lateral support-interval test of the HMD-as-CoM proxy, drained through a graded, hysteretic meter that telegraphs slips and rewards recovery. The layer is built to be *additive* over a proven counter-motion grab-and-pull baseline, and a procedurally generated route with a forced same-side stretch makes it provably load-bearing. The system is fully implemented and runnable (including headless, in the XR Device Simulator), and we have specified a pre-registered-style evaluation — within-subject baseline vs. full, with time-to-summit, cause-split falls, balance-margin traces, NASA-TLX, SSQ, and realism/preference items — to test the central, falsifiable claim that the layer raises perceived realism and challenge without raising simulator sickness.

The immediate next step is obvious and is the project's main open item: **run the planned study** (Section 5) with n ≈ 5–8 classmates, build the on-device CSV logging into the build, and report the now-empty tables — turning every prediction into a measured result, or a refuted one. Beyond that, several directions follow naturally from the limitations of Section 6:

- **Richer balance, still cheap.** Extend the one-dimensional support interval toward a true 2-D support polygon (adding fore–aft balance and rotational moments), and import more of the robotics lineage we only borrow vocabulary from — a capture-point-style "where must I place a contact to recover" hint [8] could drive an explicit flag suggestion.
- **Player-controlled feet.** Replace pure auto-snapping with a lightweight player gesture for choosing and placing a foothold (as in Mitsuda and Kimura's downward-gesture placement [6]), recovering some of the dexterity the abstraction currently discards while keeping the support-base model.
- **Better CoM estimation.** Combine the HMD with controller positions to estimate torso lean more faithfully than head-alone, reducing the hip-lean and head-tuck mis-scoring identified in §6.2.
- **Parameter sweeps and adaptivity.** Use the inspector-exposed constants (§4.9) and the planned logging to tune thresholds per player — arm span, height, skill — rather than shipping a single hand-tuned set, and explore difficulty that adapts to a player's measured balance margin.
- **Input and embodiment studies.** Run the deferred controller-grip vs. hand-tracking comparison flagged in §5.1, and test whether an optional *cosmetic* IK leg (§3.4.3) — explicitly outside the balance computation — helps or harms embodiment relative to bare foot markers.
- **Content and haptics.** Grow the route library beyond the single default wall, add fragile/rest-hold-driven route puzzles that exploit the existing hold types, and explore richer haptic feedback for the moment of a balance slip.

In short, *Summit VR* contributes a runnable existence proof that footwork and balance can be brought to controller-only VR climbing cheaply and additively, together with the instrumentation and protocol needed to find out whether doing so actually makes the experience feel more like real bouldering. The system is ready; the measurement is the work that remains.

## References

1. Another Axiom (2021). *Gorilla Tag.*
2. Bald Spot Studios (2026). *New Heights: Realistic Climbing and Bouldering.* https://store.steampowered.com/app/2179440/New_Heights_Realistic_Climbing_and_Bouldering/
3. Crytek (2019/2021). *The Climb 2.* https://en.wikipedia.org/wiki/The_Climb_2
4. Kim, S. et al. (2022). InfinityWall — Vertical Locomotion in Virtual Reality using a Rock Climbing Treadmill. *CHI 2022.* https://dl.acm.org/doi/fullHtml/10.1145/3491101.3519654
5. Kosmalla, F., Zenner, A., Tasch, C., Daiber, F., and Krüger, A. (2020). The Importance of Virtual Hands and Feet for Virtual Reality Climbing. *CHI EA '20: Extended Abstracts of the 2020 CHI Conference on Human Factors in Computing Systems.* https://dl.acm.org/doi/10.1145/3334480.3383067
6. Mitsuda, T., and Kimura, T. (2026). Virtual climbing: climb in place with four limbs. *Frontiers in Virtual Reality.* https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2026.1764455/full
7. Mottelson, A., Muresan, A., Hornbæk, K., and Makransky, G. (2023). A Systematic Review and Meta-analysis of the Effectiveness of Body Ownership Illusions in Virtual Reality. *ACM Transactions on Computer-Human Interaction (TOCHI).* https://dl.acm.org/doi/full/10.1145/3590767
8. Pratt, J., Carff, J., Drakunov, S., and Goswami, A. (2006). Capture Point: A Step toward Humanoid Push Recovery. *IEEE-RAS International Conference on Humanoid Robots (Humanoids 2006).* https://www.semanticscholar.org/paper/Capture-Point:-A-Step-toward-Humanoid-Push-Recovery-Pratt-Carff/9d9aabdfd5b47862bdba0b9bb35711065b673418
9. Stewart, T. H. et al. (2022). Actual vs. perceived exertion during active virtual reality game exercise. *Frontiers in Rehabilitation Sciences.* https://www.frontiersin.org/journals/rehabilitation-sciences/articles/10.3389/fresc.2022.887740/full
10. Unity Technologies (2023). Animation Rigging: Two Bone IK. https://docs.unity3d.com/Packages/com.unity.animation.rigging@latest
11. Unity Technologies (2024). XR Interaction Toolkit — Climb Provider / Climb Interactable. https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/climb-provider.html
12. Vukobratović, M. and Borovac, B. (2004). Zero-Moment Point — Thirty-Five Years of Its Life. *International Journal of Humanoid Robotics* 1(1), 157–173. https://doi.org/10.1142/S0219843604000083
13. Born, F., Abramowski, S., and Masuch, M. (2019). Exergaming in VR: The Impact of Immersive Embodiment on Motivation, Performance, and Perceived Exertion. *IEEE AIVR 2019.* https://ieeexplore.ieee.org/document/8864579/
14. Hart, S. G., and Staveland, L. E. (1988). Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research. *Advances in Psychology*, 52, 139–183.
15. Kennedy, R. S., Lane, N. E., Berbaum, K. S., and Lilienthal, M. G. (1993). Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness. *The International Journal of Aviation Psychology*, 3(3), 203–220.