using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// One-click wiring helper. Put this on your XR Origin, assign the HMD and the left/right
    /// controller transforms, then use the context menu <c>Set Up Climber</c>. It adds and links the
    /// CharacterController, ClimbController, ClimbingHand (×2), FootPlacementSystem and BalanceSystem,
    /// and auto-assigns the Hold layer when it exists.
    ///
    /// For a real XR rig, bind each <see cref="ClimbingHand.gripAction"/> to your XRI grip/Select
    /// action in the Inspector. Simulation scenes use <c>overrideGrip</c> instead.
    /// </summary>
    public class PlayerClimberSetup : MonoBehaviour
    {
        public Transform head;
        public Transform leftController;
        public Transform rightController;

        [ContextMenu("Set Up Climber")]
        public void SetUp()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;

            var cc = GetComponent<CharacterController>();
            if (cc == null) cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.6f; cc.radius = 0.25f; cc.center = new Vector3(0f, 0.8f, 0f);
            cc.minMoveDistance = 0f;   // don't swallow slow per-frame climb deltas (see ClimbController)

            var leftHand  = EnsureHand(leftController,  UnityEngine.XR.XRNode.LeftHand,  -VRClimb.Climbing.BodyMetrics.ShoulderHalf);
            var rightHand = EnsureHand(rightController, UnityEngine.XR.XRNode.RightHand, +VRClimb.Climbing.BodyMetrics.ShoulderHalf);

            var feet = GetComponent<FootPlacementSystem>();
            if (feet == null) feet = gameObject.AddComponent<FootPlacementSystem>();
            feet.head = head; feet.rig = transform;

            var balance = GetComponent<BalanceSystem>();
            if (balance == null) balance = gameObject.AddComponent<BalanceSystem>();
            balance.head = head; balance.rig = transform;
            balance.leftHand = leftHand; balance.rightHand = rightHand; balance.feet = feet;

            var controller = GetComponent<ClimbController>();
            if (controller == null) controller = gameObject.AddComponent<ClimbController>();
            controller.rig = transform; controller.characterController = cc;
            controller.leftHand = leftHand; controller.rightHand = rightHand;
            controller.footPlacement = feet; controller.balanceSystem = balance;

            Debug.Log("[VRClimb] Climber wired. Still to do in the Inspector: assign each " +
                      "ClimbingHand.gripAction when using a real XR rig (simulation scenes use " +
                      "overrideGrip instead).", this);
            TryAssignHoldLayer(leftHand, rightHand, feet);
        }

        /// <summary>Sets Hold layer on hands and feet when the layer exists (no-op otherwise).</summary>
        public static void TryAssignHoldLayer(ClimbingHand leftHand, ClimbingHand rightHand, FootPlacementSystem feet)
        {
            int holdLayer = LayerMask.NameToLayer("Hold");
            if (holdLayer < 0)
            {
                Debug.LogWarning("[VRClimb] Layer 'Hold' not found — create it (Edit → Project Settings → " +
                                 "Tags and Layers) or run VRClimb ▸ Set Up Test Scene.", leftHand != null ? leftHand : feet);
                return;
            }
            LayerMask mask = 1 << holdLayer;
            if (leftHand != null) leftHand.holdLayer = mask;
            if (rightHand != null) rightHand.holdLayer = mask;
            if (feet != null) feet.holdLayer = mask;
        }

        ClimbingHand EnsureHand(Transform controller, UnityEngine.XR.XRNode node, float shoulderSide)
        {
            if (controller == null) return null;
            var hand = controller.GetComponent<ClimbingHand>();
            if (hand == null) hand = controller.gameObject.AddComponent<ClimbingHand>();
            hand.handTransform = controller;
            hand.hapticNode = node;
            // Arm-reach limit: a hold must be within arm's length of this shoulder to be grabbed.
            // A little slack over the bare bone length for shoulder mobility / a committing lunge.
            hand.reachHead = head; hand.reachRig = transform;
            hand.shoulderSide = shoulderSide;
            // Bone length + shoulder mobility and a committing lunge — the max dynamic reach.
            hand.armReach = VRClimb.Climbing.BodyMetrics.ArmReach + 0.30f;   // ~0.88 m
            return hand;
        }
    }
}
