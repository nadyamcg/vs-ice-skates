namespace IceSkates.src.Movement
{
    /// <summary>
    /// display modes for movement metrics overlay
    /// </summary>
    public enum MovementDisplayMode
    {
        /// <summary>simple speed display - just current speed and basic stats</summary>
        Simple,

        /// <summary>comprehensive developer overlay with all metrics</summary>
        Developer,

        /// <summary>session summary - averages, peaks, and analysis</summary>
        SessionSummary,

        /// <summary>ice skating specific metrics - slide, friction, turn efficiency</summary>
        IceSkating,

        /// <summary>minimal - just speed number</summary>
        Minimal,

        /// <summary>custom formatted output</summary>
        Custom
    }

    /// <summary>
    /// what units to display speeds in
    /// </summary>
    public enum SpeedUnit
    {
        /// <summary>meters per second (default for Vintage Story - 1 block = 1m)</summary>
        MetersPerSecond,

        /// <summary>kilometers per hour</summary>
        KilometersPerHour,

        /// <summary>blocks per second (same as m/s in VS)</summary>
        BlocksPerSecond,

        /// <summary>miles per hour</summary>
        MilesPerHour
    }
}
