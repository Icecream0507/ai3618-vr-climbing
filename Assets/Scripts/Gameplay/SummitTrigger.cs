using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Place at the top of the wall. When the player rig enters this trigger volume, the climb is
    /// marked complete. The player is detected by looking for a <see cref="ClimbController"/> on the
    /// entering object (its CharacterController fires the trigger) — no manual "Player" tag needed.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SummitTrigger : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<ClimbController>() == null) return;
            GameManager.Instance?.OnSummitReached();
        }
    }
}
