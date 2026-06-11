using System.Collections.Generic;
using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Util
{
    /// <summary>
    /// A framework-free self-test for <see cref="ClimbMath"/> — the climbing/balance maths. Add this
    /// component to any GameObject and either tick <see cref="runOnStart"/> or use the context-menu
    /// <c>Run Self-Test</c>; results are logged to the Console (e.g. "ClimbMath self-test: 9/9 passed").
    /// It needs no headset, no scene setup, and no Unity Test Framework, so anyone can sanity-check the
    /// core logic in seconds. (For full NUnit EditMode tests you would add assembly definitions; this
    /// keeps the project's simple, asmdef-free layout.)
    /// </summary>
    public class ClimbMathSelfTest : MonoBehaviour
    {
        public bool runOnStart = false;

        void Start() { if (runOnStart) Run(); }

        [ContextMenu("Run Self-Test")]
        public void Run()
        {
            int pass = 0, fail = 0;
            void Check(string name, bool cond)
            {
                if (cond) pass++;
                else { fail++; Debug.LogError("[ClimbMath self-test] FAIL: " + name, this); }
            }

            const float margin = 0.06f, maxOver = 0.30f;
            Vector3 axis = Vector3.right;
            Vector3 com = Vector3.zero;   // centre of mass (head) at the origin

            List<Vector3> C(params Vector3[] v) => new List<Vector3>(v);

            // --- StabilityScore ---
            // No contacts -> not on the wall -> treated as fully stable.
            Check("no contacts => +1",
                Mathf.Approximately(ClimbMath.StabilityScore(com, axis, C(), margin, maxOver), 1f));

            // One hold directly above the CoM (lateral 0) -> supported.
            Check("single centered => stable",
                ClimbMath.StabilityScore(com, axis, C(new Vector3(0f, 1f, 0f)), margin, maxOver) > 0f);

            // One hold far to the right, CoM at 0 -> leaning out, unstable.
            Check("single far-right => unstable",
                ClimbMath.StabilityScore(com, axis, C(new Vector3(0.5f, 1f, 0f)), margin, maxOver) < 0f);

            // Two holds bracketing the CoM left and right -> well supported.
            Check("bracketed => stable",
                ClimbMath.StabilityScore(com, axis, C(new Vector3(-0.3f, 1f, 0f), new Vector3(0.3f, 1f, 0f)), margin, maxOver) > 0f);

            // Two holds both to the right (same-side) -> CoM outside span -> unstable.
            Check("same-side => unstable",
                ClimbMath.StabilityScore(com, axis, C(new Vector3(0.4f, 1f, 0f), new Vector3(0.6f, 1f, 0f)), margin, maxOver) < 0f);

            // Adding a left foot widens the span back over the CoM -> footwork recovers balance.
            Check("foot recenters same-side => stable",
                ClimbMath.StabilityScore(com, axis, C(new Vector3(0.4f, 1f, 0f), new Vector3(0.6f, 1f, 0f), new Vector3(-0.5f, 0f, 0f)), margin, maxOver) > 0f);

            // Score is bounded in [-1, 1].
            float s = ClimbMath.StabilityScore(com, axis, C(new Vector3(5f, 1f, 0f)), margin, maxOver);
            Check("score >= -1", s >= -1f - 1e-4f);
            Check("score in range", s <= 1f + 1e-4f);

            // --- ClimbDelta ---
            // Hand tracked upward by 0.1 -> rig must move down 0.1 to keep the hold pinned.
            Vector3 d = ClimbMath.ClimbDelta(new Vector3(0f, 1f, 0f), new Vector3(0f, 1.1f, 0f));
            Check("climb delta cancels hand motion", Mathf.Approximately(d.y, -0.1f));

            Debug.Log($"[ClimbMath self-test] {pass}/{pass + fail} passed" + (fail == 0 ? " ✅" : $" — {fail} FAILED"), this);
        }
    }
}
