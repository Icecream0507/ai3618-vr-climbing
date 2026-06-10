using System.Collections.Generic;
using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Spawns a playable bouldering route from primitives — so v1 runs in an empty scene with no
    /// art: a wall, colour-coded hand/foot holds, a finish hold and a summit trigger at the top.
    /// Assign a <see cref="RouteDefinition"/>, or leave it empty to build the baked default beginner
    /// route (which includes a deliberate same-side stretch that needs a foot/flag to pass).
    ///
    /// Holds are placed on the layer named <see cref="holdLayerName"/>; create that layer first
    /// (Edit → Project Settings → Tags and Layers) and point the hands' / feet's holdLayer at it.
    /// </summary>
    public class RouteBuilder : MonoBehaviour
    {
        public RouteDefinition route;
        [Tooltip("Which baked route to build when no RouteDefinition is assigned (0=Warm-up, 1=Balance Test, 2=The Arete, 3=Endurance).")]
        public int routeIndex = 0;
        [Tooltip("Layer name climb holds are placed on. Create it in Tags & Layers.")]
        public string holdLayerName = "Hold";
        public bool buildOnAwake = true;

        readonly System.Collections.Generic.Dictionary<Color, Material> _matCache =
            new System.Collections.Generic.Dictionary<Color, Material>();

        void Awake() { if (buildOnAwake) Build(); }

        [ContextMenu("Build Route")]
        public void Build()
        {
            Clear();
            var root = new GameObject("Route").transform;
            root.SetParent(transform, false);

            Vector2 wall = route != null ? route.wallSize : new Vector2(3.5f, 6f);
            float thick = route != null ? route.wallThickness : 0.3f;

            // Wall: front face at local z = 0, bottom at y = 0.
            var wallGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGo.name = "Wall";
            wallGo.transform.SetParent(root, false);
            wallGo.transform.localScale = new Vector3(wall.x, wall.y, thick);
            wallGo.transform.localPosition = new Vector3(0f, wall.y * 0.5f, -thick * 0.5f);
            Paint(wallGo, new Color(0.45f, 0.42f, 0.40f));

            var holds = (route != null && route.holds.Count > 0) ? route.holds : RouteCatalog.Get(routeIndex, wall);
            int holdLayer = LayerMask.NameToLayer(holdLayerName);
            if (holdLayer < 0)
                Debug.LogWarning($"[VRClimb] Layer '{holdLayerName}' not found — holds left on Default. " +
                                 "Create it in Tags & Layers and rebuild.", this);

            foreach (var h in holds)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Hold_{h.role}_{h.type}";
                go.transform.SetParent(root, false);
                float s = h.size <= 0f ? 0.16f : h.size;
                go.transform.localScale = Vector3.one * s;
                go.transform.localPosition = h.localPos;
                if (holdLayer >= 0) go.layer = holdLayer;
                var col = go.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;   // grabbed via overlap; must not block the CharacterController

                var hold = go.AddComponent<ClimbHold>();
                hold.role = h.role;
                hold.type = h.type;
                Paint(go, ColorFor(h.role, h.type));
            }

            // Summit trigger spanning the top of the wall.
            var summit = new GameObject("Summit");
            summit.transform.SetParent(root, false);
            summit.transform.localPosition = new Vector3(0f, wall.y + 0.3f, 0.1f);
            var box = summit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(wall.x, 0.6f, 0.8f);
            summit.AddComponent<SummitTrigger>();
        }

        [ContextMenu("Clear Route")]
        public void Clear()
        {
            var existing = transform.Find("Route");
            if (existing == null) return;
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        static Color ColorFor(ClimbHold.HoldRole role, ClimbHold.HoldType type)
        {
            if (type == ClimbHold.HoldType.Finish)  return Color.green;
            if (type == ClimbHold.HoldType.Fragile) return new Color(0.8f, 0.1f, 0.1f);
            if (type == ClimbHold.HoldType.Rest)    return new Color(0.2f, 0.6f, 1f);
            switch (role)
            {
                case ClimbHold.HoldRole.Foot:   return new Color(1f, 0.55f, 0f);     // orange
                case ClimbHold.HoldRole.Either: return new Color(0.7f, 0.3f, 0.9f);  // purple
                default:                        return new Color(0.95f, 0.85f, 0.2f); // yellow
            }
        }

        // One material per colour, reused across rebuilds (avoids leaking a material per hold).
        void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            if (!_matCache.TryGetValue(c, out var m) || m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) return;
                m = new Material(shader);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                _matCache[c] = m;
            }
            r.sharedMaterial = m;
        }
    }
}
