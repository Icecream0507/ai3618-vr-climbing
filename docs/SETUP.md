# Setup & Scene Building

This skeleton ships the **code**. Follow these steps to turn it into a playable scene. Times are
rough estimates for someone new to Unity XR.

## 0. Prerequisites (~30 min)

- **Unity Hub** + **Unity 2022.3 LTS** (or Unity 6 LTS). When installing, tick **Android Build
  Support** (SDK + NDK + OpenJDK) if you plan to build to a Quest.
- Open the project: Unity Hub → *Add project from disk* → select the `VRClimb/` folder.
- On first open, **Package Manager** resolves `Packages/manifest.json`. If it warns that a version
  is missing, click to accept the recommended version, or open *Window → Package Manager* and add:
  XR Interaction Toolkit, OpenXR Plugin, Input System, (optional) XR Hands.
- When prompted to enable the new **Input System backend**, choose **Yes** (restarts the editor).

## 1. Enable XR (~15 min)

1. *Edit → Project Settings → XR Plug-in Management* → **Install**.
2. **PC tab:** enable **OpenXR**. **Android tab:** enable **OpenXR**.
3. Under *OpenXR* fix any red ⚠ in the validation list. Add an **Interaction Profile**:
   - *Oculus Touch Controller Profile* (controllers), and/or *Meta Quest Touch Pro/Plus*.
4. (Quest build only) *Project Settings → Player → Android*: Minimum API ≥ 29, scripting backend
   IL2CPP, ARM64. Texture compression ASTC.

## 2. Import XRI Starter Assets (~10 min)

*Window → Package Manager → XR Interaction Toolkit → Samples → import* **Starter Assets** (and
**XR Device Simulator** if present). This gives you:

- A ready **XR Origin (XR Rig)** prefab with hands/controllers.
- The **input action asset** with `Select Value` / grip actions you wire into `ClimbingHand`.
- The **XR Device Simulator** prefab to test without a headset.

## 3. Build the climbing scene (~1–2 h)

1. New scene → save as `Assets/Scenes/ClimbGym.unity`.
2. Drag in the **XR Origin (XR Rig)** prefab. Add a **CharacterController** to the XR Origin root,
   then add our **`ClimbController`**; assign `rig` = XR Origin, and the left/right
   **`ClimbingHand`** (next step).
3. On each hand controller object, add **`ClimbingHand`**:
   - `gripAction` → bind to *XRI LeftHand/RightHand Interaction → Select Value* (or Grip).
   - `handTransform` → the controller's attach/transform.
   - `holdLayer` → a new layer named **Hold** (create it, step 4).
   - `hapticNode` → Left/Right hand accordingly.
   - Assign these two hands back into `ClimbController.leftHand/rightHand`.
4. **Holds:** make a small sphere/rock mesh, add a Collider, set its layer to **Hold**, add
   **`ClimbHold`**. Make it a prefab (`Hold_Normal`). Duplicate for `Hold_Finish` (type Finish),
   `Hold_Fragile`, `Hold_Rest`. Scatter them up a wall to form a route.
5. **Wall:** a tall cube/quad with a collider so the CharacterController rests against it.
6. **Summit:** a trigger box at the top with **`SummitTrigger`**; tag the XR Origin **Player** and
   set the trigger's `playerTag` = Player.
7. **Checkpoints (optional):** trigger volumes with **`Checkpoint`** at ledges.
8. **GameManager:** empty GameObject + **`GameManager`**.
9. **HUD (optional):** world-space Canvas on the wrist with two TMP texts + an Image (fill); add
   **`GameHUD`** and wire the fields. Add **`StaminaSystem`** to the rig and assign both hands.

## 4. Test (~ongoing)

- **In editor, no headset:** drag the **XR Device Simulator** prefab into the scene, press Play.
  Use the documented keys to move the head/hands and trigger grip to validate climbing logic.
- **Quest Link / Air Link:** connect, press Play — runs on the headset over the link.
- **Standalone build:** *File → Build Settings → Android → Build and Run* to the connected Quest.

## 5. Using Unity's built-in Climb Provider instead (alternative)

If you prefer the toolkit's implementation over our `ClimbController`:

1. Add a **Climb Provider** component (under the Locomotion system on the XR Origin).
2. Replace `ClimbHold` colliders' setup with **Climb Interactable** components (give child colliders
   the *Interactable* layer; set the interactor's mask accordingly).
3. Use **Climb Settings Override** on the topmost holds to allow free Y/Z so the player can mount the
   summit ledge instead of sliding back down.

Our gameplay scripts (`GameManager`, `SummitTrigger`, `StaminaSystem`, `Checkpoint`, `GameHUD`) work
with either locomotion backend.

## Common errors

| Symptom | Fix |
|---|---|
| Scripts don't compile: `InputActionProperty` not found | Install **Input System** package; enable its backend. |
| `TMP_Text` not found | Import **TextMeshPro Essentials** (*Window → TextMeshPro → Import*). |
| Can't grab anything | Holds not on the **Hold** layer, or `ClimbingHand.holdLayer` not set, or `grabRadius` too small. |
| Hand grabs but body doesn't move | `handTransform` not assigned, or hands not assigned into `ClimbController`. |
| Player falls through the wall | XR Origin needs a **CharacterController**; wall needs a collider. |
| Teleport-to-spawn jitter | Respawn disables/re-enables the CharacterController by design — keep that order. |
| Nothing renders in headset | XR Plug-in Management → OpenXR not enabled on the relevant (PC/Android) tab. |
