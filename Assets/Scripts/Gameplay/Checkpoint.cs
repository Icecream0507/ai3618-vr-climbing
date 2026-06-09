using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// When the player passes through, updates the respawn point on the <see cref="ClimbController"/>
    /// so a fall sends them back here instead of all the way to the bottom.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        public string playerTag = "Player";

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            var controller = other.GetComponentInParent<ClimbController>();
            if (controller != null) controller.SetCheckpoint(transform.position);
        }
    }
}
