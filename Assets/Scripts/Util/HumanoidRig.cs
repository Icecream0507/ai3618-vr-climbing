using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Util
{
    /// <summary>
    /// Articulated climber for the recorded demo's third-person view (NOT used in real VR play — there
    /// you are first-person and only see your own hands). It is a lightweight, kinematic approximation
    /// of climbing physics so the body looks like a real person on the wall rather than a stiff
    /// mannequin.
    ///
    /// Model (this is the key to realism — the body hangs from the HANDS, not from a fixed upright head):
    ///  • The <b>pelvis</b> is a damped gravity pendulum. Its rest pose is support-aware: standing over
    ///    planted feet (more upright) or dangling below the gripping hand(s) and swinging (saggier).
    ///  • The <b>spine</b> runs pelvis → chest → head and TILTS toward whatever is bearing load: it aims
    ///    from the pelvis up toward the grip/feet support, so a one-arm hang leans the whole torso (and
    ///    head) under that arm — the diagonal body tension of real climbing. The spine also curves
    ///    slightly (chest leads, head trails), never a rigid pole.
    ///  • The <b>head</b> rides the top of the spine and FACES THE WALL almost all the time (a climber
    ///    reads the holds in front of/above them); it only GLANCES partway toward the next hold or the
    ///    feet, clamped to a neck range-of-motion cone so it never swivels away from the wall, and is
    ///    rate-damped so it eases rather than snaps. Never bolt upright — it tilts gently with the body.
    ///  • Arms/legs are 2-bone IK with joint limits (no hyperextension); legs dangle under gravity when a
    ///    foot is off a hold.
    ///
    /// Purely cosmetic: it reads the head/hand/foot transforms gameplay drives and never feeds back.
    /// </summary>
    public class HumanoidRig : MonoBehaviour
    {
        public Transform head;          // HMD proxy / CoM the sim leans (reference only — NOT the visible head)
        public Transform leftHand;      // controller proxies the sim moves onto holds
        public Transform rightHand;
        public Transform leftFoot;      // foot markers FootPlacementSystem positions on planted holds
        public Transform rightFoot;
        public Transform rig;           // lateral / forward axes
        public ClimbingHand leftHandC;  // to read which hand is bearing load
        public ClimbingHand rightHandC;

        [Header("Gravity feel")]
        public float gravity = 8f;           // m/s^2 bias pulling the hips down (scaled, not real 9.8)
        public float standStiffness = 65f;   // spring toward rest when feet support you (stiffer)
        public float hangStiffness = 22f;    // spring when hanging from hands only (saggier, swingier)
        public float damping = 6.5f;

        [Header("Spine tilt")]
        [Range(0f, 1f)] public float hangLean = 0.70f;   // how hard the torso leans under the loaded arm when hanging
        [Range(0f, 1f)] public float standLean = 0.30f;  // torso lean when standing on feet (more upright)
        public float spineCurve = 0.14f;                 // chest leads the head a touch (curved back, not a pole)
        public float headTrack = 8f;                     // how fast the head turns to look toward the next reach (damped neck — rotational rate limit)
        [Range(0f, 0.8f)] public float headLean = 0.26f; // how far the neck bends off the spine toward the gaze (kept modest so the head tucks onto the spine, never strands off-axis)
        [Range(0f, 90f)] public float lookCone = 48f;    // neck range-of-motion: the head never turns more than this off facing the wall (so it stays wall-facing)
        [Range(0f, 1f)] public float glanceWeight = 0.5f;// how far the head turns from wall-facing toward the active hold; the rest of the time it faces the wall
        public float legSwing = 0.06f;                   // dangling legs trail the hip swing (pendulum secondary motion)
        public float hipTwist = 16f;                     // deg of spinal torsion: reaching-side hip turns into the wall
        public float swingImpulse = 0.7f;                // m/s sideways kick when a hand releases (body swings under the other)

        // anatomical lengths
        const float TorsoLen = BodyMetrics.HipDrop - BodyMetrics.ShoulderDrop; // pelvis -> shoulders (~0.58)
        const float NeckLen  = 0.21f;                                          // shoulders -> head centre along spine

        Transform _torso, _pelvis, _neck;          // trunk: one rounded torso (tank top) + shorts/pelvis + neck
        Transform _headPivot, _headBall, _hair;    // oriented head (skull + hair cap so the facing reads)
        Transform _luA, _lfA, _ruA, _rfA;          // arm capsules (bare skin — sleeveless)
        Transform _luL, _llL, _ruL, _rlL;          // leg capsules (shorts thigh + bare shin)
        Transform _lHand, _rHand, _lShoe, _rShoe;  // gripping fists + climbing-shoe feet (so hands/feet aren't bare balls)
        Material _skin, _jacket, _pants;

        Vector3 _hip, _hipVel;
        Quaternion _headRot = Quaternion.identity;
        float _reach;   // smoothed reaching side: +1 right reaching, -1 left reaching
        int _prevGrips;
        bool _init;

        void Start()
        {
            _skin   = Mat(new Color(0.95f, 0.78f, 0.60f));
            _jacket = Mat(new Color(0.12f, 0.55f, 0.62f));
            _pants  = Mat(new Color(0.18f, 0.22f, 0.32f));

            // Trunk: ONE rounded torso (tank top) + shorts + a short neck — no separate waist/chest/belly
            // blocks (the old stack read as "too complex"; the Klifur reference is a single clean torso).
            _torso = Limb(_jacket); _pelvis = Limb(_pants); _neck = Limb(_skin);

            // Limbs are smooth bare-skin tubes (sleeveless top, shorts). The rounded capsule caps form the
            // shoulder/elbow/hip/knee joints directly, so there are NO separate joint balls cluttering it.
            _luA = Limb(_skin);  _lfA = Limb(_skin);  _ruA = Limb(_skin);  _rfA = Limb(_skin);   // bare arms
            _luL = Limb(_pants); _llL = Limb(_skin);  _ruL = Limb(_pants); _rlL = Limb(_skin);   // shorts thigh + bare shin

            // Hands & feet: gripping fists (skin) and climbing shoes (dark rubber) so the contact points
            // read as a person's hands/feet, not bare marker balls. Oriented each frame in LateUpdate.
            var rubber = Mat(new Color(0.11f, 0.11f, 0.13f));
            _lHand = Ball(_skin, 0.12f); _lHand.SetParent(transform, true);
            _rHand = Ball(_skin, 0.12f); _rHand.SetParent(transform, true);
            _lShoe = Ball(rubber, 0.12f); _lShoe.SetParent(transform, true);
            _rShoe = Ball(rubber, 0.12f); _rShoe.SetParent(transform, true);

            // Head: a clean rounded skull + a hair cap (an unmistakable front/back so the facing reads) and
            // two small eyes. No nose — the Klifur reference head is a smooth, simple shape, not a face study.
            var hairMat = Mat(new Color(0.20f, 0.14f, 0.10f));
            _headPivot = new GameObject("HeadPivot").transform; _headPivot.SetParent(transform, true);
            _headBall = Ball(_skin, 0.15f); _headBall.SetParent(_headPivot, false);
            _headBall.localScale = new Vector3(0.16f, 0.178f, 0.165f);      // clean rounded skull (barely egg)
            _hair = Ball(hairMat, 0.1f); _hair.SetParent(_headPivot, false);
            _hair.localPosition = new Vector3(0f, 0.056f, -0.028f);         // a smooth cap high/back of centre
            _hair.localScale = new Vector3(0.172f, 0.118f, 0.184f);        // so it never bands across the face
            // Eyes: two small dark ovals on the face (+z) so the head still reads as a person looking at the
            // wall (kept subtle — the reference head has no facial detail at all).
            var eyeMat = Mat(new Color(0.09f, 0.08f, 0.11f));
            var le = Ball(eyeMat, 0.04f); le.SetParent(_headPivot, false);
            le.localPosition = new Vector3(-0.043f, 0.012f, 0.073f); le.localScale = new Vector3(0.033f, 0.043f, 0.03f);
            var re = Ball(eyeMat, 0.04f); re.SetParent(_headPivot, false);
            re.localPosition = new Vector3( 0.043f, 0.012f, 0.073f); re.localScale = new Vector3(0.033f, 0.043f, 0.03f);
        }

        void LateUpdate()
        {
            if (head == null || rig == null) return;
            Vector3 right = rig.right, up = Vector3.up, fwd = Vector3.forward;  // +z = away from wall
            float dt = Mathf.Clamp(Time.deltaTime, 1e-4f, 0.05f);

            // --- gather what is actually holding the body up ---
            Vector3 gripSum = Vector3.zero; int grips = 0;
            if (leftHandC != null && leftHandC.IsGripping)  { gripSum += leftHand.position;  grips++; }
            if (rightHandC != null && rightHandC.IsGripping) { gripSum += rightHand.position; grips++; }
            Vector3 footSum = Vector3.zero; int feetN = 0;
            if (leftFoot != null && leftFoot.gameObject.activeInHierarchy)  { footSum += leftFoot.position;  feetN++; }
            if (rightFoot != null && rightFoot.gameObject.activeInHierarchy) { footSum += rightFoot.position; feetN++; }
            Vector3 gripC = grips > 0 ? gripSum / grips : Vector3.zero;
            Vector3 footC = feetN > 0 ? footSum / feetN : Vector3.zero;

            // --- pelvis pendulum: support-aware rest pose (this is what creates the realistic hang) ---
            Vector3 hipRest;
            float stiffness, lean;
            if (feetN > 0)
            {
                float x = Mathf.Lerp(head.position.x, footC.x, 0.55f);       // sit over your feet
                if (grips > 0) x = Mathf.Lerp(x, gripC.x, 0.25f);            // bias toward the pulling arm
                float z = Mathf.Min(head.position.z, footC.z + 0.22f);
                hipRest = new Vector3(x, head.position.y - BodyMetrics.HipDrop, z);
                stiffness = standStiffness; lean = standLean;
            }
            else if (grips > 0)
            {
                // hang below the hand(s), pulled toward the wall (hips stay close, like real climbing)
                float z = Mathf.Min(head.position.z, gripC.z + 0.13f);
                hipRest = new Vector3(gripC.x, gripC.y - (BodyMetrics.HipDrop + 0.10f), z);
                stiffness = hangStiffness; lean = hangLean;
            }
            else
            {
                hipRest = head.position - up * BodyMetrics.HipDrop;          // airborne / falling
                stiffness = hangStiffness; lean = hangLean;
            }

            if (!_init) { _hip = hipRest; _init = true; _prevGrips = grips; }

            // Momentum: when a hand releases (grip count drops) the body swings under the remaining
            // grip — the emergent pendulum behaviour real climbers (and Klifur) get for free.
            if (grips > 0 && grips < _prevGrips)
            {
                Vector3 toGrip = gripC - _hip; toGrip.y = 0f;
                if (toGrip.sqrMagnitude > 1e-4f) _hipVel += toGrip.normalized * swingImpulse;
            }
            _prevGrips = grips;

            Vector3 accel = (hipRest - _hip) * stiffness - _hipVel * damping + Vector3.down * gravity;
            _hipVel += accel * dt;
            _hip += _hipVel * dt;

            // --- spine: aim from pelvis toward the support so the torso TILTS under the load ---
            Vector3 support = grips > 0 ? gripC : (feetN > 0 ? head.position : head.position);
            Vector3 aim = (support - _hip);
            Vector3 spineDir = aim.sqrMagnitude > 1e-4f
                ? Vector3.Slerp(up, aim.normalized, lean).normalized : up;

            Vector3 hpC = _hip;
            Vector3 chest = hpC + spineDir * TorsoLen;                       // shoulders / chest top
            Vector3 wallInto = -fwd;                                         // toward the wall (used by gaze & toe)
            Vector3 shC = chest;

            // Spinal torsion (blading) — CONSTRAINED so the body stays fundamentally square to the wall
            // (正向性). Only a genuine ONE-ARM load blades the torso; on two hands or in the air the body
            // relaxes back to square-with-the-wall. The twist is rate-limited so it eases rather than snaps
            // (a damped rotational constraint, not a free spin).
            bool lg2 = leftHandC != null && leftHandC.IsGripping;
            bool rg2 = rightHandC != null && rightHandC.IsGripping;
            float reachTarget = (lg2 && !rg2) ? 1f : (rg2 && !lg2) ? -1f : 0f; // square (0) unless one-arm hanging
            _reach = Mathf.MoveTowards(_reach, reachTarget, 1.8f * dt);
            Vector3 hipR = Quaternion.AngleAxis(hipTwist * _reach, spineDir) * right;
            Vector3 shR  = Quaternion.AngleAxis(-hipTwist * 0.5f * _reach, spineDir) * right;
            Vector3 lSh = shC - shR * BodyMetrics.ShoulderHalf, rSh = shC + shR * BodyMetrics.ShoulderHalf;
            Vector3 lHp = hpC - hipR * BodyMetrics.HipHalf,      rHp = hpC + hipR * BodyMetrics.HipHalf;

            // Trunk: ONE clean rounded torso (tank top) running hips→shoulders + the shorts across the
            // hips. The capsule's rounded caps give rounded shoulders & hips, so the body reads as a single
            // smooth shape (like the Klifur figure) instead of a stack of blocks — and needs no joint balls.
            PlaceCapsule(_torso, hpC, shC, 0.33f);
            PlaceCapsule(_pelvis, lHp, rHp, 0.27f);

            // --- head: a climber FACES THE WALL almost all the time (they read the holds in front of and
            // above them) and only GLANCES toward the next hold or their feet. So the gaze is anchored INTO
            // the wall (a touch up the route) and turned only PARTWAY toward the point of interest, then
            // hard-clamped to a neck range-of-motion cone — it can never swivel away from the wall. ---
            Vector3 headC0 = shC + spineDir * NeckLen;                       // where the head would sit if rigid
            Vector3 freeHandTarget = PickFreeHand(grips, gripC);
            // On an overhead match (both hands stacked high & close) glance DOWN to scan footwork instead of
            // burying the face straight up between the two symmetric forearms — what a climber actually does.
            bool handsClose = (leftHand.position - rightHand.position).sqrMagnitude < 0.1225f; // ~0.35 m apart
            if (grips >= 1 && handsClose && freeHandTarget.y > headC0.y + 0.05f)
                freeHandTarget = feetN > 0 ? footC : _hip + Vector3.down * 0.4f;

            // Rest gaze = into the wall, slightly up the route (where the next holds are). Turn only
            // `glanceWeight` of the way toward the active hold, then clamp within `lookCone` of the wall —
            // a neck-like range-of-motion limit that keeps the head wall-facing and the body's orientation honest.
            Vector3 wallGaze = (wallInto + up * 0.15f).normalized;
            Vector3 toPoi = freeHandTarget - headC0;
            Vector3 desired = toPoi.sqrMagnitude > 1e-4f
                ? Vector3.Slerp(wallGaze, toPoi.normalized, glanceWeight).normalized
                : wallGaze;
            Vector3 gaze = Vector3.RotateTowards(wallGaze, desired, lookCone * Mathf.Deg2Rad, 0f).normalized;

            Vector3 headUp = Vector3.Slerp(up, spineDir, 0.35f).normalized;  // head stays near-upright, tipped slightly with the spine
            Vector3 neckDir = Vector3.Slerp(spineDir, gaze, headLean).normalized;  // neck arcs gently toward the look
            Vector3 headC = shC + neckDir * NeckLen;                         // head offset off straight-up -> visible tilt
            Quaternion targetRot = Quaternion.LookRotation(gaze, headUp);
            // Rotational constraint: ease toward the target (a damped neck with a capped rate), never snap.
            _headRot = _init ? Quaternion.Slerp(_headRot, targetRot, Mathf.Clamp01(headTrack * dt)) : targetRot;
            _headPivot.SetPositionAndRotation(headC, _headRot);
            // Neck: a substantial column running the FULL distance shoulders→skull (overlapping into the
            // head base) so the head is always visibly seated on the body — never a blob on a thin stalk,
            // even at a full topout reach seen from below.
            PlaceCapsule(_neck, shC - spineDir * 0.03f, headC, 0.095f);

            // Arms: elbows bend down/out/away-from-wall; IK to wherever the sim put each hand.
            Vector3 armPoleL = (-right * 0.5f - up * 0.7f + fwd * 0.4f).normalized;
            Vector3 armPoleR = ( right * 0.5f - up * 0.7f + fwd * 0.4f).normalized;
            Vector3 lHandP, rHandP;
            Vector3 lEl = SolveLimb(lSh, leftHand.position,  BodyMetrics.UpperArm, BodyMetrics.ForeArm, armPoleL, _luA, _lfA, 0.10f, out lHandP);
            Vector3 rEl = SolveLimb(rSh, rightHand.position, BodyMetrics.UpperArm, BodyMetrics.ForeArm, armPoleR, _ruA, _rfA, 0.10f, out rHandP);

            // Fists: a compact flattened hand at each wrist, elongated along the forearm. Placed at the IK
            // end-effector (the clamped forearm tip), so the fist stays attached even when the hold is out
            // of reach — the climber visibly stretches short instead of the hand floating off.
            PlaceFist(_lHand, lHandP, lHandP - lEl, up);
            PlaceFist(_rHand, rHandP, rHandP - rEl, up);

            // Legs: knees bend out/away-from-wall (stem/frog). A dangling foot keeps a slight bend (target
            // pulled up & forward of straight-down) so it never reads as a stiff stick under gravity.
            Vector3 legPoleL = (-right * 0.55f + fwd * 0.8f - up * 0.05f).normalized;
            Vector3 legPoleR = ( right * 0.55f + fwd * 0.8f - up * 0.05f).normalized;
            // dangling legs trail the hip's horizontal swing (pendulum secondary motion brings them to life)
            Vector3 swing = right * Mathf.Clamp(_hipVel.x * legSwing, -0.22f, 0.22f);
            Vector3 lFootT = (leftFoot != null && leftFoot.gameObject.activeInHierarchy)
                ? leftFoot.position : lHp + (-right * 0.16f - up * (BodyMetrics.LegReach * 0.72f) + fwd * 0.20f) + swing;
            Vector3 rFootT = (rightFoot != null && rightFoot.gameObject.activeInHierarchy)
                ? rightFoot.position : rHp + ( right * 0.16f - up * (BodyMetrics.LegReach * 0.72f) + fwd * 0.20f) + swing;
            Vector3 lFootP, rFootP;
            SolveLimb(lHp, lFootT, BodyMetrics.Thigh, BodyMetrics.Shin, legPoleL, _luL, _llL, 0.12f, out lFootP);
            SolveLimb(rHp, rFootT, BodyMetrics.Thigh, BodyMetrics.Shin, legPoleR, _ruL, _rlL, 0.12f, out rFootP);

            // Shoes: an elongated climbing shoe at each foot, toe pointing into the wall (and a touch down).
            // Placed at the IK end-effector so the shoe never detaches from the shin.
            Vector3 toe = (wallInto - up * 0.18f).normalized;
            PlaceShoe(_lShoe, lFootP, toe, up);
            PlaceShoe(_rShoe, rFootP, toe, up);
        }

        // The hand most likely reaching for the next hold (the non-gripping one), so the head looks at it.
        Vector3 PickFreeHand(int grips, Vector3 gripC)
        {
            bool lg = leftHandC != null && leftHandC.IsGripping;
            bool rg = rightHandC != null && rightHandC.IsGripping;
            if (lg && !rg) return rightHand.position;
            if (rg && !lg) return leftHand.position;
            // both or neither gripping: look at the higher hand
            return leftHand.position.y > rightHand.position.y ? leftHand.position : rightHand.position;
        }

        // Analytic 2-bone IK with joint limits: clamp the target into reach so the joint is always real
        // (no hyperextension), then place the joint on the pole side so the bend is anatomical.
        Vector3 SolveLimb(Vector3 root, Vector3 target, float l1, float l2, Vector3 pole,
                          Transform seg1, Transform seg2, float thick, out Vector3 endEffector)
        {
            Vector3 toT = target - root;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, (l1 + l2) - 1e-3f);
            Vector3 dir = toT.sqrMagnitude > 1e-6f ? toT.normalized : Vector3.down;
            target = root + dir * d;   // taut limb if the hold was beyond reach -> visibly can't reach
            endEffector = target;      // the REAL hand/foot tip (clamped) — fist/shoe must sit here, not on
                                       // the unreachable raw target, or it detaches and floats off the limb

            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 polePerp = pole - Vector3.Dot(pole, dir) * dir;
            if (polePerp.sqrMagnitude < 1e-6f) polePerp = Vector3.Cross(dir, Vector3.right);
            polePerp.Normalize();
            Vector3 joint = root + dir * a + polePerp * h;

            PlaceCapsule(seg1, root, joint, thick);
            PlaceCapsule(seg2, joint, target, thick * 0.9f);
            return joint;   // elbow / knee, so a joint ball can sit there
        }

        // A fist at the wrist, elongated along the forearm and slightly flattened (knuckles read).
        static void PlaceFist(Transform t, Vector3 wrist, Vector3 foreArmDir, Vector3 up)
        {
            Quaternion rot = foreArmDir.sqrMagnitude > 1e-5f
                ? Quaternion.LookRotation(foreArmDir.normalized, up) : Quaternion.identity;
            // push the fist a touch past the wrist so it caps the forearm, not sinks into it
            Vector3 c = wrist + (foreArmDir.sqrMagnitude > 1e-5f ? foreArmDir.normalized : Vector3.zero) * 0.03f;
            t.SetPositionAndRotation(c, rot);
            t.localScale = new Vector3(0.115f, 0.10f, 0.14f);   // across / thin / along-forearm
        }

        // A climbing shoe at the foot, long axis along the toe direction (into the wall, slightly down).
        static void PlaceShoe(Transform t, Vector3 foot, Vector3 toeDir, Vector3 up)
        {
            Quaternion rot = toeDir.sqrMagnitude > 1e-5f
                ? Quaternion.LookRotation(toeDir.normalized, up) : Quaternion.identity;
            Vector3 c = foot + toeDir.normalized * 0.05f;       // shoe sits forward of the ankle
            t.SetPositionAndRotation(c, rot);
            t.localScale = new Vector3(0.105f, 0.085f, 0.205f); // width / height / length toward wall
        }

        static void PlaceCapsule(Transform t, Vector3 a, Vector3 b, float thick)
        {
            Vector3 mid = (a + b) * 0.5f;
            Vector3 axis = b - a;
            float len = Mathf.Max(0.02f, axis.magnitude);
            t.position = mid;
            t.rotation = axis.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.up, axis) : Quaternion.identity;
            t.localScale = new Vector3(thick, len * 0.5f, thick);
        }

        Transform Limb(Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = m;
            go.transform.SetParent(transform, true);
            return go.transform;
        }

        Transform Ball(Material m, float d)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = m;
            go.transform.localScale = Vector3.one * d;
            return go.transform;
        }

        static Material Mat(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
