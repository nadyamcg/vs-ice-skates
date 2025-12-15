using System;

namespace IceSkates
{
    /// <summary>
    /// Modular configuration for ice skating mechanics
    /// ALL values are runtime-adjustable via developer commands for rapid iteration
    /// </summary>
    public class IceSkatesConfig
    {
        // ============================================
        // GENERAL SETTINGS
        // ============================================

        /// <summary>Enable debug mode with verbose logging and HUD overlays</summary>
        public bool DebugMode { get; set; } = false;

        /// <summary>Enable developer commands for runtime tweaking</summary>
        public bool EnableDevCommands { get; set; } = true;


        // ============================================
        // PHYSICS - FRICTION & MOMENTUM
        // ============================================

        /// <summary>Ice surface tau - walk acceleration (lower = more slippery). Default: 0.8</summary>
        public double IceTauWalkUp { get; set; } = 0.8;

        /// <summary>Ice surface tau - walk deceleration (lower = slides longer). Default: 1.2</summary>
        public double IceTauWalkDown { get; set; } = 1.2;

        /// <summary>Ice surface tau - sprint acceleration (lower = faster speed buildup). Default: 0.6</summary>
        public double IceTauSprintUp { get; set; } = 0.6;

        /// <summary>Ice surface tau - sprint deceleration (lower = harder to stop). Default: 1.0</summary>
        public double IceTauSprintDown { get; set; } = 1.0;

        /// <summary>Ice surface tau - no input deceleration (passive friction). Default: 1.5</summary>
        public double IceTauNoInput { get; set; } = 1.5;

        /// <summary>Ice surface tau - turn resistance (lower = easier turns). Default: 0.8</summary>
        public double IceTauTurnStrength { get; set; } = 0.8;


        // ============================================
        // PHYSICS - SPEED LIMITS
        // ============================================

        /// <summary>Maximum skating speed multiplier relative to base walk speed. Default: 1.5 (1.5x running speed)</summary>
        public double MaxSkatingSpeedMultiplier { get; set; } = 1.5;

        /// <summary>Minimum speed (m/s) before momentum physics kick in. Default: 0.05</summary>
        public double MinimumSpeedThreshold { get; set; } = 0.05;


        // ============================================
        // PHYSICS - OFF-ICE PENALTY (The Bear Scenario)
        // ============================================

        /// <summary>Enable severe movement penalty when wearing skates on non-ice surfaces</summary>
        public bool EnableOffIcePenalty { get; set; } = true;

        /// <summary>Walk speed multiplier on non-ice surfaces (0.1 = 10% normal speed). Default: 0.1</summary>
        public double OffIceWalkSpeedMultiplier { get; set; } = 0.1;

        /// <summary>Sprint speed multiplier on non-ice surfaces. Default: 0.15</summary>
        public double OffIceSprintSpeedMultiplier { get; set; } = 0.15;

        /// <summary>Friction tau on non-ice (higher = more friction, harder to move). Default: 8.0</summary>
        public double OffIceFrictionTau { get; set; } = 8.0;


        // ============================================
        // SURFACE DETECTION
        // ============================================

        /// <summary>How often to check surface type (seconds). Lower = more responsive but more CPU intensive. Default: 0.1</summary>
        public double SurfaceCheckInterval { get; set; } = 0.1;

        /// <summary>Y-offset below player feet to check for ice blocks (in blocks). Default: 0.05</summary>
        public double SurfaceCheckYOffset { get; set; } = 0.05;


        // ============================================
        // EQUIPMENT DETECTION
        // ============================================

        /// <summary>Require both feet to have skates equipped, or just one?</summary>
        public bool RequireBothSkates { get; set; } = true;


        // ============================================
        // CAMERA SETTINGS
        // ============================================

        /// <summary>Force third-person camera when wearing skates</summary>
        public bool ForceThirdPersonCamera { get; set; } = true;

        /// <summary>Allow players to override forced third-person with dev command</summary>
        public bool AllowThirdPersonOverride { get; set; } = true;

        /// <summary>Third-person camera distance from player (blocks). Default: 4.0</summary>
        public double ThirdPersonCameraDistance { get; set; } = 4.0;

        /// <summary>Camera FOV adjustment during high-speed skating (degrees). Default: 5.0</summary>
        public double HighSpeedFOVIncrease { get; set; } = 5.0;

        /// <summary>Speed threshold for FOV increase (m/s). Default: 8.0</summary>
        public double HighSpeedThreshold { get; set; } = 8.0;


        // ============================================
        // FUTURE: STAMINA (PLACEHOLDER)
        // ============================================

        /// <summary>Enable stamina drain while skating (future feature)</summary>
        public bool EnableStamina { get; set; } = false;

        /// <summary>Stamina drain per second while skating. Default: 0.5</summary>
        public double StaminaDrainPerSecond { get; set; } = 0.5;

        /// <summary>Stamina drain per second while sprint skating. Default: 1.2</summary>
        public double StaminaDrainSprintPerSecond { get; set; } = 1.2;


        // ============================================
        // DEVELOPER TOOLS
        // ============================================

        /// <summary>Export config to dedicated log file on change</summary>
        public bool AutoExportConfigOnChange { get; set; } = false;

        /// <summary>Show physics debug overlay in HUD (requires DebugMode)</summary>
        public bool ShowPhysicsDebugHUD { get; set; } = false;

        /// <summary>Log all physics calculations (VERY verbose, performance impact)</summary>
        public bool LogPhysicsCalculations { get; set; } = false;


        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// Get all ice tau values as SurfaceTaus struct (for physics system)
        /// </summary>
        public (double walkUp, double walkDown, double sprintUp, double sprintDown, double noInput, double turnStrength) GetIceTaus()
        {
            return (IceTauWalkUp, IceTauWalkDown, IceTauSprintUp, IceTauSprintDown, IceTauNoInput, IceTauTurnStrength);
        }

        /// <summary>
        /// Set all ice tau values at once (for bulk updates)
        /// </summary>
        public void SetIceTaus(double walkUp, double walkDown, double sprintUp, double sprintDown, double noInput, double turnStrength)
        {
            IceTauWalkUp = walkUp;
            IceTauWalkDown = walkDown;
            IceTauSprintUp = sprintUp;
            IceTauSprintDown = sprintDown;
            IceTauNoInput = noInput;
            IceTauTurnStrength = turnStrength;
        }

        /// <summary>
        /// Validate configuration values and clamp to reasonable ranges
        /// </summary>
        public void Validate()
        {
            // Clamp tau values (0.1 to 20.0 is reasonable range)
            IceTauWalkUp = Math.Clamp(IceTauWalkUp, 0.1, 20.0);
            IceTauWalkDown = Math.Clamp(IceTauWalkDown, 0.1, 20.0);
            IceTauSprintUp = Math.Clamp(IceTauSprintUp, 0.1, 20.0);
            IceTauSprintDown = Math.Clamp(IceTauSprintDown, 0.1, 20.0);
            IceTauNoInput = Math.Clamp(IceTauNoInput, 0.1, 20.0);
            IceTauTurnStrength = Math.Clamp(IceTauTurnStrength, 0.1, 10.0);

            // Clamp speed multipliers
            MaxSkatingSpeedMultiplier = Math.Clamp(MaxSkatingSpeedMultiplier, 0.5, 5.0);
            OffIceWalkSpeedMultiplier = Math.Clamp(OffIceWalkSpeedMultiplier, 0.01, 1.0);
            OffIceSprintSpeedMultiplier = Math.Clamp(OffIceSprintSpeedMultiplier, 0.01, 1.0);

            // Clamp intervals
            SurfaceCheckInterval = Math.Clamp(SurfaceCheckInterval, 0.01, 1.0);

            // Camera settings
            ThirdPersonCameraDistance = Math.Clamp(ThirdPersonCameraDistance, 1.0, 20.0);
            HighSpeedFOVIncrease = Math.Clamp(HighSpeedFOVIncrease, 0.0, 30.0);
        }
    }
}
