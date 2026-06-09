# Team Tasks & Schedule (5 people)

Dates: **today ≈ 6/9** · optional `pre` **6/18** (+bonus) · submission **7/2**.
Strategy: lock a **playable vertical slice by 6/18**, then add depth and polish for 7/2.

## Roles (1 person each — adjust to strengths)

| # | Owner role | Owns | Key files / deliverables |
|---|---|---|---|
| **P1** | **Climbing systems** | locomotion correctness & feel | `ClimbController`, `ClimbingHand`, grab tuning, two-hand handling |
| **P2** | **Gameplay & rules** | win/lose loop, stamina, holds | `GameManager`, `SummitTrigger`, `Checkpoint`, `StaminaSystem`, `ClimbHold` types |
| **P3** | **Scene / level design & art** | the wall, routes, look | `ClimbGym.unity`, hold prefabs, materials, lighting, skybox |
| **P4** | **XR integration & build** | rig, input, device/Quest builds | XR Plug-in Management, input actions, XR Device Simulator, Android build, haptics |
| **P5** | **UX, audio, report & video** | HUD, sound, docs | `GameHUD`, audio hookup, README/report, demo capture & editing |

> These overlap on purpose — P1+P4 pair on input, P2+P3 pair on hold layout, P5 tracks the report
> from day one so writing isn't a last-night scramble.

## Milestones

### Week of 6/9 – 6/18 → **vertical slice + (optional) pre**
- [ ] **P4:** project opens clean, OpenXR enabled, XR Origin + Device Simulator running. *(by 6/11)*
- [ ] **P1:** grab + counter-motion working in the simulator; can pull up a couple of holds. *(by 6/13)*
- [ ] **P3:** one rough route up a wall with placed holds + a summit zone. *(by 6/14)*
- [ ] **P2:** fall→respawn, summit→win, timer counting. *(by 6/15)*
- [ ] **P5:** minimal HUD + capture a 60–90s demo; draft Abstract/Intro/Method. *(by 6/17)*
- [ ] **All:** rehearse + (optional) sign up for the **6/18 pre**.

### Week of 6/19 – 6/25 → **depth**
- [ ] Stamina + fragile/rest holds tuned (P1/P2).
- [ ] 2+ routes, better art & lighting, audio (P3/P5).
- [ ] Comfort options (vignette / 1:1 movement check) + haptics (P1/P4).
- [ ] (Stretch) hand-tracking interface to compare vs controllers (P4/P1).

### Week of 6/26 – 7/2 → **polish, test, submit**
- [ ] Playtest with classmates; collect metrics (time, falls, grab success, TLX) (P5/all).
- [ ] Bug-fix pass, frame-rate check on device (P4/P1).
- [ ] Finalize report (P5 + all write your own section), record final **demo video**.
- [ ] **Submit:** GitHub link + demo video by **7/2**.

## Git workflow
- `main` stays runnable. Branch per feature: `feat/climb-tuning`, `feat/stamina`, `feat/level-1`.
- **Always commit `.meta` files** alongside their assets (the `.gitignore` already keeps them).
- Keep large binaries (big models/audio) reasonable; consider Git LFS if they get heavy.
- Small, frequent PRs; whoever owns the system reviews changes to it.

## Definition of done (for the grade)
A stranger can: clone → open in Unity → press Play (simulator) → grab holds → climb → fall/respawn →
reach the summit and see a time. Everything else is depth on top of that.
