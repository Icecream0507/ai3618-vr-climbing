using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Util
{
    /// <summary>
    /// Makes the (otherwise invisible) climber rig watchable for the recorded demo: a simple capsule
    /// "torso" tracking head→pelvis (so the lean is visible), and a smooth third-person spectator
    /// camera that follows the climber up the wall. Head/hand/foot markers are plain primitives
    /// created by <c>DemoBuild</c>; this component just animates the torso and the camera.
    ///
    /// Demo-only and inert in real gameplay — nothing here touches the climbing logic.
    /// </summary>
    public class DemoVisuals : MonoBehaviour
    {
        public Transform head;
        public Transform body;          // capsule torso to orient between head and pelvis
        public Camera spectator;        // camera to follow the climb
        public Transform lookTargetRoot; // rig root (x stays at wall centre)

        [Header("Camera framing")]
        public Vector3 camOffset = new Vector3(2.1f, 0.2f, 2.8f);  // front-right of the wall
        public float camLag = 2.5f;
        public float pelvisDrop = 1.0f; // torso length below the head

        float _camY;

        void Start()
        {
            if (head != null) _camY = head.position.y - 0.6f;
        }

        void LateUpdate()
        {
            if (head != null && body != null)
            {
                Vector3 pelvis = head.position + Vector3.down * pelvisDrop;
                body.position = (head.position + pelvis) * 0.5f;
                Vector3 dir = head.position - pelvis;
                if (dir.sqrMagnitude > 1e-5f)
                    body.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                body.localScale = new Vector3(0.34f, Mathf.Max(0.2f, dir.magnitude * 0.5f), 0.34f);
            }

            if (spectator != null && head != null)
            {
                float targetY = head.position.y - 0.4f;
                _camY = Mathf.Lerp(_camY, targetY, Time.deltaTime * camLag);
                float baseX = lookTargetRoot != null ? lookTargetRoot.position.x : 0f;
                spectator.transform.position = new Vector3(baseX + camOffset.x, _camY + camOffset.y, camOffset.z);
                spectator.transform.LookAt(new Vector3(baseX, _camY + 0.7f, 0.15f));
            }
        }
    }
}
