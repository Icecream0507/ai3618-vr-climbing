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
        public const int Count = 5;
        const float Z = 0.12f;

        public static readonly string[] Names = { "Warm-up", "Balance Test", "The Arete", "Endurance", "The Gap (unclimbable)" };

        public static List<RouteDefinition.HoldSpec> Get(int index, Vector2 wall)
        {
            switch (Mathf.Clamp(index, 0, Count - 1))
            {
                case 1:  return BalanceTest(wall);
                case 2:  return TheArete(wall);
                case 3:  return Endurance(wall);
                case 4:  return TheGap(wall);
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

        // Route 2 — "The Arete": a line up the edge with a tempting Fragile hold mid-way you must NOT
        // weight (commit a foot / move past it instead). Gentle zig-zags (≤ ~0.6 m apart) so balance is
        // about reading the fragile decoy, not lunging.
        static List<RouteDefinition.HoldSpec> TheArete(Vector2 wall)
        {
            var l = new List<RouteDefinition.HoldSpec>();
            Hand(l, 0.25f, 1.2f); Hand(l, -0.25f, 1.6f); Hand(l, 0.3f, 2.1f);
            Fragile(l, 0.05f, 2.5f);                                   // tempting central hold — but it crumbles
            Hand(l, -0.3f, 2.9f); Hand(l, 0.25f, 3.4f);
            Either(l, -0.1f, 3.8f); Hand(l, 0.3f, 4.3f); Hand(l, -0.2f, 4.8f);
            Foot(l, 0.3f, 0.7f); Foot(l, -0.3f, 1.1f); Foot(l, 0.3f, 1.6f);
            Foot(l, -0.2f, 2.2f); Foot(l, 0.2f, 2.8f); Foot(l, -0.3f, 3.4f); Foot(l, 0.2f, 4.0f);
            Finish(l, 0f, wall.y - 0.4f);
            return l;
        }

        // Route 3 — Endurance. Headline challenge is *stamina management*, not a single hard move:
        // a long, hold-dense zig-zag that keeps draining you, with two Rest holds (blue) you must use
        // to recover, and a Fragile decoy near the top that looks like a third rest but crumbles —
        // so you have to read the wall and commit your breathers to the real rest points. Feet are
        // threaded throughout for support (they also keep you balanced through the longer reaches).
        static List<RouteDefinition.HoldSpec> Endurance(Vector2 wall)
        {
            var l = new List<RouteDefinition.HoldSpec>();

            // Lower section: dense hand holds to start the drain.
            Hand(l, -0.5f, 1.3f); Hand(l, 0.5f, 1.6f); Hand(l, -0.5f, 1.9f); Hand(l, 0.4f, 2.2f);
            Rest(l, -0.1f, 2.5f);                                   // first breather — recover before the middle
            // Middle section: keep moving, no easy rest.
            Hand(l, 0.5f, 2.9f); Hand(l, -0.4f, 3.2f);
            Fragile(l, 0.5f, 3.5f);                                 // decoy "rest": looks restful, but it breaks
            Hand(l, -0.5f, 3.8f);
            Rest(l, 0.1f, 4.1f);                                    // last real breather before the top push
            // Top push: commit to the finish.
            Hand(l, 0.5f, 4.5f); Hand(l, -0.3f, 4.9f); Either(l, 0.2f, 5.2f);
            Finish(l, 0f, wall.y - 0.4f);

            // Feet threaded below the hands the whole way up (support + balance through the reaches).
            Foot(l, -0.5f, 0.8f); Foot(l, 0.5f, 1.1f); Foot(l, -0.4f, 1.5f); Foot(l, 0.4f, 1.9f);
            Foot(l, -0.3f, 2.4f); Foot(l, 0.5f, 2.8f); Foot(l, -0.4f, 3.3f); Foot(l, 0.4f, 3.7f);
            Foot(l, -0.3f, 4.2f); Foot(l, 0.4f, 4.6f);
            return l;
        }

        // Route 4 — "The Gap": deliberately UNCLIMBABLE, to show the reach limit is a real constraint.
        // A normal, reachable lower section ends at ~2.2 m. Then there is a blank wall: the next hand
        // hold sits ~2.0 m higher with NO holds or feet in between, so no amount of pulling/lock-off
        // (lock-off ≈ 0.6 m + arm reach ≈ 0.88 m) can bridge it. The climber tops out the low section,
        // reaches full stretch for the gap hold, can't touch it, and comes off the wall.
        static List<RouteDefinition.HoldSpec> TheGap(Vector2 wall)
        {
            var l = new List<RouteDefinition.HoldSpec>();

            // Reachable start.
            Hand(l, -0.4f, 1.3f); Hand(l, 0.4f, 1.7f); Hand(l, -0.3f, 2.2f);
            // THE GAP — next hand hold ~2.0 m above the last, nothing to use in between.
            Hand(l, 0.0f, 4.2f);
            Either(l, 0.0f, 4.9f);
            Finish(l, 0f, wall.y - 0.4f);

            // Feet only under the lower (reachable) section — none to bridge the gap.
            Foot(l, -0.4f, 0.8f); Foot(l, 0.4f, 1.1f); Foot(l, -0.3f, 1.6f); Foot(l, 0.4f, 2.0f);
            return l;
        }
    }
}
