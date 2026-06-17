using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRClimb.Climbing;
using VRClimb.Gameplay;
using VRClimb.Util;

namespace VRClimb.EditorTools
{
    /// <summary>
    /// Builds a mouse/keyboard-playable scene — the real gameplay stack wired to a human driver
    /// (<see cref="PlayInputController"/>) instead of the robot, with a third-person follow camera,
    /// a visible avatar and an on-screen balance meter. Same construction as <see cref="DemoBuild"/>
    /// minus the SimulatedClimber/FrameRecorder.
    ///
    ///   • Menu <b>VRClimb ▸ Build Play Scene</b> — saves Assets/Scenes/Play.unity. Open it and press
    ///     Play to climb (mouse aims, L/R-click grabs left/right hand, A/D lean, W/S pull, R reset).
    /// </summary>
    public static class PlayBuild
    {
        const string ScenePath = "Assets/Scenes/Play.unity";

        [MenuItem("VRClimb/Build Play Scene")]
        public static void BuildAndSave()
        {
            ClimbSceneSetup.EnsureLayer("Hold");
            RenderPipelineSetup.Ensure();
            BuildScene();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayBuild] Saved " + ScenePath + " — open it and press Play. " +
                      "Mouse aims at a hold; left-click grabs with the left hand, right-click the right; " +
                      "A/D lean, W/S pull up / down, R reset.");
        }

        // Entry point for -executeMethod (headless build smoke test): build, save, exit 0.
        public static void BuildAndExit()
        {
            try { BuildAndSave(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogError(e); EditorApplication.Exit(3); }
        }

        static int _routeIndex = 0;   // Warm-up by default; change RouteBuilder.routeIndex + Build Route to switch

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

            var camGo = new GameObject("PlayCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.26f);
            cam.fieldOfView = 56f;
            camGo.transform.position = new Vector3(0f, 2.05f, 3.3f);

            // Player rig in front of the wall (front face at z = 0).
            var rig = new GameObject("PlayerRig");
            rig.transform.position = new Vector3(0f, 0f, 0.4f);

            var head = new GameObject("Head").transform;
            head.SetParent(rig.transform, false);
            head.localPosition = new Vector3(0f, 1.6f, 0f);

            var left = MakeHand(rig.transform, "LeftHand", new Vector3(-0.25f, 1.0f, 0.15f));
            var right = MakeHand(rig.transform, "RightHand", new Vector3(0.25f, 1.0f, 0.15f));

            var setup = rig.AddComponent<PlayerClimberSetup>();
            setup.head = head; setup.leftController = left; setup.rightController = right;
            setup.SetUp();

            var leftHand = left.GetComponent<ClimbingHand>();
            var rightHand = right.GetComponent<ClimbingHand>();
            leftHand.hapticOnGrab = rightHand.hapticOnGrab = false;
            var feet = rig.GetComponent<FootPlacementSystem>();
            feet.leftFootMarker = MakeFootMarker("LeftFoot");
            feet.rightFootMarker = MakeFootMarker("RightFoot");

            // Cosmetic avatar so you can read your body / balance / feet (and the green held-holds).
            var human = new GameObject("Humanoid").AddComponent<HumanoidRig>();
            human.transform.SetParent(rig.transform, false);
            human.head = head; human.leftHand = left; human.rightHand = right;
            human.leftFoot = feet.leftFootMarker; human.rightFoot = feet.rightFootMarker;
            human.rig = rig.transform;
            human.leftHandC = leftHand; human.rightHandC = rightHand;

            var play = rig.AddComponent<PlayInputController>();
            play.leftHand = leftHand; play.rightHand = rightHand;
            play.head = head; play.rig = rig.transform;
            play.controller = rig.GetComponent<ClimbController>();
            play.balance = rig.GetComponent<BalanceSystem>();
            play.feet = feet;
            play.cam = cam;
            play.holdLayer = leftHand.holdLayer;

            ClimbUIAudioSetup.ApplyToScene();
        }

        static Transform MakeHand(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            // No visible marker: HumanoidRig draws the real gripping fist at the IK end-effector.
            return t;
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
