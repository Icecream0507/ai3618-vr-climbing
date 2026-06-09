using UnityEngine;
using VRClimb.Gameplay;

namespace VRClimb.Climbing
{
    /// <summary>
    /// Drives the XR Origin (player rig).
    ///
    /// While a hand grips a hold, the rig is moved <b>counter</b> to that hand's tracked motion so
    /// the gripped point stays fixed in the world — the player feels like they are pulling their
    /// body along the wall. This is the same principle as Unity XRI's built-in Climb Provider
    /// ("translate the XR Origin counter to movement of the selecting interactor"); we implement it
    /// directly so the maths is visible and version-independent.
    ///
    /// With no hand gripping, gravity pulls the rig down through a <see cref="CharacterController"/>.
    /// Falling below <see cref="fallResetY"/> respawns the player at the last checkpoint.
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

        [Header("Gravity & Falling")]
        public float gravity = -9.81f;
        [Tooltip("World Y below which the player is considered fallen and is respawned.")]
        public float fallResetY = -10f;

        public bool IsClimbing => _activeHand != null && _activeHand.IsGripping;

        ClimbingHand _activeHand;   // hand currently driving movement
        Vector3 _anchorWorld;       // world position the active hand is pinned to
        Vector3 _velocity;          // gravity accumulation
        Vector3 _spawnPoint;

        void Reset()
        {
            characterController = GetComponent<CharacterController>();
            rig = transform;
        }

        void Awake()
        {
            if (rig == null) rig = transform;
            if (characterController == null) characterController = GetComponent<CharacterController>();
            _spawnPoint = rig.position;
        }

        void OnEnable()  { Subscribe(leftHand, true);  Subscribe(rightHand, true); }
        void OnDisable() { Subscribe(leftHand, false); Subscribe(rightHand, false); }

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
            else GravityStep();

            if (rig.position.y < fallResetY) Respawn();
        }

        void ClimbStep()
        {
            // Counter the hand's motion: move the rig so the active hand returns to its anchor.
            Vector3 handNow = _activeHand.HandPosition;
            Vector3 delta = _anchorWorld - handNow;
            characterController.Move(delta);
            // The hand is a child of the rig, so it moved with the rig; re-pin the anchor.
            _anchorWorld = _activeHand.HandPosition;
        }

        void GravityStep()
        {
            _velocity.y += gravity * Time.deltaTime;
            characterController.Move(_velocity * Time.deltaTime);
            if (characterController.isGrounded && _velocity.y < 0f) _velocity.y = -1f;
        }

        public void SetCheckpoint(Vector3 worldPos) => _spawnPoint = worldPos;

        public void Respawn()
        {
            _velocity = Vector3.zero;
            _activeHand = null;
            // CharacterController must be disabled to teleport reliably.
            characterController.enabled = false;
            rig.position = _spawnPoint;
            characterController.enabled = true;
            GameManager.Instance?.OnPlayerFell();
        }
    }
}
