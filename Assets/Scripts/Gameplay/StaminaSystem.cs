using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Optional challenge layer: gripping a (non-Rest) hold drains stamina; releasing, or resting on a
    /// Rest hold, regenerates it. At zero stamina both hands are force-released, dropping the climber
    /// into the normal fall/respawn loop. Remove the component entirely for a relaxed / casual mode.
    ///
    /// Self-wiring, so it works without manual Inspector hookup:
    ///  - if the hand refs are empty it auto-finds the two ClimbingHands in the scene on Awake;
    ///  - it refills to full on every fall (subscribes to GameManager.PlayerFell), so a respawn starts
    ///    fresh instead of at whatever stamina the fall left behind.
    /// The HUD's stamina bar is linked automatically by "VRClimb ▸ Set Up HUD + Audio"; attach this
    /// component via "VRClimb ▸ Add Stamina to Climber".
    /// </summary>
    public class StaminaSystem : MonoBehaviour
    {
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;

        [Header("Tuning (empirical placeholders — verify in-engine)")]
        [Tooltip("Total stamina pool.")]
        public float maxStamina = 100f;
        [Tooltip("Recovery per second while not gripping, or while on a Rest hold.")]
        public float regenPerSecond = 25f;

        public float Stamina { get; private set; }
        public float Normalized => maxStamina > 0f ? Stamina / maxStamina : 0f;

        /// <summary>Raised once each time stamina hits zero (not every frame).</summary>
        public event System.Action Exhausted;

        bool _depleted;

        void Awake()
        {
            Stamina = maxStamina;
            if (leftHand == null || rightHand == null) AutoFindHands();
        }

        // GameManager is a singleton that may be created in the same frame, so (like ClimbAudio) we
        // subscribe in Start, after all Awakes have run.
        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerFell += ResetStamina;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerFell -= ResetStamina;
        }

        /// <summary>Refill to full (called on each fall, or manually to restart a run).</summary>
        public void ResetStamina()
        {
            Stamina = maxStamina;
            _depleted = false;
        }

        void Update()
        {
            float drain = HandDrain(leftHand) + HandDrain(rightHand);
            Stamina += (drain > 0f ? -drain : regenPerSecond) * Time.deltaTime;
            Stamina = Mathf.Clamp(Stamina, 0f, maxStamina);

            if (Stamina <= 0f)
            {
                if (!_depleted)                 // fire once per depletion, not every frame
                {
                    _depleted = true;
                    Exhausted?.Invoke();
                    leftHand?.ForceRelease();
                    rightHand?.ForceRelease();
                }
            }
            else
            {
                _depleted = false;
            }
        }

        void AutoFindHands()
        {
            var hands = FindObjectsOfType<ClimbingHand>();
            foreach (var h in hands)
            {
                if (h.hapticNode == UnityEngine.XR.XRNode.LeftHand && leftHand == null) leftHand = h;
                else if (h.hapticNode == UnityEngine.XR.XRNode.RightHand && rightHand == null) rightHand = h;
            }
            // Fallback if hapticNode wasn't set: just take the first two found.
            if (leftHand == null && hands.Length > 0) leftHand = hands[0];
            if (rightHand == null && hands.Length > 1) rightHand = hands[1];
        }

        static float HandDrain(ClimbingHand hand)
        {
            if (hand == null || !hand.IsGripping || hand.CurrentHold == null) return 0f;
            if (hand.CurrentHold.type == ClimbHold.HoldType.Rest) return 0f;
            return hand.CurrentHold.staminaCostPerSecond;
        }
    }
}
