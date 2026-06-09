using System.Collections.Generic;
using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Data-only description of a bouldering route: the wall size plus a list of holds (local
    /// position, role, type, size). Create assets via <c>Assets → Create → VRClimb → Route
    /// Definition</c> and hand-author routes, or leave a <see cref="RouteBuilder"/> with no route to
    /// build the baked default. Positions are local to the wall origin (bottom-centre of the front
    /// face); +Z points away from the wall toward the climber.
    /// </summary>
    [CreateAssetMenu(fileName = "Route", menuName = "VRClimb/Route Definition")]
    public class RouteDefinition : ScriptableObject
    {
        [System.Serializable]
        public struct HoldSpec
        {
            public Vector3 localPos;
            public ClimbHold.HoldRole role;
            public ClimbHold.HoldType type;
            [Tooltip("Diameter in metres (0 = default).")]
            public float size;
        }

        [Tooltip("Wall width x height in metres.")]
        public Vector2 wallSize = new Vector2(3.5f, 6f);
        public float wallThickness = 0.3f;
        public List<HoldSpec> holds = new List<HoldSpec>();
    }
}
