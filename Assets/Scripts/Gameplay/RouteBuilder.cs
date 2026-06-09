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

            var holds = (route != null && route.holds.Count > 0) ? route.holds : DefaultRoute(wall);
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

        // Short beginner route. Hand holds zig-zag up; a same-side stretch on the right forces a
        // left foot (or flag) to stay balanced; foot holds are threaded below the hands.
        static List<RouteDefinition.HoldSpec> DefaultRoute(Vector2 wall)
        {
            const float z = 0.12f;
            var list = new List<RouteDefinition.HoldSpec>();

            void Add(float x, float y, ClimbHold.HoldRole role, ClimbHold.HoldType type, float size)
                => list.Add(new RouteDefinition.HoldSpec
                {
                    localPos = new Vector3(x, y, z), role = role, type = type, size = size
                });

            void Hand(float x, float y)   => Add(x, y, ClimbHold.HoldRole.Hand,   ClimbHold.HoldType.Normal, 0.16f);
            void Foot(float x, float y)   => Add(x, y, ClimbHold.HoldRole.Foot,   ClimbHold.HoldType.Normal, 0.13f);
            void Either(float x, float y) => Add(x, y, ClimbHold.HoldRole.Either, ClimbHold.HoldType.Normal, 0.16f);

            // Hand holds, bottom -> top.
            Hand(-0.5f, 1.3f); Hand(0.5f, 1.7f);
            Hand(-0.4f, 2.2f); Hand(0.5f, 2.7f);
            Hand(0.6f, 3.2f);  Hand(0.7f, 3.7f);   // <- same-side (right) stretch
            Either(-0.2f, 4.2f); Hand(0.3f, 4.7f);

            // Foot holds threaded below.
            Foot(-0.5f, 0.7f); Foot(0.5f, 1.1f); Foot(-0.4f, 1.6f); Foot(0.4f, 2.1f);
            Foot(0.5f, 2.6f);  Foot(-0.3f, 3.1f); Foot(0.5f, 3.6f);

            // Finish hold near the top.
            Add(0f, wall.y - 0.4f, ClimbHold.HoldRole.Either, ClimbHold.HoldType.Finish, 0.2f);
            return list;
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
