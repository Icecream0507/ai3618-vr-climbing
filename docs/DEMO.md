# Demo Video Checklist (for the 6/18 pre and 7/2 submission)

Goal: a tight **60–90 second** clip that shows the core loop **and** our differentiator — balance +
footwork — clearly enough that a viewer who's never climbed gets it.

## Before recording
- Pick the route: **Warm-up** (RouteBuilder `routeIndex = 0`) reads clearest on camera; use **Balance
  Test** (`1`) if you want to show footwork being load-bearing.
- Capture method:
  - **In-editor (no headset):** XR Device Simulator + screen capture (OBS / Unity Recorder).
  - **Quest:** the headset's built-in record, or cast to a PC and capture.
- Make sure the **balance bar** is visible in frame (it turns red while slipping — that's the star).
- Do a dry run; climbing on camera is harder than it looks.

## Shot list (the beats)
1. **(0–5s) Title card** — "Summit VR — VR bouldering with balance & footwork".
2. **(5–12s) The wall + legend** — pan the wall; caption the colours (🟡 hand · 🟠 foot · 🟣 either · 🟢 finish).
3. **(12–25s) Climb** — grab yellow/purple holds and pull up a few moves (show counter-motion feels physical).
4. **(25–45s) THE MONEY SHOT** — reach out on the **same-side stretch**: balance bar drains and flashes
   **red**, then **place a foot on an orange hold** (or add an opposite-side hold) and watch it recover.
   This is the one thing no shipped VR climber does — make it unmistakable.
5. **(45–55s) A fall** — let balance run out → peel off → respawn at checkpoint (show the fall count).
6. **(55–75s) Top out** — reach the summit → "Summit!" + final time on the HUD.
7. **(optional) Second route / stamina / fragile hold** if you have time left in the 90s.

## Tips
- Caption or voice-over the **balance mechanic** as it happens — viewers won't infer it on their own.
- Keep cuts tight; cut the dead air between moves.
- Steady framerate matters more than resolution for judging feel.

## Talking points (for the live pre)
- **The gap:** VR climbing is big (*The Climb*, *Gorilla Tag*) but **hands-only — no feet, no balance**.
  Real bouldering is *about* footwork and balance.
- **Our idea:** add a balance + footwork layer on **stock Quest** (head + 2 controllers, no foot
  trackers): the **HMD as a centre-of-mass proxy**, feet **abstracted** onto holds.
- **Grounded in research:** Kosmalla et al. (CHI 2020) — feet matter more than hands; Mitsuda & Kimura
  (Frontiers in VR 2026) — head-as-CoM balance model. (See `docs/RESEARCH.md` / `docs/REPORT.md`.)
- **Honest status:** playable vertical slice; balance/feet are deliberately abstracted, not faked-real.
