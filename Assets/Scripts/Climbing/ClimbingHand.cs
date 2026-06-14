using UnityEngine;
using UnityEngine.InputSystem;

namespace VRClimb.Climbing
{
    /// <summary>
    /// One per controller/hand. Reads the grip input action and, on press, searches for the
    /// nearest <see cref="ClimbHold"/> within reach (physics overlap on the Hold layer). While
    /// gripping it exposes the data <see cref="ClimbController"/> needs to move the rig.
    ///
    /// Input is read through the Input System (an <see cref="InputActionProperty"/>) instead of a
    /// specific XRI interactor, so this script is independent of the installed XRI version. Wire
    /// the grip action to e.g. "XRI RightHand Interaction/Select Value" from the XRI Starter Assets.
    /// </summary>
    public class ClimbingHand : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Grip value action for this hand (e.g. XRI RightHand Interaction/Select Value).")]
        public InputActionProperty gripAction;
        [Range(0f, 1f)] public float gripThreshold = 0.6f;

        [Header("Reach")]
        [Tooltip("Transform used as the hand position (usually the controller / attach transform).")]
        public Transform handTransform;
        [Tooltip("Radius searched for a hold when grip is pressed.")]
        public float grabRadius = 0.12f;
        [Tooltip("Physics layer(s) climb holds live on.")]
        public LayerMask holdLayer = ~0;

        [Header("Arm-reach limit (climbing difficulty)")]
        [Tooltip("HMD/head transform. If set with armReach>0, a hold can only be grabbed when it is " +
                 "within arm's reach of the shoulder — you must move your body into range first.")]
        public Transform reachHead;
        [Tooltip("XR Origin (lateral axis). Defaults to reachHead's parent if left empty.")]
        public Transform reachRig;
        [Tooltip("Lateral shoulder offset along the rig's right axis (-=left hand, +=right hand).")]
        public float shoulderSide = 0f;
        [Tooltip("Max shoulder->hold distance allowed to grab (m). 0 disables the limit (e.g. for pure VR, where your real arm already limits reach).")]
        public float armReach = 0f;

        /// <summary>World shoulder position this hand reaches from (for the arm-reach limit).</summary>
        public Vector3 ShoulderPosition
        {
            get
            {
                if (reachHead == null) return HandPosition;
                Transform r = reachRig != null ? reachRig : reachHead;
                return reachHead.position - Vector3.up * BodyMetrics.ShoulderDrop + r.right * shoulderSide;
            }
        }

        [Header("Feedback")]
        [Tooltip("Optional: XR node used for a haptic pulse on grab.")]
        public UnityEngine.XR.XRNode hapticNode = UnityEngine.XR.XRNode.RightHand;
        public bool hapticOnGrab = true;

        public bool IsGripping { get; private set; }
        public ClimbHold CurrentHold { get; private set; }

        /// <summary>Test/sim hook: while true, grip counts as held regardless of controller input
        /// (used by the headless end-to-end simulation; never set from gameplay).</summary>
        [System.NonSerialized] public bool overrideGrip;

        /// <summary>World position of the hand this frame.</summary>
        public Vector3 HandPosition => handTransform != null ? handTransform.position : transform.position;

        public event System.Action<ClimbingHand> Grabbed;
        public event System.Action<ClimbingHand> Released;

        static readonly Collider[] s_Overlap = new Collider[8];

        void OnEnable()  => gripAction.action?.Enable();
        void OnDisable() => ReleaseInternal();

        void Update()
        {
            float grip = gripAction.action?.ReadValue<float>() ?? 0f;
            bool pressed = overrideGrip || grip >= gripThreshold;

            if (pressed && !IsGripping) TryGrab();
            else if (!pressed && IsGripping) ReleaseInternal();

            // Drop the hold if it broke / was disabled while held.
            if (IsGripping && (CurrentHold == null || CurrentHold.IsBroken)) ReleaseInternal();

            // Safety net: drop the hold only if the body has moved absurdly far from it (well beyond
            // any reachable pose) — normal climbing never trips this; the real limit is on *grabbing*.
            if (IsGripping && reachHead != null && armReach > 0f && CurrentHold != null)
            {
                float slack = armReach * 2.2f;
                if ((CurrentHold.GripPoint - ShoulderPosition).sqrMagnitude > slack * slack) ReleaseInternal();
            }
        }

        void TryGrab()
        {
            Vector3 p = HandPosition;
            int n = Physics.OverlapSphereNonAlloc(p, grabRadius, s_Overlap, holdLayer, QueryTriggerInteraction.Collide);

            bool limitReach = reachHead != null && armReach > 0f;
            Vector3 shoulder = ShoulderPosition;
            float maxReachSqr = armReach * armReach;

            ClimbHold best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var hold = s_Overlap[i].GetComponentInParent<ClimbHold>();
                if (hold == null || hold.IsBroken) continue;
                // Any hold takes a hand or a foot (like real rock) — role/colour is only a visual hint.
                // Out of arm's reach: your body isn't close enough yet — climb into range first.
                if (limitReach && (hold.GripPoint - shoulder).sqrMagnitude > maxReachSqr) continue;
                float d = (hold.GripPoint - p).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = hold; }
            }
            if (best == null) return;

            IsGripping = true;
            CurrentHold = best;
            best.IncrementGrippers();
            best.NotifyGrabbed(this);
            Grabbed?.Invoke(this);

            if (hapticOnGrab) Util.HapticFeedback.Pulse(hapticNode, 0.5f, 0.08f);
        }

        void ReleaseInternal()
        {
            if (!IsGripping) return;
            IsGripping = false;
            var hold = CurrentHold;
            CurrentHold = null;
            if (hold != null) { hold.DecrementGrippers(); hold.NotifyReleased(this); }
            Released?.Invoke(this);
        }

        /// <summary>Forced release, e.g. when stamina is exhausted.</summary>
        public void ForceRelease() => ReleaseInternal();

        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGripping ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(HandPosition, grabRadius);
        }
    }
}
