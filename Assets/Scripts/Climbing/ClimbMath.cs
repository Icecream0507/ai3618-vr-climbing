using System.Collections.Generic;
using UnityEngine;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Pure, side-effect-free maths for the climbing systems, extracted so the logic can be unit-
    /// tested without the Unity engine objects (Transforms, Physics, MonoBehaviours). Only operates
    /// on plain vectors and numbers. See <c>ClimbMathSelfTest</c> for runnable checks.
    /// </summary>
    public static class ClimbMath
    {
        /// <summary>
        /// Signed stability of the centre of mass relative to the lateral support span of the
        /// contact points. <paramref name="origin"/> is the CoM (HMD) world position;
        /// <paramref name="axis"/> is the lateral (wall-right) direction. Contacts are gripped
        /// hand-holds plus planted feet. Returns &gt;0 (up to 1) when the CoM is comfortably within
        /// the contacts' lateral span, &lt;0 (down to -1) the further it leans outside. With no
        /// contacts it returns +1 (you are not on the wall, so there is nothing to be unstable about).
        /// </summary>
        public static float StabilityScore(Vector3 origin, Vector3 axis, IReadOnlyList<Vector3> contacts,
                                           float supportMargin, float maxOvershoot)
        {
            int n = contacts != null ? contacts.Count : 0;
            if (n == 0) return 1f;

            axis = axis.sqrMagnitude > 1e-8f ? axis.normalized : Vector3.right;

            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                float x = Vector3.Dot(contacts[i] - origin, axis);   // lateral offset of contact from CoM
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }

            float low = minX - supportMargin;
            float high = maxX + supportMargin;

            // The CoM lateral coordinate is 0 by construction (measured relative to origin = CoM).
            if (0f >= low && 0f <= high)
            {
                float margin = Mathf.Min(0f - low, high - 0f);
                float halfSpan = Mathf.Max(1e-4f, (high - low) * 0.5f);
                return Mathf.Clamp01(margin / halfSpan);             // inside the span -> stable
            }

            float overshoot = (0f < low) ? (low - 0f) : (0f - high);
            return -Mathf.Clamp01(overshoot / Mathf.Max(1e-4f, maxOvershoot));   // outside -> unstable
        }

        /// <summary>
        /// Counter-motion locomotion delta: the amount to move the rig so the active hand returns to
        /// its anchor, cancelling the hand's tracked motion and pulling the body along the wall.
        /// </summary>
        public static Vector3 ClimbDelta(Vector3 anchorWorld, Vector3 handWorldNow)
            => anchorWorld - handWorldNow;
    }
}
