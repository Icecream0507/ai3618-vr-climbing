using UnityEngine;
using UnityEngine.XR;

namespace VRClimb.Util
{
    /// <summary>
    /// Tiny helper to fire a controller rumble (e.g. when a hand grabs a hold). Uses the
    /// <see cref="UnityEngine.XR.InputDevices"/> haptics API, which is available regardless of the
    /// installed XR Interaction Toolkit version.
    /// </summary>
    public static class HapticFeedback
    {
        public static void Pulse(XRNode node, float amplitude = 0.5f, float duration = 0.1f)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), duration);
        }
    }
}
