using UnityEngine;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Place at the top of the wall. When the player rig (tagged <see cref="playerTag"/>) enters
    /// this trigger volume, the climb is marked complete.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SummitTrigger : MonoBehaviour
    {
        public string playerTag = "Player";

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            GameManager.Instance?.OnSummitReached();
        }
    }
}
