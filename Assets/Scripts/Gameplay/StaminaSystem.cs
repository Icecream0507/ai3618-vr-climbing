using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Optional challenge layer: gripping holds drains stamina, releasing (or resting on a Rest
    /// hold) regenerates it. At zero stamina both hands are forced to let go. Remove the component
    /// entirely for a relaxed / casual mode.
    /// </summary>
    public class StaminaSystem : MonoBehaviour
    {
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;

        [Header("Tuning")]
        public float maxStamina = 100f;
        public float regenPerSecond = 20f;

        public float Stamina { get; private set; }
        public float Normalized => maxStamina > 0f ? Stamina / maxStamina : 0f;
        public event System.Action Exhausted;

        void Awake() => Stamina = maxStamina;

        void Update()
        {
            float drain = HandDrain(leftHand) + HandDrain(rightHand);
            Stamina += (drain > 0f ? -drain : regenPerSecond) * Time.deltaTime;
            Stamina = Mathf.Clamp(Stamina, 0f, maxStamina);

            if (Stamina <= 0f)
            {
                Exhausted?.Invoke();
                leftHand?.ForceRelease();
                rightHand?.ForceRelease();
            }
        }

        static float HandDrain(ClimbingHand hand)
        {
            if (hand == null || !hand.IsGripping || hand.CurrentHold == null) return 0f;
            if (hand.CurrentHold.type == ClimbHold.HoldType.Rest) return 0f;
            return hand.CurrentHold.staminaCostPerSecond;
        }
    }
}
