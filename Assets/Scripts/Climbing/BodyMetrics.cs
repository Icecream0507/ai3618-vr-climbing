namespace VRClimb.Climbing
{
    /// <summary>
    /// Shared, roughly anatomical body dimensions (metres) used to constrain reach — so the game,
    /// the simulated climber and the demo avatar all agree on how far a hand/foot can reach. These
    /// limits are what make the wall a *puzzle*: you can't grab a far hold without first moving your
    /// body into range, exactly like real bouldering. Values are for an ~1.75 m climber.
    /// </summary>
    public static class BodyMetrics
    {
        public const float ShoulderDrop = 0.24f;   // shoulder below the head (HMD)
        public const float ShoulderHalf = 0.19f;   // half shoulder width
        public const float HipDrop      = 0.82f;   // hips below the head
        public const float HipHalf      = 0.12f;

        public const float UpperArm = 0.30f;
        public const float ForeArm  = 0.28f;
        public const float ArmReach = UpperArm + ForeArm;   // shoulder -> hand, ~0.58 (a touch of slack added at call sites)

        public const float Thigh    = 0.45f;
        public const float Shin     = 0.43f;
        public const float LegReach = Thigh + Shin;         // hip -> foot, ~0.88
    }
}
