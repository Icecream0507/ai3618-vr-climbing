using UnityEngine;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Marks a GameObject as a climbable hold. A hold needs a Collider; grabbing is resolved by
    /// <see cref="ClimbingHand"/> via an overlap query on the Hold layer, so the collider does not
    /// have to be a trigger. Metadata drives gameplay: <see cref="role"/> says whether hands, feet,
    /// or either may use it; <see cref="type"/> gives a Finish/Fragile/Rest behaviour.
    ///
    /// Colour legend (see docs/DESIGN.md): yellow = hand, orange = foot, purple = either,
    /// green = finish, red = fragile, blue = rest.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClimbHold : MonoBehaviour
    {
        public enum HoldType { Normal, Finish, Fragile, Rest }
        public enum HoldRole { Hand, Foot, Either }

        [Tooltip("Gameplay category for this hold.")]
        public HoldType type = HoldType.Normal;

        [Tooltip("Whether this hold can be used by hands, feet, or either.")]
        public HoldRole role = HoldRole.Hand;

        [Tooltip("Stamina drained per second while this hold is gripped (ignored for Rest holds).")]
        public float staminaCostPerSecond = 5f;

        [Tooltip("For Fragile holds: seconds it can be held before it breaks.")]
        public float breakAfterSeconds = 1.5f;

        [Tooltip("World point a hand/foot anchors to. Defaults to this object's transform.")]
        public Transform gripAnchor;

        public Vector3 GripPoint => gripAnchor != null ? gripAnchor.position : transform.position;
        public bool IsBroken { get; private set; }

        // Raised when a hand starts / stops gripping this hold.
        public event System.Action<ClimbingHand> Grabbed;
        public event System.Action<ClimbingHand> Released;

        float _heldTime;
        int _gripperCount;

        public void NotifyGrabbed(ClimbingHand hand)
        {
            _heldTime = 0f;
            Grabbed?.Invoke(hand);
        }

        public void NotifyReleased(ClimbingHand hand) => Released?.Invoke(hand);

        public void IncrementGrippers() => _gripperCount++;
        public void DecrementGrippers() => _gripperCount = Mathf.Max(0, _gripperCount - 1);

        void Update()
        {
            // Fragile holds crumble if you hang on them too long.
            if (type != HoldType.Fragile || IsBroken) return;
            if (_gripperCount > 0)
            {
                _heldTime += Time.deltaTime;
                if (_heldTime >= breakAfterSeconds) Break();
            }
        }

        public void Break()
        {
            if (IsBroken) return;
            IsBroken = true;
            // TODO: play break VFX/SFX before disabling.
            gameObject.SetActive(false);
        }

        void OnDrawGizmos()
        {
            Gizmos.color =
                type == HoldType.Finish  ? Color.green :
                type == HoldType.Fragile ? Color.red :
                type == HoldType.Rest    ? Color.cyan :
                role == HoldRole.Foot    ? new Color(1f, 0.55f, 0f)    :   // orange
                role == HoldRole.Either  ? new Color(0.7f, 0.3f, 0.9f) :   // purple
                                           new Color(0.95f, 0.85f, 0.2f);  // yellow
            Gizmos.DrawWireSphere(GripPoint, 0.05f);
        }
    }
}
