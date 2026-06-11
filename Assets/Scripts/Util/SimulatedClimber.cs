using System.Collections.Generic;
using UnityEngine;
using VRClimb.Climbing;
using VRClimb.Gameplay;

namespace VRClimb.Util
{
    /// <summary>
    /// Scripted "robot climber" used by the headless end-to-end check (Assets/Editor/HeadlessCheck.cs).
    /// It drives the REAL gameplay stack — ClimbingHand (via overrideGrip), ClimbController,
    /// FootPlacementSystem, BalanceSystem, GameManager, SummitTrigger — with no controllers attached.
    ///
    /// Phase A (balance): teleport to an empty wall section, grab an isolated hold, lean the head out
    /// of support, and assert that the meter drains, PeelOff fires, the climber falls and respawns.
    /// Phase B (climb): climb route 0 to the summit hand-over-hand, leaning the head toward the
    /// support midpoint the way a real player keeps their CoM over their feet, and assert Summit is
    /// reached with the timer running and exactly the one scripted fall.
    ///
    /// Results are exposed in static fields read by the editor harness. The component does nothing
    /// unless explicitly added to a wired rig, so it is inert in normal gameplay scenes.
    /// </summary>
    public class SimulatedClimber : MonoBehaviour
    {
        // ---- results read by HeadlessCheck ----
        public static bool Done;
        public static readonly List<string> Failures = new List<string>();
        public static string Summary = "";

        [Header("Wired by HeadlessCheck")]
        public ClimbController controller;
        public BalanceSystem balance;
        public FootPlacementSystem feet;
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;
        public Transform head;
        public Transform rig;

        enum Phase { PeelOff, ClimbReach, ClimbWaitGrab, ClimbPull, Finished }
        Phase _phase;
        float _phaseTime, _totalTime;

        Vector3 _start;
        ClimbHold _testHold;
        int _peelCount;
        readonly List<ClimbHold> _route = new List<ClimbHold>();
        int _next;
        ClimbingHand _gripHand, _freeHand;
        int _maxFeet;
        readonly List<string> _log = new List<string>();
        int _passCount;

        const float MaxReach = 1.6f;   // a hold is reachable when within this height above the rig
        const float PullSpeed = 1.2f;  // m/s of simulated hand pull
        // Head re-centring speed. Must cross the widest hand-switch (~0.9 m) faster than the
        // BalanceSystem grace window (0.35 s), like a player shifting weight as they reach.
        const float LeanSpeed = 3f;
        static readonly List<Vector3> s_Pts = new List<Vector3>(8);

        void Start()
        {
            Done = false; Failures.Clear(); Summary = "";
            _start = rig.position;
            if (balance != null) balance.PeelOff += OnPeel;

            // Phase A: empty stretch of wall — no foot holds within reach, one isolated hand hold.
            Teleport(new Vector3(1.2f, 0f, rig.position.z));
            _testHold = MakeTestHold(new Vector3(1.2f, 1.5f, 0.12f));
            SetHeadLocalX(0.4f);   // lean outward (away from the route, so no foot can catch us)
            rightHand.handTransform.position = _testHold.GripPoint;
            rightHand.overrideGrip = true;
            _freeHand = leftHand;
            _phase = Phase.PeelOff;
            Debug.Log("[Sim] phase A: gripping isolated hold, leaning out — expecting peel-off.");
        }

        void OnPeel()
        {
            _peelCount++;
            // Do not instantly re-grab the hold we just peeled off.
            leftHand.overrideGrip = rightHand.overrideGrip = false;
        }

        void Update()
        {
            if (_phase == Phase.Finished) return;
            _phaseTime += Time.deltaTime;
            _totalTime += Time.deltaTime;
            if (_totalTime > 120f) { Fail("overall sim timeout (120 s)"); return; }

            switch (_phase)
            {
                case Phase.PeelOff:      StepPeelOff();  break;
                case Phase.ClimbReach:   LeanTowardSupport(); StepReach();    break;
                case Phase.ClimbWaitGrab:LeanTowardSupport(); StepWaitGrab(); break;
                case Phase.ClimbPull:    LeanTowardSupport(); StepPull();     break;
            }
        }

        // ---- Phase A ----

        void StepPeelOff()
        {
            var gm = GameManager.Instance;
            if (_peelCount >= 1 && gm != null && gm.FallCount >= 1)
            {
                Check("balance drained while leaning out -> PeelOff fired", true);
                Check("fall registered (FallCount == 1)", gm.FallCount == 1);
                Check("respawned at the start point", Vector3.Distance(rig.position, _start) < 0.3f);
                Check("balance reset after respawn", balance.Balance > 0.9f);

                if (_testHold != null) DestroyImmediate(_testHold.gameObject);
                SetHeadLocalX(0f);
                BuildRouteList();
                Check("route has hand-path holds", _route.Count >= 5);
                _phase = Phase.ClimbReach; _phaseTime = 0f;
                Debug.Log($"[Sim] phase B: climbing {_route.Count} holds to the summit.");
            }
            else if (_phaseTime > 20f)
            {
                Fail($"peel-off phase timed out (peels={_peelCount}, falls={(gm != null ? gm.FallCount : -1)}, balance={balance.Balance:0.00})");
            }
        }

        // ---- Phase B ----

        void StepReach()
        {
            if (CheckSummitDone()) return;
            if (_next >= _route.Count) { _phase = Phase.ClimbPull; return; }

            var target = _route[_next];
            if (target.GripPoint.y - rig.position.y <= MaxReach)
            {
                _freeHand.handTransform.position = target.GripPoint;
                _freeHand.overrideGrip = true;
                _phase = Phase.ClimbWaitGrab; _phaseTime = 0f;
            }
            else _phase = Phase.ClimbPull;   // not reachable yet — keep pulling on the current hold
        }

        void StepWaitGrab()
        {
            var target = _route[_next];
            if (_freeHand.IsGripping && _freeHand.CurrentHold == target)
            {
                if (_gripHand != null) _gripHand.overrideGrip = false;   // old hand lets go
                var old = _gripHand;
                _gripHand = _freeHand;
                _freeHand = old != null ? old : (_gripHand == leftHand ? rightHand : leftHand);
                _next++;
                _phase = Phase.ClimbPull; _phaseTime = 0f;
            }
            else if (_phaseTime > 5f)
            {
                Fail($"hand failed to grab hold #{_next} ('{target.name}' at {target.GripPoint})");
            }
        }

        void StepPull()
        {
            if (CheckSummitDone()) return;
            if (_gripHand == null) { _phase = Phase.ClimbReach; return; }

            // Pull the gripping hand down -> counter-motion raises the rig up the wall.
            var ht = _gripHand.handTransform;
            ht.position += Vector3.down * Mathf.Min(PullSpeed * Time.deltaTime, 0.04f);

            bool nextReachable = _next < _route.Count &&
                                 _route[_next].GripPoint.y - rig.position.y <= MaxReach;
            float armLocalY = ht.position.y - rig.position.y;

            if (nextReachable) { _phase = Phase.ClimbReach; return; }
            if (armLocalY < 0.3f)
                Fail(_next >= _route.Count
                    ? "ran out of pull on the finish hold before the summit trigger fired"
                    : $"ran out of pull before hold #{_next} became reachable");
            else if (_phaseTime > 30f)
                Fail($"pull phase stalled (rig.y={rig.position.y:0.00}, next=#{_next})");
        }

        bool CheckSummitDone()
        {
            _maxFeet = Mathf.Max(_maxFeet, feet.PlantedCount);
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Summit) return false;

            Check("summit reached", true);
            Check("climb timer ran (ElapsedTime > 1 s)", gm.ElapsedTime > 1f);
            Check("exactly one fall — the scripted peel-off", gm.FallCount == 1);
            Check("no unintended peel-off during the climb", _peelCount == 1);
            Check("virtual feet planted during the climb", _maxFeet > 0);
            Finish($"SUMMIT in {gm.ElapsedTime:0.0} s (sim {_totalTime:0.0} s), falls={gm.FallCount}, " +
                   $"peels={_peelCount}, maxFeetPlanted={_maxFeet}");
            return true;
        }

        // Keep the CoM (head) over the midpoint of the current support span — what a real player
        // does with their body; this is exactly the lean the balance mechanic asks for.
        void LeanTowardSupport()
        {
            s_Pts.Clear();
            if (leftHand.IsGripping && leftHand.CurrentHold != null) s_Pts.Add(leftHand.CurrentHold.GripPoint);
            if (rightHand.IsGripping && rightHand.CurrentHold != null) s_Pts.Add(rightHand.CurrentHold.GripPoint);
            feet.CollectContacts(s_Pts);
            if (s_Pts.Count == 0) return;

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < s_Pts.Count; i++)
            { float x = s_Pts[i].x; if (x < min) min = x; if (x > max) max = x; }

            float midLocal = Mathf.Clamp((min + max) * 0.5f - rig.position.x, -0.45f, 0.45f);
            var lp = head.localPosition;
            lp.x = Mathf.MoveTowards(lp.x, midLocal, LeanSpeed * Time.deltaTime);
            head.localPosition = lp;
        }

        // ---- helpers ----

        void BuildRouteList()
        {
            _route.Clear();
            foreach (var h in FindObjectsOfType<ClimbHold>())
                if (h.role != ClimbHold.HoldRole.Foot && !h.IsBroken) _route.Add(h);
            _route.Sort((a, b) => a.GripPoint.y.CompareTo(b.GripPoint.y));
            _next = 0;
            _gripHand = null;
            _freeHand = leftHand;
        }

        void Teleport(Vector3 pos)
        {
            var cc = controller.characterController;
            cc.enabled = false;
            rig.position = pos;
            cc.enabled = true;
        }

        void SetHeadLocalX(float x)
        {
            var lp = head.localPosition;
            lp.x = x;
            head.localPosition = lp;
        }

        ClimbHold MakeTestHold(Vector3 pos)
        {
            var go = new GameObject("SimTestHold");
            go.transform.position = pos;
            int layer = LayerMask.NameToLayer("Hold");
            if (layer >= 0) go.layer = layer;
            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.08f;
            return go.AddComponent<ClimbHold>();
        }

        void Check(string name, bool ok)
        {
            if (ok) { _passCount++; _log.Add("PASS  " + name); }
            else    { Failures.Add(name); _log.Add("FAIL  " + name); Debug.LogError("[Sim] FAIL: " + name, this); }
        }

        void Fail(string why)
        {
            Failures.Add(why);
            _log.Add("FAIL  " + why);
            Debug.LogError("[Sim] FAIL: " + why, this);
            Finish("ABORTED — " + why);
        }

        void Finish(string headline)
        {
            _phase = Phase.Finished;
            Summary = headline + "\n" + string.Join("\n", _log) +
                      $"\n{_passCount} passed, {Failures.Count} failed.";
            Done = true;
            Debug.Log("[Sim] " + Summary);
        }
    }
}
