namespace IceSkates.src.Physics
{
    /// <summary>
    /// surface friction characteristics - tau values for exponential decay physics
    /// lower tau = less friction (more slippery)
    /// higher tau = more friction (more grippy)
    /// </summary>
    public readonly struct SurfaceTaus(double walkUp, double walkDown, double sprintUp, double sprintDown, double noInputDown, double turnStrength, double lateral = 4.0)
    {
        /// <summary>acceleration while walking forward</summary>
        public readonly double WalkUp = walkUp;

        /// <summary>deceleration when changing direction while walking</summary>
        public readonly double WalkDown = walkDown;

        /// <summary>acceleration while sprinting</summary>
        public readonly double SprintUp = sprintUp;

        /// <summary>deceleration when stopping from sprint</summary>
        public readonly double SprintDown = sprintDown;

        /// <summary>passive friction when no input (coasting)</summary>
        public readonly double NoInputDown = noInputDown;

        /// <summary>resistance to turning (multiplier on base tau)</summary>
        public readonly double TurnStrength = turnStrength;

        /// <summary>friction for lateral (strafing) movement</summary>
        public readonly double Lateral = lateral;

        public override string ToString()
        {
            return $"WalkUp:{WalkUp:F2} WalkDown:{WalkDown:F2} SprintUp:{SprintUp:F2} SprintDown:{SprintDown:F2} NoInput:{NoInputDown:F2} Turn:{TurnStrength:F2} Lateral:{Lateral:F2}";
        }
    }
}
