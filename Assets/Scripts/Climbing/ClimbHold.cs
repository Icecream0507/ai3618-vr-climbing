using UnityEngine;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Marks a GameObject as a climbable hold. A hold needs a Collider; grabbing is resolved by
    /// <see cref="ClimbingHand"/> via an overlap query on the Hold layer, so the collider does not
    /// have to be a trigger. Optional metadata drives gameplay: a Finish hold ends the run, a
    /// Fragile hold breaks after being held too long, a Rest hold pauses stamina drain.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClimbHold : MonoBehaviour
    {
        public enum HoldType { Normal, Finish, Fragile, Rest }

        [Tooltip("Gameplay category for this hold.")]
        public HoldType type = HoldType.Normal;

        [Tooltip("Stamina drained per second while this hold is gripped (ignored for Rest holds).")]
        public float staminaCostPerSecond = 5f;

        [Tooltip("For Fragile holds: seconds it can be held before it breaks.")]
        public float breakAfterSeconds = 1.5f;

        [Tooltip("World point a hand anchors to. Defaults to this object's transform.")]
        public Transform gripAnchor;

        public Vector3 GripPoint => gripAnchor != null ? gripAnchor.position : transform.position;
        public bool IsBroken { get; private set; }

        // Raised when a hand starts / stops gripping this hold.
        public event System.Action<ClimbingHand> Grabbed;
        public event System.Action<ClimbingHand> Released;

        float _heldTime;

        public void NotifyGrabbed(ClimbingHand hand)
        {
            _heldTime = 0f;
            Grabbed?.Invoke(hand);
        }

        public void NotifyReleased(ClimbingHand hand) => Released?.Invoke(hand);

        void Update()
        {
            // Fragile holds crumble if you hang on them too long.
            if (type != HoldType.Fragile || IsBroken) return;
            // _heldTime only advances while a hand reports this as its current hold.
            if (HasGripper()) { _heldTime += Time.deltaTime; if (_heldTime >= breakAfterSeconds) Break(); }
        }

        bool HasGripper()
        {
            // Cheap check: the hands set themselves as listeners only while gripping.
            // Kept simple here; a production version would track grippers in a list.
            return Grabbed != null && _heldTime >= 0f && _gripperCount > 0;
        }

        int _gripperCount;
        public void IncrementGrippers() => _gripperCount++;
        public void DecrementGrippers() => _gripperCount = Mathf.Max(0, _gripperCount - 1);

        public void Break()
        {
            if (IsBroken) return;
            IsBroken = true;
            // TODO: play break VFX/SFX before disabling.
            gameObject.SetActive(false);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = type switch
            {
                HoldType.Finish  => Color.green,
                HoldType.Fragile => Color.red,
                HoldType.Rest    => Color.cyan,
                _                => Color.yellow
            };
            Gizmos.DrawWireSphere(GripPoint, 0.05f);
        }
    }
}
