using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Util
{
    /// <summary>
    /// A simple articulated climber built from primitives, for the recorded demo's third-person view.
    /// NOT used in real VR play (there you are first-person and only see your own hands). It exists so
    /// the spectator video reads as a person with weight:
    ///
    ///  • anatomical limb lengths from <see cref="BodyMetrics"/> (arms ~0.58 m, legs ~0.88 m);
    ///  • 2-bone IK with joint limits — elbows/knees can't hyperextend and bend the natural way;
    ///  • a gravity-driven hip pendulum (damped spring) so the body sags and sways under its weight
    ///    instead of being rigidly pinned to the head — and legs dangle when a foot is off a hold.
    ///
    /// It is purely cosmetic: it reads the head/hand/foot transforms the gameplay drives and never
    /// feeds anything back into the climbing logic.
    /// </summary>
    public class HumanoidRig : MonoBehaviour
    {
        public Transform head;          // HMD proxy (already has a skin-coloured sphere)
        public Transform leftHand;      // controller proxies the sim moves onto holds
        public Transform rightHand;
        public Transform leftFoot;      // foot markers FootPlacementSystem positions on planted holds
        public Transform rightFoot;
        public Transform rig;           // for lateral / forward axes

        [Header("Gravity feel")]
        [Tooltip("Hip pendulum stiffness toward the rest pose. Higher = stiffer/less sway.")]
        public float hipStiffness = 55f;
        public float hipDamping = 9f;
        [Tooltip("How far the hips sag below the rest pose under load (m).")]
        public float sag = 0.05f;

        Transform _torso, _pelvis, _neck;
        Transform _luA, _lfA, _ruA, _rfA;          // arm capsules
        Transform _luL, _llL, _ruL, _rlL;          // leg capsules
        Material _skin, _jacket, _pants;

        Vector3 _hip;          // simulated hip position (the pendulum bob)
        Vector3 _hipVel;
        bool _init;

        void Start()
        {
            _skin   = Mat(new Color(0.95f, 0.78f, 0.60f));
            _jacket = Mat(new Color(0.12f, 0.55f, 0.62f));   // teal — stands out from yellow/orange holds
            _pants  = Mat(new Color(0.18f, 0.22f, 0.32f));

            _torso  = Limb(_jacket); _pelvis = Limb(_pants); _neck = Limb(_skin);
            _luA = Limb(_jacket); _lfA = Limb(_jacket); _ruA = Limb(_jacket); _rfA = Limb(_jacket);
            _luL = Limb(_pants);  _llL = Limb(_pants);  _ruL = Limb(_pants);  _rlL = Limb(_pants);
        }

        void LateUpdate()
        {
            if (head == null || rig == null) return;
            Vector3 right = rig.right, up = Vector3.up, fwd = Vector3.forward;  // +z = away from wall
            float dt = Mathf.Max(1e-4f, Time.deltaTime);

            Vector3 shC = head.position - up * BodyMetrics.ShoulderDrop;

            // --- gravity-driven hips: a damped spring toward the rest pose, plus a sag, so the body
            //     has weight and sways instead of being rigidly bolted under the head. ---
            int footHolds = (leftFoot != null && leftFoot.gameObject.activeInHierarchy ? 1 : 0)
                          + (rightFoot != null && rightFoot.gameObject.activeInHierarchy ? 1 : 0);
            float support = footHolds > 0 ? 1f : 0.45f;   // feet on the wall steady the hips
            Vector3 hipRest = head.position - up * (BodyMetrics.HipDrop + sag * (2 - footHolds));
            if (!_init) { _hip = hipRest; _init = true; }
            Vector3 accel = (hipRest - _hip) * (hipStiffness * support) - _hipVel * (hipDamping * support);
            accel += up * -2.0f;                          // a little gravity bias -> hangs/sways
            _hipVel += accel * dt;
            _hip += _hipVel * dt;
            // keep the torso a sane length even mid-swing
            _hip = shC + Vector3.ClampMagnitude(_hip - shC, BodyMetrics.HipDrop + 0.12f);

            Vector3 hpC = _hip;
            Vector3 lSh = shC - right * BodyMetrics.ShoulderHalf, rSh = shC + right * BodyMetrics.ShoulderHalf;
            Vector3 lHp = hpC - right * BodyMetrics.HipHalf,      rHp = hpC + right * BodyMetrics.HipHalf;

            PlaceCapsule(_neck,  shC, head.position - up * 0.04f, 0.085f);
            PlaceCapsule(_torso, shC, hpC, 0.27f);
            PlaceCapsule(_pelvis, lHp, rHp, 0.20f);

            // Arms: elbows bend down/out/away-from-wall.
            Vector3 armPoleL = (-right * 0.5f - up * 0.7f + fwd * 0.4f).normalized;
            Vector3 armPoleR = ( right * 0.5f - up * 0.7f + fwd * 0.4f).normalized;
            SolveLimb(lSh, leftHand.position,  BodyMetrics.UpperArm, BodyMetrics.ForeArm, armPoleL, _luA, _lfA, 0.085f);
            SolveLimb(rSh, rightHand.position, BodyMetrics.UpperArm, BodyMetrics.ForeArm, armPoleR, _ruA, _rfA, 0.085f);

            // Legs: knees bend out/away-from-wall; when the foot is off a hold it dangles under gravity.
            Vector3 legPoleL = (-right * 0.4f + fwd * 0.85f - up * 0.1f).normalized;
            Vector3 legPoleR = ( right * 0.4f + fwd * 0.85f - up * 0.1f).normalized;
            Vector3 lFootT = (leftFoot != null && leftFoot.gameObject.activeInHierarchy)
                ? leftFoot.position : lHp + (-right * 0.10f - up * (BodyMetrics.LegReach - 0.06f) + fwd * 0.04f);
            Vector3 rFootT = (rightFoot != null && rightFoot.gameObject.activeInHierarchy)
                ? rightFoot.position : rHp + ( right * 0.10f - up * (BodyMetrics.LegReach - 0.06f) + fwd * 0.04f);
            SolveLimb(lHp, lFootT, BodyMetrics.Thigh, BodyMetrics.Shin, legPoleL, _luL, _llL, 0.10f);
            SolveLimb(rHp, rFootT, BodyMetrics.Thigh, BodyMetrics.Shin, legPoleR, _ruL, _rlL, 0.10f);
        }

        // Analytic 2-bone IK with joint limits: clamp the target into reach so the joint is always
        // real (no hyperextension), then place the joint on the pole side so the bend is anatomical.
        void SolveLimb(Vector3 root, Vector3 target, float l1, float l2, Vector3 pole,
                       Transform seg1, Transform seg2, float thick)
        {
            Vector3 toT = target - root;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, (l1 + l2) - 1e-3f);
            Vector3 dir = toT.sqrMagnitude > 1e-6f ? toT.normalized : Vector3.down;
            target = root + dir * d;   // taut limb if the hold was beyond reach -> visibly can't reach

            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 polePerp = pole - Vector3.Dot(pole, dir) * dir;
            if (polePerp.sqrMagnitude < 1e-6f) polePerp = Vector3.Cross(dir, Vector3.right);
            polePerp.Normalize();
            Vector3 joint = root + dir * a + polePerp * h;

            PlaceCapsule(seg1, root, joint, thick);
            PlaceCapsule(seg2, joint, target, thick * 0.92f);
        }

        // Stretch/orient a unit capsule (local height 2) between A and B.
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
