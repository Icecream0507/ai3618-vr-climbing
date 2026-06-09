using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// One-click wiring helper. Put this on your XR Origin, assign the HMD and the left/right
    /// controller transforms, then use the context menu <c>Set Up Climber</c>. It adds and links the
    /// CharacterController, ClimbController, ClimbingHand (×2), FootPlacementSystem and BalanceSystem.
    ///
    /// Two things it deliberately leaves for you (they can't be guessed safely): each
    /// <see cref="ClimbingHand.gripAction"/> (bind to your XRI grip/Select action) and the Hold
    /// <c>LayerMask</c> on the hands and feet. Set those in the Inspector afterwards.
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

            var leftHand  = EnsureHand(leftController,  UnityEngine.XR.XRNode.LeftHand);
            var rightHand = EnsureHand(rightController, UnityEngine.XR.XRNode.RightHand);

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
                      "ClimbingHand.gripAction, and set the Hold LayerMask on both hands and the " +
                      "FootPlacementSystem.", this);
        }

        ClimbingHand EnsureHand(Transform controller, UnityEngine.XR.XRNode node)
        {
            if (controller == null) return null;
            var hand = controller.GetComponent<ClimbingHand>();
            if (hand == null) hand = controller.gameObject.AddComponent<ClimbingHand>();
            hand.handTransform = controller;
            hand.hapticNode = node;
            return hand;
        }
    }
}
