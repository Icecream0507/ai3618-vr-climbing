using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRClimb.Climbing;
using VRClimb.Gameplay;

namespace VRClimb.Util
{
    /// <summary>
    /// Mouse + keyboard play driver for the climbing stack — lets a human climb in the editor with no
    /// headset. It drives the SAME contract the headless <see cref="SimulatedClimber"/> uses: it only
    /// ever writes <see cref="ClimbingHand.handTransform"/> position + <see cref="ClimbingHand.overrideGrip"/>;
    /// <see cref="ClimbController"/> does all the rig movement (counter-motion), balance, feet, falls and
    /// summit. Nothing here feeds gameplay math — it's pure input, so the e2e/asserts are untouched.
    ///
    /// Controls (third-person):
    ///   • Aim the mouse at a hold (it gets a reticle) → <b>Left-click = grab with left hand</b>,
    ///     <b>Right-click = grab with right hand</b>. On a successful grab the body auto-pulls up so the
    ///     new grip settles to about shoulder height. A hold out of arm's reach can't be grabbed.
    ///   • <b>A/D</b> shift your centre of mass left/right (balance). <b>W/S</b> pull up / ease down.
    ///   • <b>R</b> reset to the start. Top out to win; lean off your support and you peel off and fall.
    /// </summary>
    public class PlayInputController : MonoBehaviour
    {
        [Header("Climber refs (wired by PlayBuild)")]
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;
        public Transform head;
        public Transform rig;
        public ClimbController controller;
        public BalanceSystem balance;
        public FootPlacementSystem feet;
        public Camera cam;
        public LayerMask holdLayer = ~0;

        [Header("Feel")]
        [Tooltip("Body-rise speed of the auto-pull after a grab (m/s).")]
        public float pullSpeed = 1.25f;
        [Tooltip("Most a single grab can haul you up (m).")]
        public float maxPullPerGrab = 1.3f;
        [Tooltip("Gap kept between the settled grip and the head (m) — leaves the grip ~shoulder height.")]
        public float settleGap = 0.28f;
        [Tooltip("Lateral lean speed for A/D (m/s) and the clamp on the manual nudge.")]
        public float leanSpeed = 0.9f;
        public float leanClamp = 0.30f;
        [Tooltip("Screen-space radius (px) within which the mouse picks up a hold.")]
        public float targetRadiusPx = 80f;

        enum Phase { Idle, Releasing, Grabbing }

        class Hand
        {
            public ClimbingHand h;
            public Phase phase = Phase.Idle;
            public ClimbHold pending;
            public int wait;
        }

        Hand _left, _right;
        Hand _active;            // hand that most recently grabbed → the one ClimbController is driving
        bool _pulling;
        float _pullTargetY;

        ClimbHold _target;       // hold currently under the mouse
        bool _targetReachable;

        float _lean;             // manual A/D lean offset (decays toward 0)
        float _flashT;
        string _flash = "";

        readonly List<ClimbHold> _holds = new List<ClimbHold>(64);
        readonly List<Vector3> _contacts = new List<Vector3>(8);
        Texture2D _px;
        GUIStyle _mid, _small, _rightLbl;

        void Awake()
        {
            if (rig == null) rig = transform;
            _left = new Hand { h = leftHand };
            _right = new Hand { h = rightHand };
        }

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerFell += OnFell;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerFell -= OnFell;
        }

        void OnFell()
        {
            // A peel-off reset everything on the wall — clear our hand state so we don't keep pulling.
            Flash("Fell!");
            ResetHands();
        }

        void ResetHands()
        {
            _pulling = false;
            _active = null;
            foreach (var hc in new[] { _left, _right })
            {
                if (hc?.h != null) hc.h.overrideGrip = false;
                if (hc != null) { hc.phase = Phase.Idle; hc.pending = null; }
            }
        }

        void Update()
        {
            if (cam == null || head == null) return;
            bool won = GameManager.Instance != null && GameManager.Instance.State == GameState.Summit;

            RefreshHolds();
            UpdateTarget();

            var mouse = Mouse.current;
            var kb = Keyboard.current;

            if (!won && mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) GrabWith(_left, _target);
                if (mouse.rightButton.wasPressedThisFrame) GrabWith(_right, _target);
            }

            TickHand(_left);
            TickHand(_right);

            // Manual pull / descend with W / S on the active gripping hand.
            if (!won && kb != null && _active != null && _active.h != null && _active.h.IsGripping)
            {
                var ht = _active.h.handTransform;
                if (kb.wKey.isPressed) { ht.position += Vector3.down * pullSpeed * Time.deltaTime; _pulling = false; }
                else if (kb.sKey.isPressed) ht.position += Vector3.up * (pullSpeed * 0.7f) * Time.deltaTime;
            }

            AutoPull();
            Balance(kb, won);

            if (kb != null && kb.rKey.wasPressedThisFrame && controller != null) { controller.Respawn(); ResetHands(); }

            if (_flashT > 0f) _flashT -= Time.deltaTime;
        }

        // ---- hold targeting ----

        void RefreshHolds()
        {
            _holds.Clear();
            // FindObjectsOfType skips inactive (broken) holds, which is exactly what we want.
            foreach (var h in Object.FindObjectsOfType<ClimbHold>())
                if (h != null && !h.IsBroken && h.role != ClimbHold.HoldRole.Foot) _holds.Add(h);
        }

        void UpdateTarget()
        {
            var mouse = Mouse.current;
            if (mouse == null) { _target = null; return; }
            Vector2 m = mouse.position.ReadValue();

            ClimbHold best = null;
            float bestPx = targetRadiusPx;
            foreach (var h in _holds)
            {
                Vector3 sp = cam.WorldToScreenPoint(h.GripPoint);
                if (sp.z <= 0f) continue;                       // behind the camera
                float d = Vector2.Distance(new Vector2(sp.x, sp.y), m);
                if (d < bestPx) { bestPx = d; best = h; }
            }
            _target = best;
            _targetReachable = best != null && Reachable(best);
        }

        bool Reachable(ClimbHold h)
        {
            float r = leftHand != null ? leftHand.armReach : 0f;
            if (r <= 0f) return true;                            // reach limit disabled
            float dl = leftHand != null ? (h.GripPoint - leftHand.ShoulderPosition).magnitude : 999f;
            float dr = rightHand != null ? (h.GripPoint - rightHand.ShoulderPosition).magnitude : 999f;
            return Mathf.Min(dl, dr) <= r * 1.02f;
        }

        // ---- grabbing ----

        void GrabWith(Hand hc, ClimbHold hold)
        {
            if (hc?.h == null || hold == null) return;
            hc.pending = hold;
            if (hc.h.IsGripping)
            {
                hc.h.overrideGrip = false;                        // let go first, then re-grab next frames
                hc.phase = Phase.Releasing; hc.wait = 2;
            }
            else
            {
                hc.h.handTransform.position = hold.GripPoint;
                hc.h.overrideGrip = true;
                hc.phase = Phase.Grabbing; hc.wait = 2;
            }
        }

        void TickHand(Hand hc)
        {
            if (hc == null || hc.phase == Phase.Idle) return;
            if (--hc.wait > 0) return;

            if (hc.phase == Phase.Releasing)
            {
                hc.h.handTransform.position = hc.pending.GripPoint;
                hc.h.overrideGrip = true;
                hc.phase = Phase.Grabbing; hc.wait = 2;
                return;
            }

            // Phase.Grabbing — check whether the grab actually latched (reach gate may have refused it).
            if (hc.h.IsGripping && hc.h.CurrentHold == hc.pending)
            {
                _active = hc;
                BeginAutoPull(hc.pending);
            }
            else
            {
                hc.h.overrideGrip = false;
                Flash("Out of reach!");
            }
            hc.phase = Phase.Idle; hc.pending = null;
        }

        // ---- auto-pull (counter-motion: move the gripping hand DOWN → rig rises) ----

        void BeginAutoPull(ClimbHold hold)
        {
            float rise = (hold.GripPoint.y - head.position.y) - settleGap;
            if (rise <= 0.02f) { _pulling = false; return; }
            _pullTargetY = rig.position.y + Mathf.Min(rise, maxPullPerGrab);
            _pulling = true;
        }

        void AutoPull()
        {
            if (!_pulling) return;
            if (_active == null || _active.h == null || !_active.h.IsGripping ||
                GameManager.Instance == null || GameManager.Instance.State != GameState.Climbing)
            { _pulling = false; return; }

            float remaining = _pullTargetY - rig.position.y;
            if (remaining <= 0.01f) { _pulling = false; return; }

            float ease = Mathf.Clamp01(remaining / 0.3f);         // decelerate into the settle point
            float step = pullSpeed * Mathf.Lerp(0.25f, 1f, ease) * Time.deltaTime;
            _active.h.handTransform.position += Vector3.down * step;
        }

        // ---- balance: auto-centre over the support span + manual A/D nudge ----

        void Balance(Keyboard kb, bool won)
        {
            if (head == null || rig == null) return;

            float input = 0f;
            if (!won && kb != null)
            {
                if (kb.aKey.isPressed) input -= 1f;
                if (kb.dKey.isPressed) input += 1f;
            }
            _lean = Mathf.Clamp(_lean + input * leanSpeed * Time.deltaTime, -leanClamp, leanClamp);
            if (input == 0f) _lean = Mathf.MoveTowards(_lean, 0f, leanSpeed * 0.6f * Time.deltaTime);

            // Support midpoint (gripped holds + planted feet), in rig-local lateral space.
            _contacts.Clear();
            if (leftHand != null && leftHand.IsGripping && leftHand.CurrentHold != null) _contacts.Add(leftHand.CurrentHold.GripPoint);
            if (rightHand != null && rightHand.IsGripping && rightHand.CurrentHold != null) _contacts.Add(rightHand.CurrentHold.GripPoint);
            if (feet != null) feet.CollectContacts(_contacts);

            float supportLocalX = head.localPosition.x;
            if (_contacts.Count > 0)
            {
                float sum = 0f;
                foreach (var c in _contacts) sum += Vector3.Dot(c - rig.position, rig.right);
                supportLocalX = sum / _contacts.Count;
            }

            float desired = Mathf.Clamp(supportLocalX + _lean, -0.47f, 0.47f);
            Vector3 lp = head.localPosition;
            lp.x = Mathf.MoveTowards(lp.x, desired, 1.6f * Time.deltaTime);
            head.localPosition = lp;
        }

        // ---- camera: third-person 3/4 view that follows the climber up ----

        void LateUpdate()
        {
            if (cam == null || head == null) return;
            // Centred, slightly-high head-on follow: track the head's world X and Y so the climber
            // stays in the middle at rest AND as they traverse; holds read straight-on (easy to click);
            // we see the back as they face the wall.
            float hx = head.position.x;
            Vector3 want = new Vector3(hx, head.position.y + 0.45f, 3.3f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-6f * Time.deltaTime));
            cam.transform.LookAt(new Vector3(hx, head.position.y - 0.1f, 0f));
        }

        void Flash(string s) { _flash = s; _flashT = 1.6f; }

        // ---- HUD (English labels — IMGUI's default font has no CJK glyphs) ----

        void OnGUI()
        {
            if (_px == null)
            {
                _px = new Texture2D(1, 1); _px.SetPixel(0, 0, Color.white); _px.Apply();
            }
            if (_mid == null)
            {
                _mid = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold };
                _small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
                _rightLbl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 16 };
            }

            // Balance bar.
            float bw = 240f, bh = 18f, x = 16f, y = 16f;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, bw + 4f, bh + 4f), _px);
            float n = balance != null ? Mathf.Clamp01(balance.Normalized) : 1f;
            GUI.color = (balance != null && balance.IsSlipping) ? new Color(0.92f, 0.27f, 0.2f) : new Color(0.3f, 0.85f, 0.42f);
            GUI.DrawTexture(new Rect(x, y, bw * n, bh), _px);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + bh + 2f, 200f, 20f), "BALANCE");

            var gm = GameManager.Instance;
            if (gm != null)
                GUI.Label(new Rect(Screen.width - 230f, 14f, 214f, 22f), $"Time {gm.ElapsedTime:0.0}s    Falls {gm.FallCount}", _rightLbl);

            // Mouse reticle on the targeted hold.
            if (_target != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(_target.GripPoint);
                if (sp.z > 0f)
                {
                    float gx = sp.x, gy = Screen.height - sp.y, r = 16f, t = 2f;
                    GUI.color = _targetReachable ? new Color(0.2f, 0.95f, 1f) : new Color(0.6f, 0.6f, 0.6f);
                    GUI.DrawTexture(new Rect(gx - r, gy - r, 2f * r, t), _px);             // top
                    GUI.DrawTexture(new Rect(gx - r, gy + r - t, 2f * r, t), _px);         // bottom
                    GUI.DrawTexture(new Rect(gx - r, gy - r, t, 2f * r), _px);             // left
                    GUI.DrawTexture(new Rect(gx + r - t, gy - r, t, 2f * r), _px);         // right
                    GUI.color = Color.white;
                    GUI.Label(new Rect(gx - 60f, gy + r + 2f, 120f, 18f), _targetReachable ? "L / R to grab" : "too far", _small);
                }
            }

            // Controls hint.
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(0f, Screen.height - 26f, Screen.width, 22f),
                "L-click: left hand   R-click: right hand   |   A/D: lean   W/S: pull up / down   |   R: reset", _small);
            GUI.color = Color.white;

            // Centre banners.
            if (gm != null && gm.State == GameState.Summit)
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 50f), $"SUMMIT!  {gm.ElapsedTime:0.0}s  —  press R to climb again", _mid);
            else if (_flashT > 0f)
            {
                GUI.color = _flash == "Out of reach!" ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.5f, 0.45f);
                GUI.Label(new Rect(0f, Screen.height * 0.42f, Screen.width, 46f), _flash, _mid);
                GUI.color = Color.white;
            }
        }
    }
}
