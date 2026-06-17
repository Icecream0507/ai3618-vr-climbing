using System.Collections.Generic;
using UnityEngine;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Abstracted footwork for 3-point VR (head + 2 controllers, no foot tracking on Quest).
    ///
    /// Two "virtual feet" are auto-placed on the nearest Foot/Either holds in a stance zone below
    /// the body (estimated from the HMD). A foot stays planted until its hold leaves reach as you
    /// climb past it, then re-snaps to a lower one (foot-gluing). Feet are a state derived from
    /// holds — NOT the player's real feet or an IK leg — visualised with simple markers. Their
    /// positions feed <see cref="BalanceSystem"/>, so placing feet widens the support base.
    ///
    /// Design follows the evidence-based recommendation to keep feet abstracted rather than use IK
    /// legs or Meta Generative Legs (which break in climbing poses). See docs/RESEARCH.md.
    /// </summary>
    public class FootPlacementSystem : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("HMD / main camera. Auto-finds Camera.main if left empty.")]
        public Transform head;
        [Tooltip("XR Origin — defines the right (lateral) axis. Defaults to this transform.")]
        public Transform rig;
        [Tooltip("Optional visual markers shown on the held foot-holds (e.g. small discs/shoes).")]
        public Transform leftFootMarker;
        public Transform rightFootMarker;

        [Header("Tuning")]
        [Tooltip("Approx vertical distance from the head down to the feet zone (m).")]
        public float bodyDrop = 1.15f;
        [Tooltip("How far left/right of centre each foot looks for a hold (m).")]
        public float stanceHalfWidth = 0.22f;
        [Tooltip("Radius a foot searches for / keeps a hold (m). Smaller = feet only stick to holds " +
                 "right below you, so support isn't always free (stops the 'float up forever' feel).")]
        public float footReach = 0.28f;
        [Tooltip("Physics layer(s) climb holds live on.")]
        public LayerMask holdLayer = ~0;

        struct Foot { public bool planted; public ClimbHold hold; }
        Foot _left, _right;

        public int PlantedCount => (_left.planted ? 1 : 0) + (_right.planted ? 1 : 0);

        static readonly Collider[] s_Overlap = new Collider[8];

        void Awake()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (rig == null) rig = transform;
        }

        void Update()
        {
            Vector3 basePos = (head != null ? head.position : transform.position) - Vector3.up * bodyDrop;
            Vector3 right = (rig != null ? rig.right : Vector3.right).normalized;

            _left  = Solve(basePos - right * stanceHalfWidth, _left,  leftFootMarker);
            _right = Solve(basePos + right * stanceHalfWidth, _right, rightFootMarker);
        }

        Foot Solve(Vector3 restPos, Foot prev, Transform marker)
        {
            ClimbHold chosen = null;

            // Foot-gluing: keep the previous hold while it is still in reach.
            if (prev.planted && prev.hold != null && !prev.hold.IsBroken &&
                (prev.hold.GripPoint - restPos).sqrMagnitude <= footReach * footReach)
            {
                chosen = prev.hold;
            }
            else
            {
                int nb = Physics.OverlapSphereNonAlloc(restPos, footReach, s_Overlap, holdLayer, QueryTriggerInteraction.Collide);
                float best = float.MaxValue;
                for (int i = 0; i < nb; i++)
                {
                    var hold = s_Overlap[i].GetComponentInParent<ClimbHold>();
                    if (hold == null || hold.IsBroken) continue;
                    // Any hold is foot-able (like real rock) — role/colour is only a visual hint.
                    float d = (hold.GripPoint - restPos).sqrMagnitude;
                    if (d < best) { best = d; chosen = hold; }
                }
            }

            // Update the green contact highlight when this foot steps onto a different hold (or off).
            if (prev.hold != chosen)
            {
                if (prev.planted && prev.hold != null) prev.hold.DecrementFeet();
                if (chosen != null) chosen.IncrementFeet();
            }

            var foot = new Foot { planted = chosen != null, hold = chosen };
            if (marker != null)
            {
                marker.gameObject.SetActive(foot.planted);
                if (foot.planted) marker.position = chosen.GripPoint;
            }
            return foot;
        }

        /// <summary>Adds the planted feet's contact points for the balance computation.</summary>
        public void CollectContacts(List<Vector3> into)
        {
            if (_left.planted && _left.hold != null)  into.Add(_left.hold.GripPoint);
            if (_right.planted && _right.hold != null) into.Add(_right.hold.GripPoint);
        }

        /// <summary>Drop both feet (e.g. on a peel-off / respawn).</summary>
        public void DropAll()
        {
            if (_left.planted  && _left.hold  != null) _left.hold.DecrementFeet();
            if (_right.planted && _right.hold != null) _right.hold.DecrementFeet();
            _left = default; _right = default;
            if (leftFootMarker != null)  leftFootMarker.gameObject.SetActive(false);
            if (rightFootMarker != null) rightFootMarker.gameObject.SetActive(false);
        }
    }
}
