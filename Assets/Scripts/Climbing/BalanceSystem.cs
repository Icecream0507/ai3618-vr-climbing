using System.Collections.Generic;
using UnityEngine;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Lightweight bouldering balance model — the headline v1 mechanic. This is NOT a physics sim:
    /// the player's centre of mass is approximated by the HMD (head) position, and we test whether
    /// it is laterally supported by the current contact points (gripped hand-holds + auto-placed
    /// virtual feet). Leaning out past your support — e.g. reaching far on same-side holds with no
    /// foot below — drains a balance meter; adding a foot or an opposite-side hold re-centres you.
    /// At zero balance you "peel off" (<see cref="PeelOff"/>) and fall.
    ///
    /// Balance is only a threat while you are actually on the wall by at least one hand; otherwise
    /// (before the climb, or mid-fall) it quietly recovers, so the meter and PeelOff never churn.
    ///
    /// Grounded in: head-as-CoM + lean-over-unsupported-foot fall check (Mitsuda &amp; Kimura,
    /// Frontiers in VR 2026) and the climbing "barn-door" concept. All thresholds are tuning
    /// constants, not derived physics — tune them by feel. See docs/RESEARCH.md.
    /// </summary>
    public class BalanceSystem : MonoBehaviour
    {
        [Header("Refs")]
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;
        public FootPlacementSystem feet;
        [Tooltip("HMD / main camera — cheap centre-of-mass proxy. Auto-finds Camera.main if empty.")]
        public Transform head;
        [Tooltip("XR Origin — its right axis defines the wall's lateral direction. Defaults to this transform.")]
        public Transform rig;

        [Header("Tuning (empirical)")]
        [Tooltip("Lateral slack (m) the CoM may sit outside the contact span before counting as unstable.")]
        public float supportMargin = 0.03f;
        [Tooltip("Lateral distance (m) past the support edge that counts as fully unstable.")]
        public float maxOvershoot = 0.30f;
        public float drainPerSecond = 0.9f;
        public float regenPerSecond = 0.7f;
        [Tooltip("Seconds of instability tolerated before the meter starts draining (anti-jitter).")]
        public float graceTime = 0.22f;

        public float Balance { get; private set; } = 1f;
        public float Normalized => Balance;
        public bool IsSlipping { get; private set; }

        /// <summary>Raised when balance hits zero and the climber peels off the wall.</summary>
        public event System.Action PeelOff;

        static readonly List<Vector3> s_Contacts = new List<Vector3>(8);
        float _unstableTimer;

        /// <summary>Restore full balance (e.g. after a respawn) so you don't immediately re-slip.</summary>
        public void ResetBalance()
        {
            Balance = 1f;
            _unstableTimer = 0f;
            IsSlipping = false;
        }

        void Awake()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (rig == null) rig = transform;
        }

        void Update()
        {
            // Balance only matters while you are actually on the wall by at least one hand. Before
            // the climb starts, or while falling, recover quietly so the meter / PeelOff don't churn.
            bool engaged = (leftHand != null && leftHand.IsGripping) ||
                           (rightHand != null && rightHand.IsGripping);
            if (!engaged)
            {
                IsSlipping = false;
                _unstableTimer = 0f;
                Balance = Mathf.Min(1f, Balance + regenPerSecond * Time.deltaTime);
                return;
            }

            float stability = ComputeStability();   // +1..0 supported, 0..-1 leaning out

            if (stability < 0f)
            {
                _unstableTimer += Time.deltaTime;
                IsSlipping = _unstableTimer >= graceTime;
                if (IsSlipping) Balance += stability * drainPerSecond * Time.deltaTime;  // stability<0 -> drain
            }
            else
            {
                _unstableTimer = 0f;
                IsSlipping = false;
                Balance += stability * regenPerSecond * Time.deltaTime;
            }

            Balance = Mathf.Clamp01(Balance);

            if (Balance <= 0f)
            {
                PeelOff?.Invoke();
                Balance = 0.4f;          // reset so we don't re-fire every frame during the fall
                _unstableTimer = 0f;
                IsSlipping = false;
            }
        }

        /// <summary>
        /// Signed stability: &gt;0 (up to 1) when the CoM sits comfortably within the lateral span of
        /// the contact points; &lt;0 (down to -1) the further it leans outside that span.
        /// </summary>
        float ComputeStability()
        {
            s_Contacts.Clear();
            if (leftHand != null && leftHand.IsGripping && leftHand.CurrentHold != null)
                s_Contacts.Add(leftHand.CurrentHold.GripPoint);
            if (rightHand != null && rightHand.IsGripping && rightHand.CurrentHold != null)
                s_Contacts.Add(rightHand.CurrentHold.GripPoint);
            if (feet != null) feet.CollectContacts(s_Contacts);

            Vector3 axis = rig != null ? rig.right : Vector3.right;
            Vector3 origin = head != null ? head.position : transform.position;
            return ClimbMath.StabilityScore(origin, axis, s_Contacts, supportMargin, maxOvershoot);
        }
    }
}
