using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Util
{
    /// <summary>
    /// Makes grabbing usable with the XR Device Simulator. The simulator's grip is momentary and only
    /// applies to the controller you're actively manipulating, so keeping one hand gripped while you
    /// move the other needs an awkward hold-and-switch dance. This turns grip into a <b>toggle</b>:
    /// a rising edge on a hand's grip input flips that hand's <see cref="ClimbingHand.overrideGrip"/>,
    /// so a single tap of <c>G</c> latches the hand on the nearest hold and it stays there until you
    /// tap <c>G</c> on that same hand again.
    ///
    /// Input-only convenience (uses the existing overrideGrip contract the sim/robot already use) —
    /// it never touches balance, reach, or rig movement.
    /// </summary>
    public class VRGripLatch : MonoBehaviour
    {
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;

        bool _lPrev, _rPrev;

        void Update()
        {
            Tick(leftHand, ref _lPrev);
            Tick(rightHand, ref _rPrev);
        }

        void Tick(ClimbingHand h, ref bool prev)
        {
            if (h == null) return;
            var a = h.gripAction.action;
            float v = a != null ? a.ReadValue<float>() : 0f;
            bool pressed = v >= h.gripThreshold;
            if (pressed && !prev) h.overrideGrip = !h.overrideGrip;   // toggle on press
            prev = pressed;
        }
    }
}
