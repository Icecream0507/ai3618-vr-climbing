using System.Collections.Generic;
using UnityEngine;
using VRClimb.Climbing;

namespace VRClimb.Gameplay
{
    /// <summary>
    /// Hand-authored beginner routes used when no <see cref="RouteDefinition"/> asset is assigned to
    /// the <see cref="RouteBuilder"/>. Each is designed with an intended balance/footwork challenge.
    /// Positions are local to the wall origin (bottom-centre of the front face); z is the small
    /// offset in front of the wall so holds are reachable.
    /// </summary>
    public static class RouteCatalog
    {
        public const int Count = 3;
        const float Z = 0.12f;

        public static readonly string[] Names = { "Warm-up", "Balance Test", "The Arete" };

        public static List<RouteDefinition.HoldSpec> Get(int index, Vector2 wall)
        {
            switch (Mathf.Clamp(index, 0, Count - 1))
            {
                case 1:  return BalanceTest(wall);
                case 2:  return TheArete(wall);
                default: return WarmUp(wall);
            }
        }

        // ---- authoring helpers ----
        static void Hand(List<RouteDefinition.HoldSpec> l, float x, float y, float s = 0.16f)
            => l.Add(Spec(x, y, ClimbHold.HoldRole.Hand, ClimbHold.HoldType.Normal, s));
        static void Foot(List<RouteDefinition.HoldSpec> l, float x, float y, float s = 0.13f)
            => l.Add(Spec(x, y, ClimbHold.HoldRole.Foot, ClimbHold.HoldType.Normal, s));
        static void Either(List<RouteDefinition.HoldSpec> l, float x, float y, float s = 0.16f)
            => l.Add(Spec(x, y, ClimbHold.HoldRole.Either, ClimbHold.HoldType.Normal, s));
        static void Rest(List<RouteDefinition.HoldSpec> l, float x, float y)
            => l.Add(Spec(x, y, ClimbHold.HoldRole.Hand, ClimbHold.HoldType.Rest, 0.18f));
        static void Fragile(List<RouteDefinition.HoldSpec> l, float x, float y)
            => l.Add(Spec(x, y, ClimbHold.HoldRole.Hand, ClimbHold.HoldType.Fragile, 0.16f));
        static void Finish(List<RouteDefinition.HoldSpec> l, float x, float y)
            => l.Add(Spec(x, y, ClimbHold.HoldRole.Either, ClimbHold.HoldType.Finish, 0.2f));
        static RouteDefinition.HoldSpec Spec(float x, float y, ClimbHold.HoldRole r, ClimbHold.HoldType t, float s)
            => new RouteDefinition.HoldSpec { localPos = new Vector3(x, y, Z), role = r, type = t, size = s };

        // Route 0 — gentle zig-zag with one same-side stretch.
        static List<RouteDefinition.HoldSpec> WarmUp(Vector2 wall)
        {
            var l = new List<RouteDefinition.HoldSpec>();
            Hand(l, -0.5f, 1.3f); Hand(l, 0.5f, 1.7f); Hand(l, -0.4f, 2.2f); Hand(l, 0.5f, 2.7f);
            Hand(l, 0.6f, 3.2f);  Hand(l, 0.7f, 3.7f);                 // same-side (right) stretch
            Either(l, -0.2f, 4.2f); Hand(l, 0.3f, 4.7f);
            Foot(l, -0.5f, 0.7f); Foot(l, 0.5f, 1.1f); Foot(l, -0.4f, 1.6f); Foot(l, 0.4f, 2.1f);
            Foot(l, 0.5f, 2.6f);  Foot(l, -0.3f, 3.1f); Foot(l, 0.5f, 3.6f);
            Finish(l, 0f, wall.y - 0.4f);
            return l;
        }

        // Route 1 — two same-side sequences and sparse feet: footwork must hold your balance.
        static List<RouteDefinition.HoldSpec> BalanceTest(Vector2 wall)
        {
            var l = new List<RouteDefinition.HoldSpec>();
            Hand(l, -0.6f, 1.2f); Hand(l, -0.7f, 1.7f);                // left same-side
            Rest(l, 0.0f, 2.1f);                                       // breather in the middle
            Hand(l, 0.7f, 2.5f); Hand(l, 0.8f, 3.0f); Hand(l, 0.7f, 3.5f); // right same-side
            Hand(l, -0.3f, 4.0f); Either(l, 0.2f, 4.5f);
            Foot(l, -0.6f, 0.7f); Foot(l, -0.6f, 1.4f);                // feet under the left section
            Foot(l, 0.7f, 2.0f);  Foot(l, 0.7f, 2.9f);                 // feet under the right section
            Foot(l, -0.2f, 3.5f);
            Finish(l, 0f, wall.y - 0.4f);
            return l;
        }

        // Route 2 — taller, with a fragile hold you can't linger on (forces committing a foot/flag).
        static List<RouteDefinition.HoldSpec> TheArete(Vector2 wall)
        {
            var l = new List<RouteDefinition.HoldSpec>();
            Hand(l, 0.4f, 1.2f); Hand(l, -0.4f, 1.6f); Hand(l, 0.4f, 2.1f);
            Fragile(l, 0.5f, 2.6f);                                    // don't hang here
            Hand(l, -0.5f, 3.0f); Hand(l, -0.6f, 3.5f);                // left same-side after the fragile
            Either(l, 0.3f, 3.9f); Hand(l, -0.2f, 4.4f); Hand(l, 0.3f, 4.9f);
            Foot(l, 0.4f, 0.7f); Foot(l, -0.4f, 1.1f); Foot(l, 0.4f, 1.6f);
            Foot(l, 0.5f, 2.1f); Foot(l, -0.5f, 2.6f); Foot(l, -0.5f, 3.2f); Foot(l, 0.2f, 3.8f);
            Finish(l, 0f, wall.y - 0.4f);
            return l;
        }
    }
}
