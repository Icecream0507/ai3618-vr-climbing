using System.IO;
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
    /// Builds the watchable demo scene — a third-person view of the robot climber driving the real
    /// gameplay stack (peel-off → fall → respawn → climb Route 0 → summit) with a visible avatar,
    /// follow camera and on-screen balance meter + captions.
    ///
    ///   • Menu <b>VRClimb ▸ Build Demo Scene</b> — saves Assets/Scenes/Demo.unity. Open it and press
    ///     Play to watch (and screen-record) the playthrough. No frames are written to disk.
    ///   • Menu <b>VRClimb ▸ Record Demo</b> / <c>-executeMethod VRClimb.EditorTools.DemoBuild.Record</c>
    ///     — builds the same scene with the <see cref="FrameRecorder"/> armed, plays it to the summit,
    ///     writing a JPG sequence to Logs/frames, then exits. ffmpeg turns that into the mp4.
    /// </summary>
    public static class DemoBuild
    {
        const string ScenePath = "Assets/Scenes/Demo.unity";
        const double TimeoutSeconds = 180;

        static bool _recording;
        static double _startTime;
        static double _doneTime = -1;

        [MenuItem("VRClimb/Build Demo Scene")]
        public static void BuildAndSave()
        {
            BuildScene(false);
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DemoBuild] Saved " + ScenePath + " — open it and press Play to watch the demo.");
        }

        [MenuItem("VRClimb/Record Demo")]
        public static void RecordFromMenu() { Record(); }

        // Entry point for -executeMethod (headless offline render).
        public static void Record()
        {
            ClimbSceneSetup.EnsureLayer("Hold");
            RenderPipelineSetup.Ensure();
            BuildScene(true);

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            _recording = true;
            _startTime = EditorApplication.timeSinceStartup;
            _doneTime = -1;
            SimulatedClimber.Done = false; SimulatedClimber.Failures.Clear(); SimulatedClimber.Summary = "";
            EditorApplication.update += Poll;
            EditorApplication.EnterPlaymode();
        }

        static void Poll()
        {
            bool timedOut = EditorApplication.timeSinceStartup - _startTime > TimeoutSeconds;

            // Keep capturing ~2 s after the summit so the final caption lands, then stop.
            if (SimulatedClimber.Done && _doneTime < 0) _doneTime = EditorApplication.timeSinceStartup;
            bool tailDone = _doneTime > 0 && EditorApplication.timeSinceStartup - _doneTime > 2.0;

            if (EditorApplication.isPlaying && !tailDone && !timedOut) return;

            if (EditorApplication.isPlaying) { EditorApplication.ExitPlaymode(); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EditorApplication.update -= Poll;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            int frames = LastFrameCount;
            string msg = $"[DemoBuild] Recording finished: {frames} frames in Logs/frames " +
                         (timedOut ? "(TIMED OUT) " : "") +
                         (SimulatedClimber.Done ? "(summit reached). " : "(sim did not finish). ") +
                         "Run ffmpeg to assemble the mp4.";
            Debug.Log(msg);
            File.WriteAllText("Logs/record-result.txt",
                msg + "\n" + SimulatedClimber.Summary + "\n");
            if (_recording) EditorApplication.Exit(SimulatedClimber.Done && !timedOut ? 0 : 3);
        }

        static int LastFrameCount
        {
            get
            {
                try { return Directory.Exists("Logs/frames") ? Directory.GetFiles("Logs/frames", "*.jpg").Length : 0; }
                catch { return 0; }
            }
        }

        // ---- scene construction ----

        static void BuildScene(bool forRecording)
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
            rb.routeIndex = 0; rb.holdLayerName = "Hold"; rb.buildOnAwake = true;

            // Spectator camera (also the MainCamera the systems will tolerate; head is explicitly wired).
            var camGo = new GameObject("SpectatorCamera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.19f, 0.26f);
            cam.fieldOfView = 56f;

            // Rig in front of the wall (front face at z = 0).
            var rig = new GameObject("DemoRig");
            rig.transform.position = new Vector3(0f, 0f, 0.4f);

            var head = new GameObject("Head").transform;
            head.SetParent(rig.transform, false);
            head.localPosition = new Vector3(0f, 1.6f, 0f);
            AddMarker(head, 0.16f, new Color(0.95f, 0.85f, 0.55f), "HeadView");

            var left = MakeHand(rig.transform, "LeftHand", new Vector3(-0.25f, 1.0f, 0.15f));
            var right = MakeHand(rig.transform, "RightHand", new Vector3(0.25f, 1.0f, 0.15f));

            var setup = rig.AddComponent<PlayerClimberSetup>();
            setup.head = head; setup.leftController = left; setup.rightController = right;
            setup.SetUp();

            int holdLayer = LayerMask.NameToLayer("Hold");
            LayerMask mask = holdLayer >= 0 ? (LayerMask)(1 << holdLayer) : (LayerMask)~0;
            var leftHand = left.GetComponent<ClimbingHand>();
            var rightHand = right.GetComponent<ClimbingHand>();
            leftHand.holdLayer = rightHand.holdLayer = mask;
            leftHand.hapticOnGrab = rightHand.hapticOnGrab = false;
            var feet = rig.GetComponent<FootPlacementSystem>();
            feet.holdLayer = mask;
            feet.leftFootMarker = MakeFootMarker("LeftFoot");
            feet.rightFootMarker = MakeFootMarker("RightFoot");

            var sim = rig.AddComponent<SimulatedClimber>();
            sim.controller = rig.GetComponent<ClimbController>();
            sim.balance = rig.GetComponent<BalanceSystem>();
            sim.feet = feet;
            sim.leftHand = leftHand; sim.rightHand = rightHand;
            sim.head = head; sim.rig = rig.transform;
            sim.demoMode = true;

            var vis = rig.AddComponent<DemoVisuals>();
            vis.head = head; vis.body = null; vis.spectator = cam; vis.lookTargetRoot = rig.transform;

            // Articulated humanoid (cosmetic, demo-only) so the spectator video reads as a person.
            var human = new GameObject("Humanoid").AddComponent<HumanoidRig>();
            human.transform.SetParent(rig.transform, false);
            human.head = head; human.leftHand = left; human.rightHand = right;
            human.leftFoot = feet.leftFootMarker; human.rightFoot = feet.rightFootMarker;
            human.rig = rig.transform;

            var overlay = rig.AddComponent<DemoOverlay>();
            overlay.spectator = cam; overlay.balance = sim.balance; overlay.sim = sim;

            var rec = rig.AddComponent<FrameRecorder>();
            rec.record = forRecording; rec.spectator = cam;
        }

        static Transform MakeHand(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            AddMarker(t, 0.1f, new Color(0.9f, 0.5f, 0.3f), name + "View");
            return t;
        }

        static void AddMarker(Transform parent, float size, Color color, string name)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false);
            s.transform.localScale = Vector3.one * size;
            Paint(s, color);
        }

        static Transform MakeFootMarker(string name)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.localScale = new Vector3(0.14f, 0.08f, 0.14f);
            Paint(s, new Color(1f, 0.55f, 0f));
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
