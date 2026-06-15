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
    /// Headless CI-style check, runnable with no headset and no human:
    ///
    ///   Tuanjie.exe -projectPath . -batchmode -nographics ^
    ///       -executeMethod VRClimb.EditorTools.HeadlessCheck.Run -logFile Logs/e2e.log
    ///
    /// It (1) runs the ClimbMath self-test, (2) builds the SimTest scene (floor + route 0 +
    /// a plain camera rig wired by PlayerClimberSetup + a <see cref="SimulatedClimber"/> robot),
    /// saves it to Assets/Scenes/SimTest.unity, then (3) enters play mode and lets the robot
    /// climb: it must peel off when leaning out of support, fall, respawn, then climb route 0
    /// hand-over-hand to the summit. Exits with code 0 on success, 2 on any failure.
    /// The scene is kept in the project — open SimTest.unity and press Play to watch it.
    /// (Run is also available from the menu: VRClimb ▸ Run Headless Check.)
    /// </summary>
    public static class HeadlessCheck
    {
        const string ScenePath = "Assets/Scenes/SimTest.unity";
        const double TimeoutSeconds = 240;

        static int _mathPass, _mathFail;
        static double _startTime;
        static bool _exitWhenDone;   // true when launched via -executeMethod (batch)

        [MenuItem("VRClimb/Run Headless Check")]
        public static void RunFromMenu() { _exitWhenDone = false; Begin(); }

        // Entry point for -executeMethod.
        public static void Run() { _exitWhenDone = true; Begin(); }

        static void Begin()
        {
            // Domain reload is disabled below, so statics survive a previous run — reset them.
            SimulatedClimber.Done = false;
            SimulatedClimber.Failures.Clear();
            SimulatedClimber.Summary = "";

            // ---- 1. pure-math self-test (edit mode) ----
            var tester = new GameObject("MathSelfTest").AddComponent<ClimbMathSelfTest>();
            tester.Run();
            _mathPass = tester.LastPass; _mathFail = tester.LastFail;
            Object.DestroyImmediate(tester.gameObject);

            // ---- 2. build + save the simulation scene ----
            ClimbSceneSetup.EnsureLayer("Hold");
            RenderPipelineSetup.Ensure();   // keep URP materials from rendering magenta
            BuildScene();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[HeadlessCheck] Scene saved to " + ScenePath + ". Entering play mode...");

            // ---- 3. play the robot climber ----
            // Domain reload would wipe this class's statics and update hook; disable it for the run.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            _startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Poll;
            EditorApplication.EnterPlaymode();
        }

        static void Poll()
        {
            bool timedOut = EditorApplication.timeSinceStartup - _startTime > TimeoutSeconds;
            if (EditorApplication.isPlaying && !SimulatedClimber.Done && !timedOut) return;
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.update -= Poll; Report(timedOut); return; }
            if (SimulatedClimber.Done || timedOut) EditorApplication.ExitPlaymode();
        }

        static void Report(bool timedOut)
        {
            EditorSettings.enterPlayModeOptionsEnabled = false;   // restore normal editor behaviour

            int simFails = SimulatedClimber.Failures.Count + (SimulatedClimber.Done ? 0 : 1);
            bool ok = _mathFail == 0 && simFails == 0 && !timedOut;

            string report =
                "==== VRClimb headless check ====\n" +
                $"ClimbMath self-test : {_mathPass} passed, {_mathFail} failed\n" +
                $"End-to-end sim      : {(SimulatedClimber.Done ? "completed" : "DID NOT FINISH")}" +
                (timedOut ? " (TIMED OUT)" : "") + "\n" +
                SimulatedClimber.Summary + "\n" +
                $"RESULT: {(ok ? "PASS ✅" : "FAIL ❌")}\n";

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/headless-check.txt", report);
            Debug.Log(report);

            if (_exitWhenDone) EditorApplication.Exit(ok ? 0 : 2);
        }

        // Floor + GameManager + RouteBuilder(route 0) + a plain (non-XR) camera rig wired exactly
        // like the real player via PlayerClimberSetup, plus the SimulatedClimber driver.
        static void BuildScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(20f, 0.1f, 10f);
            floor.transform.position = new Vector3(0f, -0.05f, 1f);   // top surface at y = 0

            new GameObject("GameManager", typeof(GameManager));

            var rb = new GameObject("RouteBuilder").AddComponent<RouteBuilder>();
            rb.routeIndex = 0;            // Warm-up
            rb.holdLayerName = "Hold";
            rb.buildOnAwake = true;       // built at play time; the saved scene stays clean

            // Plain rig standing in front of the wall (wall front face is at z = 0).
            var rig = new GameObject("SimRig");
            rig.transform.position = new Vector3(0f, 0f, 0.4f);

            var head = new GameObject("Head").transform;
            head.SetParent(rig.transform, false);
            head.localPosition = new Vector3(0f, 1.6f, 0f);
            head.gameObject.AddComponent<Camera>();
            head.gameObject.tag = "MainCamera";

            var left = new GameObject("LeftHand").transform;
            left.SetParent(rig.transform, false);
            left.localPosition = new Vector3(-0.25f, 1.0f, 0.15f);
            var right = new GameObject("RightHand").transform;
            right.SetParent(rig.transform, false);
            right.localPosition = new Vector3(0.25f, 1.0f, 0.15f);

            var setup = rig.AddComponent<PlayerClimberSetup>();
            setup.head = head; setup.leftController = left; setup.rightController = right;
            setup.SetUp();   // adds + links CC, ClimbController, hands, feet, balance; assigns Hold layer

            var leftHand = left.GetComponent<ClimbingHand>();
            var rightHand = right.GetComponent<ClimbingHand>();
            leftHand.hapticOnGrab = rightHand.hapticOnGrab = false;
            var feet = rig.GetComponent<FootPlacementSystem>();

            var sim = rig.AddComponent<SimulatedClimber>();
            sim.controller = rig.GetComponent<ClimbController>();
            sim.balance = rig.GetComponent<BalanceSystem>();
            sim.feet = feet;
            sim.leftHand = leftHand; sim.rightHand = rightHand;
            sim.head = head; sim.rig = rig.transform;
        }
    }
}
