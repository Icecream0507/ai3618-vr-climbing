using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;     // TrackedPoseDriver (Input System, not the legacy SpatialTracking one)
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;             // XROrigin
using VRClimb.Climbing;
using VRClimb.Gameplay;
using VRClimb.Util;

namespace VRClimb.EditorTools
{
    /// <summary>
    /// Builds a first-person <b>VR-controller</b> scene driven by the <b>XR Device Simulator</b> — play
    /// the real climbing stack with simulated VR controllers + HMD, no headset required. Same gameplay
    /// construction as <see cref="PlayBuild"/>/<see cref="DemoBuild"/>, but the third-person rig +
    /// mouse driver are replaced by an <see cref="XROrigin"/> whose camera and two controller transforms
    /// are tracked by <see cref="TrackedPoseDriver"/>s, and whose <see cref="ClimbingHand.gripAction"/>s
    /// read the simulated controllers' grip. Pure input — no gameplay-math changes (same contract the
    /// headless robot and the mouse driver use: write handTransform + overrideGrip; ClimbController does
    /// the rest).
    ///
    ///   • Menu <b>VRClimb ▸ Build VR Scene</b> — imports the simulator sample if needed, saves
    ///     Assets/Scenes/VR.unity. Open it and press Play, then use the on-screen XR Device Simulator
    ///     controls (hold Left-Shift / Space to grab a controller, move with WASD + mouse, grip to grab).
    /// </summary>
    public static class VRBuild
    {
        const string ScenePath = "Assets/Scenes/VR.unity";
        const string PkgName = "com.unity.xr.interaction.toolkit";
        const string SampleName = "XR Device Simulator";
        const float EyeHeight = 1.6f;     // matches PlayBuild's proven resting head height above the rig base

        static int _routeIndex = 0;       // Warm-up by default; change RouteBuilder.routeIndex + Build Route to switch

        [MenuItem("VRClimb/Build VR Scene")]
        public static void BuildAndSave()
        {
            ClimbSceneSetup.EnsureLayer("Hold");
            RenderPipelineSetup.Ensure();
            EnsureSimulatorImported();
            BuildScene();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[VRBuild] Saved " + ScenePath + " — open it and press Play. " +
                      "Hold Left-Shift to aim the LEFT hand / Space for the RIGHT hand; move it with the " +
                      "mouse + WASD (Q/E = down/up); tap G to grab (it LATCHES — tap G again to let go). " +
                      "Hold right-mouse to look around. Move a gripped hand DOWN to haul yourself up.");
        }

        // Entry point for -executeMethod (headless build smoke test): import + build + save, exit 0/3.
        public static void BuildAndExit()
        {
            try { BuildAndSave(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogError(e); EditorApplication.Exit(3); }
        }

        // --- XR Device Simulator sample import (idempotent) -------------------------------------

        [MenuItem("VRClimb/Import XR Device Simulator")]
        public static void EnsureSimulatorImported()
        {
            if (LoadSimulatorPrefab() != null) return;   // already imported

            string ver = ResolveXriVersion();
            var samples = UnityEditor.PackageManager.UI.Sample.FindByPackage(PkgName, ver);
            if (samples != null)
            {
                foreach (var s in samples)
                {
                    if (s.displayName != SampleName) continue;
                    s.Import(UnityEditor.PackageManager.UI.Sample.ImportOptions.HideImportWindow |
                             UnityEditor.PackageManager.UI.Sample.ImportOptions.OverridePreviousImports);
                    break;
                }
            }
            AssetDatabase.Refresh();
            if (LoadSimulatorPrefab() == null)
                Debug.LogWarning("[VRBuild] Could not import the '" + SampleName + "' sample from " +
                                 PkgName + " " + ver + ". Import it manually via Package Manager → Samples.");
        }

        static string ResolveXriVersion()
        {
            foreach (var p in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                if (p.name == PkgName) return p.version;
            return "3.2.1";
        }

        static GameObject LoadSimulatorPrefab()
        {
            string ver = ResolveXriVersion();
            string path = $"Assets/Samples/XR Interaction Toolkit/{ver}/{SampleName}/{SampleName}.prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) return go;

            if (AssetDatabase.IsValidFolder("Assets/Samples"))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Samples" }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(guid);
                    if (p.EndsWith("/" + SampleName + ".prefab"))
                        return AssetDatabase.LoadAssetAtPath<GameObject>(p);
                }
            }
            return null;
        }

        // --- Scene construction ----------------------------------------------------------------

        static void BuildScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);

            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(24f, 0.1f, 12f);
            floor.transform.position = new Vector3(0f, -0.05f, 1.2f);
            Paint(floor, new Color(0.3f, 0.33f, 0.36f));

            new GameObject("GameManager", typeof(GameManager));

            var rb = new GameObject("RouteBuilder").AddComponent<RouteBuilder>();
            rb.routeIndex = _routeIndex; rb.holdLayerName = "Hold"; rb.buildOnAwake = true;

            // --- XR Origin (first-person) ---
            //   XR Origin (rig: PlayerClimberSetup, CharacterController, ClimbController…) at the wall (z=0.4)
            //    └─ Camera Offset (eye height)
            //        ├─ Head  (Camera + TrackedPoseDriver <XRHMD>)
            //        ├─ LeftHand  (TrackedPoseDriver <XRController>{LeftHand})
            //        └─ RightHand (TrackedPoseDriver <XRController>{RightHand})
            // ClimbController moves the rig; the camera/controllers (children) ride along, so the
            // counter-motion (pull a gripped hand down → body rises) works exactly as for the robot.
            var originGo = new GameObject("XR Origin");
            originGo.transform.position = new Vector3(0f, 0f, 0.4f);
            var origin = originGo.AddComponent<XROrigin>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);
            offset.transform.localPosition = new Vector3(0f, EyeHeight, 0f);

            var camGo = new GameObject("Head", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.26f);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.04f;
            AddPoseDriver(camGo, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

            var left = new GameObject("LeftHand").transform;
            left.SetParent(offset.transform, false);
            AddPoseDriver(left.gameObject, "<XRController>{LeftHand}/devicePosition", "<XRController>{LeftHand}/deviceRotation");

            var right = new GameObject("RightHand").transform;
            right.SetParent(offset.transform, false);
            AddPoseDriver(right.gameObject, "<XRController>{RightHand}/devicePosition", "<XRController>{RightHand}/deviceRotation");

            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
            origin.CameraYOffset = EyeHeight;

            // --- Climbing stack (wired for VR) ---
            var setup = originGo.AddComponent<PlayerClimberSetup>();
            setup.head = camGo.transform; setup.leftController = left; setup.rightController = right;
            setup.SetUp();

            int holdLayer = LayerMask.NameToLayer("Hold");
            LayerMask mask = holdLayer >= 0 ? (LayerMask)(1 << holdLayer) : (LayerMask)~0;
            var leftHand = left.GetComponent<ClimbingHand>();
            var rightHand = right.GetComponent<ClimbingHand>();
            leftHand.holdLayer = rightHand.holdLayer = mask;
            leftHand.hapticOnGrab = rightHand.hapticOnGrab = false;
            // Pure VR: your real (simulated) arm already limits reach — drop the auto reach-gate that the
            // mouse/robot paths use, so a grab depends only on the controller actually being on the hold.
            leftHand.armReach = 0f; rightHand.armReach = 0f;
            leftHand.gripAction  = GripAction("LeftGrip",  "<XRController>{LeftHand}/grip");
            rightHand.gripAction = GripAction("RightGrip", "<XRController>{RightHand}/grip");

            var feet = originGo.GetComponent<FootPlacementSystem>();
            feet.holdLayer = mask;
            feet.leftFootMarker = MakeFootMarker("LeftFoot");
            feet.rightFootMarker = MakeFootMarker("RightFoot");

            // Cosmetic avatar (so feet / green held-holds / body read; mostly behind the FP camera).
            var human = new GameObject("Humanoid").AddComponent<HumanoidRig>();
            human.transform.SetParent(originGo.transform, false);
            human.head = camGo.transform; human.leftHand = left; human.rightHand = right;
            human.leftFoot = feet.leftFootMarker; human.rightFoot = feet.rightFootMarker;
            human.rig = originGo.transform;
            human.leftHandC = leftHand; human.rightHandC = rightHand;

            // Grip-as-toggle so the simulator is actually playable (tap to grab, tap to release).
            var latch = originGo.AddComponent<VRGripLatch>();
            latch.leftHand = leftHand; latch.rightHand = rightHand;

            // On-screen balance / timer / banners + control hint.
            var hud = originGo.AddComponent<VRHud>();
            hud.balance = originGo.GetComponent<BalanceSystem>();

            // The simulator itself (mouse/keyboard → simulated HMD + controllers, plus its controls UI).
            var simPrefab = LoadSimulatorPrefab();
            if (simPrefab != null)
                PrefabUtility.InstantiatePrefab(simPrefab);
            else
                Debug.LogWarning("[VRBuild] Scene built without the XR Device Simulator (prefab not found). " +
                                 "Run VRClimb ▸ Import XR Device Simulator, then rebuild.");
        }

        static void AddPoseDriver(GameObject go, string posBinding, string rotBinding)
        {
            var d = go.AddComponent<TrackedPoseDriver>();
            d.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            var pos = new InputAction("pos", InputActionType.Value, posBinding, expectedControlType: "Vector3");
            var rot = new InputAction("rot", InputActionType.Value, rotBinding, expectedControlType: "Quaternion");
            d.positionInput = new InputActionProperty(pos);
            d.rotationInput = new InputActionProperty(rot);
        }

        static InputActionProperty GripAction(string name, string binding)
        {
            var a = new InputAction(name, InputActionType.Value, binding, expectedControlType: "Axis");
            return new InputActionProperty(a);
        }

        static Transform MakeFootMarker(string name)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            Paint(s, new Color(0.11f, 0.11f, 0.13f));
            s.SetActive(false);
            return s.transform;
        }

        static void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            r.sharedMaterial = m;
        }
    }
}
