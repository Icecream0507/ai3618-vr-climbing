# Research: VR Climbing / Bouldering with Balance & Footwork

A market + literature survey done to ground our v1 design. The short version: **almost every shipped
VR climbing game is hands-only, with no feet and no balance** — the body just follows your hands.
"Balance + footwork as the core skill" is an *open niche* in VR, which is both our opportunity and
our design challenge (Quest tracks only head + 2 controllers, so we cannot track real feet).

## 1. Prior art

| Title | Type | Footwork | Balance |
|---|---|---|---|
| **The Climb / The Climb 2** (Crytek) | Commercial VR | None — "disembodied hands", no legs | None; you fall only by letting go / not reaching. The Climb 2 adds props that react to your weight, but *you* don't balance. |
| **To The Top** | Commercial VR | None — arm-swing + look-to-launch | None (momentum from arm swings) |
| **Gorilla Tag** | Commercial VR | None — explicitly legless | None in-game; real balance is on the player |
| **Climbey** | Indie VR | Reads Vive foot trackers **only for floor-height calibration**, not gameplay | Pendulum/swing physics, not managed balance |
| **New Heights: Realistic Climbing & Bouldering** (2026) | **Flatscreen**, 3rd-person | Advertises hands *and feet* matter | **Balance/body-position is a core advertised mechanic** — but flatscreen, balance is abstracted, not driven by your real body |
| **Mitsuda & Kimura 2026** (Frontiers in VR) | **Academic VR** ⭐ | Foot "places" on a hold on a **downward** motion (hold turns green); releases when lifted > ~1.5 cm | **Head used as CoM proxy**; if the head leans over the *unsupported* foot you fall; one-foot balance challenge; feet-only hang times out (~6 s) |
| **Kosmalla et al. 2020** (CHI EA) | Academic VR | Real wall + Vive foot trackers; foot placement is the measured task | Real gravity (real wall). Finding: **showing virtual feet matters more than hands** for perceived accuracy & enjoyment |
| **STRIDE** | Commercial VR | None | Reads real **torso lean** (dodge) and **crouch** comfortably — useful as a cheap "flag" input |

**Take-away:** The Climb / Gorilla Tag prove hands-only is a popular, safe baseline — and exactly what
we go beyond. New Heights proves there's appetite for balance/footwork climbing. Mitsuda & Kimura
(2026) is a near-exact blueprint we can adapt to 3-point tracking; Kosmalla (2020) is the citation
that justifies showing feet at all.

> Could **not** verify (do not over-claim in the report): *Adventure Climb VR*, *Deep Water Solo VR*,
> and *Send It!* are climber-themed but no source confirms a real footwork/CoM simulation. Numeric
> balance thresholds are unpublished everywhere — all our constants are tuned by feel.

## 2. Handling feet without trackers

| Approach | Pros | Cons | Our call |
|---|---|---|---|
| **Omit feet** (status quo) | Zero cost, proven, no nausea | No footwork skill; = The Climb | Fallback only |
| **Abstracted virtual feet** (auto-snap to foot/either holds, driven by body position) | Cheap, on-theme, feeds balance, no hardware/ML | Inferred, not your real feet; needs a small solver | ✅ **chosen for v1** |
| **Player-aimed foot** (point + commit) | Explicit agency, reliable | Feels "gamey"; a UI action, not a body action | Stretch |
| **Cosmetic IK legs** (Meta Generative Legs / FinalIK VRIK) | Embodied look | Climbing poses are their documented worst case → legs float/slide; must not drive gameplay | Avoid (or cosmetic only) |
| **Real foot tracking** (Vive shin trackers / phone) | Genuine footwork & 1-foot balance | Out of scope on stock Quest; fragile; everyone needs trackers | Out of scope |

## 3. Modelling balance

| Model | Idea | Verdict |
|---|---|---|
| **Head-as-CoM lean check** (Mitsuda & Kimura) | Compare head distance to grounded vs floating foot; lean over the unsupported side → fall | Cheapest credible model; validated in a VR study |
| **CoM-in-support-polygon** | Project CoM onto wall plane; is it inside the convex hull of contacts? signed margin = stability | Rigorous; needs a hull + point-in-polygon; static only |
| **Barn-door hinge** | Same-side contacts form an axis; gravity swings you open unless you flag / add an opposite-side contact / pull hips in | Most climbing-authentic; cheap; great counterplay |
| ZMP / capture-point (momentum) | Make dynos destabilising; "lunge here to save yourself" | Post-v1 polish; amplifies jitter |
| Full active-ragdoll | Physically convincing falls | Far too heavy; the "QWOP" tuning trap |

**Our v1 model** (in `BalanceSystem.cs`) is a lightweight blend of the first and third: head = CoM
proxy; contacts = gripped hand-holds + auto-placed feet; stability = whether the CoM's lateral
position sits within the contacts' lateral span (a 1-D support interval). Reaching far on same-side
holds pushes the CoM outside the span → the balance meter drains → peel-off → fall. Adding a foot or
an opposite-side hold widens/recentres the span → stable. Hysteresis + a grace time damp tracking
jitter (a key risk flagged below).

## 4. v1 design decisions (the honest scope)

1. **Keep counter-motion grab-and-pull** as the locomotion core (already correct); balance + feet are
   *additive, optional* layers so the hands-only baseline is never at risk.
2. **Feet = abstracted state**, auto-snapped to holds — not IK legs, not Generative Legs, not trackers.
3. **Balance = head-as-CoM lateral support test**, graded (a meter), not a binary fall.
4. **Counterplay** so balance is skill not RNG: add an opposite-side hold or place a foot. (Torso-lean
   "flag" and hips-to-wall are documented stretch goals.)
5. **Colour legend** (locked, avoids the green clash with the research's green=hand): **yellow = hand,
   orange = foot, purple = either, green = finish, red = fragile, blue = rest.**
6. **Routes hand-authored** (or the baked default) — *not* a grade-aware procedural generator (no
   canonical implementation; budget risk). Each route includes ≥1 forced same-side sequence so
   footwork/balance is provably load-bearing.

## 5. Risks (carry into the report's Discussion)

- **Tracking jitter** → spurious slips. *Mitigation:* hysteresis + grace time; prefer the static
  support test over momentum terms in v1.
- **Unpublished thresholds** → everything is tuned by feel; treat constants as tuning knobs.
- **Comfort/nausea** on a slip → telegraph slips (red bar / haptic / audio), keep movement 1:1, never
  force fast involuntary camera motion.
- **"Gamey" abstraction** → strong visual/audio/haptic feedback + clear counterplay.
- **Single-source model** → Mitsuda & Kimura is one paper (n=24, all-male); a starting point, not a
  settled fact.

## 6. Key sources

- Mitsuda & Kimura, *Virtual climbing: climb in place with four limbs*, Frontiers in VR 2026 — https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2026.1764455/full
- Kosmalla, Zenner et al., *The Importance of Virtual Hands and Feet for Virtual Reality Climbing*, CHI EA 2020 — https://dl.acm.org/doi/10.1145/3334480.3383067
- *The Climb 2* — https://en.wikipedia.org/wiki/The_Climb_2 · The Climb — https://www.ukclimbing.com/articles/features/the_climb_a_vr_game_turning_non-climbers_on_to_climbing-13377
- *Gorilla Tag* design — https://medium.com/syry-io/what-makes-gorilla-tag-great-vr-design-28b253ad0294
- *New Heights* — https://store.steampowered.com/app/2179440/New_Heights_Realistic_Climbing_and_Bouldering/
- *Climbey* — https://store.steampowered.com/app/520010/Climbey/ · *STRIDE* — https://store.steampowered.com/app/1292040/STRIDE/
- Barn-door / flagging — https://philarockgym.com/how-to-prevent-the-annoying-barn-door-in-climbing/ · https://sciencebehindthesport.wvu.edu/rock-climbing/balance-and-momentum
- Support polygon — https://en.wikipedia.org/wiki/Support_polygon · ZMP — https://www.cs.cmu.edu/~cga/legs/vukobratovic.pdf · capture point — https://arxiv.org/pdf/1705.10579
- Unity Animation Rigging TwoBoneIK — https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.1/manual/constraints/TwoBoneIKConstraint.html
- Meta inside-out body tracking / generative legs — https://developers.meta.com/horizon/blog/inside-out-body-tracking-and-generative-legs/
- XRI Climb Provider — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/climb-provider.html
