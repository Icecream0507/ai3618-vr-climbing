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
        int _footCount;

        // --- contact highlight: a hold a hand is gripping or a foot is standing on lights up green,
        //     and reverts the instant it's let go. Purely cosmetic; never read by gameplay. ---
        [Tooltip("Light the hold up green while a hand grips it or a foot stands on it.")]
        public bool highlightOnContact = true;
        Renderer _renderer;
        Material _baseMat;
        bool _highlighted;
        static Material s_contactMat;
        static bool s_contactMatTried;

        static Material ContactMat
        {
            get
            {
                if (!s_contactMatTried)
                {
                    s_contactMatTried = true;
                    var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (sh != null)
                    {
                        var green = new Color(0.18f, 1f, 0.35f);
                        var m = new Material(sh) { name = "HoldContactGreen" };
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", green);
                        if (m.HasProperty("_Color")) m.SetColor("_Color", green);
                        m.EnableKeyword("_EMISSION");
                        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", green * 0.55f);
                        s_contactMat = m;
                    }
                }
                return s_contactMat;
            }
        }

        void RefreshHighlight()
        {
            if (!highlightOnContact) return;
            SetHighlighted(_gripperCount + _footCount > 0);
        }

        void SetHighlighted(bool on)
        {
            if (on == _highlighted) return;
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;
            if (on)
            {
                var mat = ContactMat;
                if (mat == null) return;            // no render pipeline (e.g. headless) — stay a no-op
                _baseMat = _renderer.sharedMaterial; // capture the painted resting colour the first time
                _renderer.sharedMaterial = mat;
            }
            else if (_baseMat != null)
            {
                _renderer.sharedMaterial = _baseMat;
            }
            _highlighted = on;
        }

        public void NotifyGrabbed(ClimbingHand hand)
        {
            _heldTime = 0f;
            Grabbed?.Invoke(hand);
        }

        public void NotifyReleased(ClimbingHand hand) => Released?.Invoke(hand);

        public void IncrementGrippers() { _gripperCount++; RefreshHighlight(); }
        public void DecrementGrippers() { _gripperCount = Mathf.Max(0, _gripperCount - 1); RefreshHighlight(); }

        // Foot contact (driven by FootPlacementSystem; feet don't grip, so they're counted separately).
        public void IncrementFeet() { _footCount++; RefreshHighlight(); }
        public void DecrementFeet() { _footCount = Mathf.Max(0, _footCount - 1); RefreshHighlight(); }

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
