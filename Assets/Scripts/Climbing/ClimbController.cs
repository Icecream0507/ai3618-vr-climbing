using UnityEngine;
using VRClimb.Gameplay;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Drives the XR Origin (player rig).
    ///
    /// While a hand grips a hold, the rig is moved <b>counter</b> to that hand's tracked motion so
    /// the gripped point stays fixed in the world — the player pulls their body along the wall (same
    /// principle as Unity XRI's Climb Provider). With no contact at all, gravity pulls the rig down
    /// through a <see cref="CharacterController"/>; falling below <see cref="fallResetY"/> respawns
    /// at the last checkpoint.
    ///
    /// Balance &amp; feet are additive layers (kept optional so the hands-only baseline always works):
    /// <see cref="FootPlacementSystem"/> supplies auto-placed virtual-foot contacts, and
    /// <see cref="BalanceSystem"/> fires <c>PeelOff</c> when the climber loses balance — we then let
    /// go of everything and the normal fall/respawn loop takes over.
    ///
    /// Two-hand rule: the most recently grabbed hand drives movement; when it releases, control
    /// passes back to the other hand if it is still holding on.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ClimbController : MonoBehaviour
    {
        [Header("Rig")]
        [Tooltip("XR Origin / rig root to move. Defaults to this GameObject.")]
        public Transform rig;
        public CharacterController characterController;

        [Header("Hands")]
        public ClimbingHand leftHand;
        public ClimbingHand rightHand;

        [Header("Balance & Feet (optional)")]
        public FootPlacementSystem footPlacement;
        public BalanceSystem balanceSystem;

        [Header("Gravity & Falling")]
        public float gravity = -9.81f;
        [Tooltip("World Y below which the player is considered fallen and is respawned.")]
        public float fallResetY = -10f;

        [Header("Safety")]
        [Tooltip("Hard cap on how far one frame of counter-motion may move the rig (m). A real hand " +
                 "can't move far in a single frame, so this stops a frame-time spike, a tracking " +
                 "glitch, or a runaway auto-pull from teleporting the body up past several holds.")]
        public float maxStepPerFrame = 0.5f;

        public bool IsClimbing => _activeHand != null && _activeHand.IsGripping;

        bool HasContact =>
            (leftHand != null && leftHand.IsGripping) ||
            (rightHand != null && rightHand.IsGripping) ||
            (footPlacement != null && footPlacement.PlantedCount > 0);

        ClimbingHand _activeHand;   // hand currently driving movement
        Vector3 _anchorWorld;       // world position the active hand is pinned to
        Vector3 _velocity;          // gravity accumulation
        Vector3 _spawnPoint;
        bool _fellFromWall;         // set on peel-off; landing afterwards counts as a fall

        void Reset()
        {
            characterController = GetComponent<CharacterController>();
            rig = transform;
        }

        void Awake()
        {
            if (rig == null) rig = transform;
            if (characterController == null) characterController = GetComponent<CharacterController>();
            // Climbing moves the rig by per-frame hand deltas. A slow pull at headset framerates is
            // well under CharacterController.minMoveDistance (default 0.001 m) and Move() silently
            // swallows such motion — and because ClimbStep re-pins the anchor each frame, the lost
            // remainder never comes back (the climb does nothing). Zero it so every delta applies.
            if (characterController != null) characterController.minMoveDistance = 0f;
            _spawnPoint = rig.position;
        }

        void OnEnable()
        {
            Subscribe(leftHand, true);  Subscribe(rightHand, true);
            if (balanceSystem != null) balanceSystem.PeelOff += OnPeelOff;
        }

        void OnDisable()
        {
            Subscribe(leftHand, false); Subscribe(rightHand, false);
            if (balanceSystem != null) balanceSystem.PeelOff -= OnPeelOff;
        }

        void Subscribe(ClimbingHand hand, bool add)
        {
            if (hand == null) return;
            if (add) { hand.Grabbed += OnHandGrabbed; hand.Released += OnHandReleased; }
            else     { hand.Grabbed -= OnHandGrabbed; hand.Released -= OnHandReleased; }
        }

        void OnHandGrabbed(ClimbingHand hand)
        {
            _activeHand = hand;             // most recent grab wins
            _anchorWorld = hand.HandPosition;
            _velocity = Vector3.zero;       // cancel any fall the moment you catch a hold
            _fellFromWall = false;          // caught yourself — no longer falling

            // Start the run (and the timer) on the first grab.
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Ready)
                GameManager.Instance.StartClimb();
        }

        void OnHandReleased(ClimbingHand hand)
        {
            if (_activeHand != hand) return;
            // Hand control back to the other hand if it is still gripping.
            if (rightHand != null && rightHand != hand && rightHand.IsGripping)
            { _activeHand = rightHand; _anchorWorld = rightHand.HandPosition; }
            else if (leftHand != null && leftHand != hand && leftHand.IsGripping)
            { _activeHand = leftHand; _anchorWorld = leftHand.HandPosition; }
            else
                _activeHand = null;
        }

        void Update()
        {
            if (IsClimbing) ClimbStep();
            else if (!HasContact) GravityStep();
            // else: supported by feet only — hang in place (no movement, no fall).

            if (rig.position.y < fallResetY) Respawn();
        }

        void OnPeelOff()
        {
            // Balance ran out: let go of everything so gravity takes over and you fall to checkpoint.
            _velocity = Vector3.zero;
            _fellFromWall = true;
            if (leftHand != null) leftHand.ForceRelease();
            if (rightHand != null) rightHand.ForceRelease();
            if (footPlacement != null) footPlacement.DropAll();
        }

        void ClimbStep()
        {
            // Counter the hand's motion: move the rig so the active hand returns to its anchor.
            Vector3 handNow = _activeHand.HandPosition;
            Vector3 delta = ClimbMath.ClimbDelta(_anchorWorld, handNow);
            // Hard per-frame cap so a dt spike / glitch / runaway auto-pull can't teleport the body up
            // past several holds. Normal climbing deltas are ~centimetres and never reach this.
            delta = Vector3.ClampMagnitude(delta, maxStepPerFrame);
            characterController.Move(delta);
            // The hand is a child of the rig, so it moved with the rig; re-pin the anchor.
            _anchorWorld = _activeHand.HandPosition;
        }

        void GravityStep()
        {
            _velocity.y += gravity * Time.deltaTime;
            characterController.Move(_velocity * Time.deltaTime);
            if (characterController.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -1f;
                // Peeling off and hitting the mat counts as a fall — reset to the checkpoint.
                // (fallResetY below stays as the safety net for scenes without a floor.)
                if (_fellFromWall) Respawn();
            }
        }

        public void SetCheckpoint(Vector3 worldPos) => _spawnPoint = worldPos;

        public void Respawn()
        {
            _velocity = Vector3.zero;
            _activeHand = null;
            _fellFromWall = false;
            // CharacterController must be disabled to teleport reliably.
            characterController.enabled = false;
            rig.position = _spawnPoint;
            characterController.enabled = true;
            if (balanceSystem != null) balanceSystem.ResetBalance();
            GameManager.Instance?.OnPlayerFell();
        }
    }
}
