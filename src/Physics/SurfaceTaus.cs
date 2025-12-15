namespace IceSkates.src.Physics
{
    /// <summary>
    /// Surface friction characteristics - tau values for exponential decay physics
    /// Lower tau = less friction (more slippery)
    /// Higher tau = more friction (more grippy)
    /// </summary>
    public readonly struct SurfaceTaus(double walkUp, double walkDown, double sprintUp, double sprintDown, double noInputDown, double turnStrength)
    {
        /// <summary>Acceleration while walking forward</summary>
        public readonly double WalkUp = walkUp;

        /// <summary>Deceleration when changing direction while walking</summary>
        public readonly double WalkDown = walkDown;

        /// <summary>Acceleration while sprinting</summary>
        public readonly double SprintUp = sprintUp;

        /// <summary>Deceleration when stopping from sprint</summary>
        public readonly double SprintDown = sprintDown;

        /// <summary>Passive friction when no input (coasting)</summary>
        public readonly double NoInputDown = noInputDown;

        /// <summary>Resistance to turning (multiplier on base tau)</summary>
        public readonly double TurnStrength = turnStrength;

        public override string ToString()
        {
            return $"WalkUp:{WalkUp:F2} WalkDown:{WalkDown:F2} SprintUp:{SprintUp:F2} SprintDown:{SprintDown:F2} NoInput:{NoInputDown:F2} Turn:{TurnStrength:F2}";
        }
    }
}
