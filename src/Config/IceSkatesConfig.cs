using System;

namespace IceSkates.src.Config
{
    /// <summary>
    /// modular configuration for ice skating mechanics
    /// ALL values are runtime-adjustable via developer commands for rapid iteration
    /// </summary>
    public class IceSkatesConfig
    {
        // ============================================
        // GENERAL SETTINGS
        // ============================================

        /// <summary>enable debug mode with verbose logging and HUD overlays</summary>
        public bool DebugMode { get; set; } = false;

        /// <summary>enable developer commands for runtime tweaking</summary>
        public bool EnableDevCommands { get; set; } = true;


        // ============================================
        // PHYSICS - FRICTION & MOMENTUM
        // ============================================

        /// <summary>ice surface tau - walk acceleration (lower = more slippery). Default: 1.2</summary>
        public double IceTauWalkUp { get; set; } = 1.2;

        /// <summary>ice surface tau - walk deceleration (lower = slides longer). Default: 1.5</summary>
        public double IceTauWalkDown { get; set; } = 1.5;

        /// <summary>ice surface tau - sprint acceleration (lower = faster speed buildup). Default: 0.4</summary>
        public double IceTauSprintUp { get; set; } = 0.4;

        /// <summary>ice surface tau - sprint deceleration (lower = harder to stop). Default: 2.5</summary>
        public double IceTauSprintDown { get; set; } = 2.5;

        /// <summary>ice surface tau - no input deceleration when walking (passive friction). Default: 2.5</summary>
        public double IceTauNoInputWalk { get; set; } = 2.5;

        /// <summary>ice surface tau - no input deceleration when sprinting (passive friction, higher = longer glide). Default: 8.0</summary>
        public double IceTauNoInputSprint { get; set; } = 8.0;

        /// <summary>ice surface tau - no input deceleration (passive friction). DEPRECATED: Use IceTauNoInputWalk or IceTauNoInputSprint. Default: 2.5</summary>
        public double IceTauNoInput { get; set; } = 2.5;

        /// <summary>ice surface tau - turn resistance multiplier (higher = harder turns). Default: 2.5</summary>
        public double IceTauTurnStrength { get; set; } = 2.5;

        /// <summary>ice surface tau - lateral (strafing) movement friction (higher = more friction). Default: 4.0</summary>
        public double IceTauLateral { get; set; } = 4.0;


        // ============================================
        // PHYSICS - SPEED LIMITS
        // ============================================

        /// <summary>walking skating speed multiplier (relative to vanilla). Default: 1.2 (20% faster)</summary>
        public double WalkSkatingSpeedMultiplier { get; set; } = 1.2;

        /// <summary>sprint skating speed multiplier (relative to vanilla). Default: 1.5 (50% faster)</summary>
        public double SprintSkatingSpeedMultiplier { get; set; } = 1.5;

        /// <summary>maximum skating speed multiplier relative to base walk speed. DEPRECATED: Use WalkSkatingSpeedMultiplier or SprintSkatingSpeedMultiplier. Default: 1.5</summary>
        public double MaxSkatingSpeedMultiplier { get; set; } = 1.5;

        /// <summary>minimum speed (m/s) before momentum physics kick in. Default: 0.05</summary>
        public double MinimumSpeedThreshold { get; set; } = 0.05;


        // ============================================
        // PHYSICS - TURN RESISTANCE
        // ============================================

        /// <summary>speed loss factor during sharp turns (0-1). 0.4 = lose up to 40% speed in 180° turn. Default: 0.4</summary>
        public double TurnSpeedLossFactor { get; set; } = 0.4;

        /// <summary>minimum turn angle (degrees) to trigger speed loss. Default: 30</summary>
        public double TurnAngleThreshold { get; set; } = 30.0;


        // ============================================
        // PHYSICS - LATERAL MOVEMENT
        // ============================================

        /// <summary>speed multiplier for pure lateral (strafing) movement. 0.5 = 50% of forward speed. Default: 0.5</summary>
        public double LateralSpeedMultiplier { get; set; } = 0.5;

        /// <summary>lateral movement ratio threshold (0-1) to trigger penalties. 0.3 = 30% sideways. Default: 0.3</summary>
        public double LateralMovementThreshold { get; set; } = 0.3;


        // ============================================
        // PHYSICS - OFF-ICE PENALTY
        // ============================================

        /// <summary>enable severe movement penalty when wearing skates on non-ice surfaces</summary>
        public bool EnableOffIcePenalty { get; set; } = true;

        /// <summary>walk speed multiplier on non-ice surfaces (0.1 = 10% normal speed). Default: 0.1</summary>
        public double OffIceWalkSpeedMultiplier { get; set; } = 0.1;

        /// <summary>sprint speed multiplier on non-ice surfaces. Default: 0.15</summary>
        public double OffIceSprintSpeedMultiplier { get; set; } = 0.15;

        /// <summary>friction tau on non-ice (higher = more friction, harder to move). Default: 8.0</summary>
        public double OffIceFrictionTau { get; set; } = 8.0;


        // ============================================
        // SURFACE DETECTION
        // ============================================

        /// <summary>how often to check surface type (seconds). lower = more responsive but more CPU intensive. Default: 0.1</summary>
        public double SurfaceCheckInterval { get; set; } = 0.1;

        /// <summary>Y-offset below player feet to check for ice blocks (in blocks). Default: 0.05</summary>
        public double SurfaceCheckYOffset { get; set; } = 0.05;


        // ============================================
        // CAMERA SETTINGS
        // ============================================

        /// <summary>force third-person camera when wearing skates</summary>
        public bool ForceThirdPersonCamera { get; set; } = true;

        /// <summary>allow players to override forced third-person with dev command</summary>
        public bool AllowThirdPersonOverride { get; set; } = true;

        /// <summary>third-person camera distance from player (blocks). Default: 4.0</summary>
        public double ThirdPersonCameraDistance { get; set; } = 4.0;

        /// <summary>camera FOV adjustment during high-speed skating (degrees). Default: 5.0</summary>
        public double HighSpeedFOVIncrease { get; set; } = 5.0;

        /// <summary>speed threshold for FOV increase (m/s). Default: 8.0</summary>
        public double HighSpeedThreshold { get; set; } = 8.0;


        // ============================================
        // STRIDE SYSTEM - RHYTHMIC SKATING
        // ============================================

        /// <summary>enable stride-based rhythmic skating (push/glide phases)</summary>
        public bool EnableStrideSystem { get; set; } = true;

        /// <summary>base stride cycle duration in seconds (walking). Default: 0.8</summary>
        public double BaseStrideDuration { get; set; } = 0.8;

        /// <summary>sprint stride cycle duration in seconds (faster cadence). Default: 0.5</summary>
        public double SprintStrideDuration { get; set; } = 0.5;


        // ============================================
        // LATERAL WOBBLE
        // ============================================

        /// <summary>enable lateral wobble effect during strides. Default: true</summary>
        public bool EnableLateralWobble { get; set; } = true;

        /// <summary>maximum lateral wobble offset in blocks. Default: 0.08</summary>
        public double MaxLateralWobble { get; set; } = 0.08;


        // ============================================
        // CAMERA BOB (STRIDE-SYNCED)
        // ============================================

        /// <summary>enable camera bob synced to stride cycle. Default: true</summary>
        public bool EnableStrideCameraBob { get; set; } = true;

        /// <summary>vertical camera bob amplitude in blocks. Default: 0.04</summary>
        public double CameraBobVertical { get; set; } = 0.04;

        /// <summary>lateral camera sway amplitude in blocks. Default: 0.03</summary>
        public double CameraBobLateral { get; set; } = 0.03;

        /// <summary>camera roll angle amplitude in degrees. Default: 1.5</summary>
        public double CameraBobRoll { get; set; } = 1.5;

        /// <summary>speed at which camera bob reaches full intensity (m/s). Default: 6.0</summary>
        public double CameraBobFullIntensitySpeed { get; set; } = 6.0;

        /// <summary>disable vanilla head bobbing when wearing skates. Default: true</summary>
        public bool DisableVanillaBobWhenSkating { get; set; } = true;


        // ============================================
        // FUTURE: STAMINA (PLACEHOLDER)
        // ============================================

        /// <summary>enable stamina drain while skating (future feature)</summary>
        public bool EnableStamina { get; set; } = false;

        /// <summary>stamina drain per second while skating. Default: 0.5</summary>
        public double StaminaDrainPerSecond { get; set; } = 0.5;

        /// <summary>stamina drain per second while sprint skating. Default: 1.2</summary>
        public double StaminaDrainSprintPerSecond { get; set; } = 1.2;


        // ============================================
        // DEVELOPER TOOLS
        // ============================================

        /// <summary>export config to dedicated log file on change</summary>
        public bool AutoExportConfigOnChange { get; set; } = false;

        /// <summary>show physics debug overlay in HUD (requires DebugMode)</summary>
        public bool ShowPhysicsDebugHUD { get; set; } = false;

        /// <summary>log all physics calculations (VERY verbose, performance impact)</summary>
        public bool LogPhysicsCalculations { get; set; } = false;

        /// <summary>always-on speed readout overlay (m/s). Default: true</summary>
        public bool ShowSpeedOverlay { get; set; } = true;

        /// <summary>use enhanced movement overlay with comprehensive metrics. Default: true</summary>
        public bool UseEnhancedMovementOverlay { get; set; } = true;

        /// <summary>movement overlay display mode. Options: Minimal, Simple, Developer, SessionSummary, IceSkating</summary>
        public string MovementOverlayDisplayMode { get; set; } = "Simple";

        /// <summary>speed unit for display. Options: MetersPerSecond, KilometersPerHour, BlocksPerSecond, MilesPerHour</summary>
        public string MovementOverlaySpeedUnit { get; set; } = "MetersPerSecond";

        /// <summary>movement overlay update interval (seconds). Default: 0.1</summary>
        public double MovementOverlayUpdateInterval { get; set; } = 0.1;

        /// <summary>movement overlay font size. Default: 16</summary>
        public int MovementOverlayFontSize { get; set; } = 16;

        /// <summary>movement overlay X offset. Default: 10</summary>
        public int MovementOverlayOffsetX { get; set; } = 10;

        /// <summary>movement overlay Y offset. Default: 10</summary>
        public int MovementOverlayOffsetY { get; set; } = 10;

        // ============================================
        // HELPER METHODS
        // ============================================

        /// <summary>
        /// get all ice tau values as SurfaceTaus struct (for physics system)
        /// </summary>
        public (double walkUp, double walkDown, double sprintUp, double sprintDown, double noInput, double turnStrength) GetIceTaus()
        {
            return (IceTauWalkUp, IceTauWalkDown, IceTauSprintUp, IceTauSprintDown, IceTauNoInput, IceTauTurnStrength);
        }

        /// <summary>
        /// set all ice tau values at once (for bulk updates)
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
        /// validate configuration values and clamp to reasonable ranges
        /// </summary>
        public void Validate()
        {
            // clamp tau values (0.1 to 20.0 is reasonable range)
            IceTauWalkUp = Math.Clamp(IceTauWalkUp, 0.1, 20.0);
            IceTauWalkDown = Math.Clamp(IceTauWalkDown, 0.1, 20.0);
            IceTauSprintUp = Math.Clamp(IceTauSprintUp, 0.1, 20.0);
            IceTauSprintDown = Math.Clamp(IceTauSprintDown, 0.1, 20.0);
            IceTauNoInput = Math.Clamp(IceTauNoInput, 0.1, 20.0);
            IceTauNoInputWalk = Math.Clamp(IceTauNoInputWalk, 0.1, 20.0);
            IceTauNoInputSprint = Math.Clamp(IceTauNoInputSprint, 0.1, 20.0);
            IceTauTurnStrength = Math.Clamp(IceTauTurnStrength, 0.1, 10.0);
            IceTauLateral = Math.Clamp(IceTauLateral, 0.1, 20.0);

            // clamp speed multipliers
            MaxSkatingSpeedMultiplier = Math.Clamp(MaxSkatingSpeedMultiplier, 0.5, 5.0);
            WalkSkatingSpeedMultiplier = Math.Clamp(WalkSkatingSpeedMultiplier, 0.5, 5.0);
            SprintSkatingSpeedMultiplier = Math.Clamp(SprintSkatingSpeedMultiplier, 0.5, 5.0);
            OffIceWalkSpeedMultiplier = Math.Clamp(OffIceWalkSpeedMultiplier, 0.01, 1.0);
            OffIceSprintSpeedMultiplier = Math.Clamp(OffIceSprintSpeedMultiplier, 0.01, 1.0);
            LateralSpeedMultiplier = Math.Clamp(LateralSpeedMultiplier, 0.1, 1.0);

            // clamp turn parameters
            TurnSpeedLossFactor = Math.Clamp(TurnSpeedLossFactor, 0.0, 1.0);
            TurnAngleThreshold = Math.Clamp(TurnAngleThreshold, 0.0, 180.0);
            LateralMovementThreshold = Math.Clamp(LateralMovementThreshold, 0.0, 1.0);

            // clamp intervals
            SurfaceCheckInterval = Math.Clamp(SurfaceCheckInterval, 0.01, 1.0);

            // camera settings
            ThirdPersonCameraDistance = Math.Clamp(ThirdPersonCameraDistance, 1.0, 20.0);
            HighSpeedFOVIncrease = Math.Clamp(HighSpeedFOVIncrease, 0.0, 30.0);

            // stride system
            BaseStrideDuration = Math.Clamp(BaseStrideDuration, 0.2, 2.0);
            SprintStrideDuration = Math.Clamp(SprintStrideDuration, 0.1, 1.5);

            // lateral wobble
            MaxLateralWobble = Math.Clamp(MaxLateralWobble, 0.0, 0.3);

            // camera bob
            CameraBobVertical = Math.Clamp(CameraBobVertical, 0.0, 0.2);
            CameraBobLateral = Math.Clamp(CameraBobLateral, 0.0, 0.2);
            CameraBobRoll = Math.Clamp(CameraBobRoll, 0.0, 10.0);
            CameraBobFullIntensitySpeed = Math.Clamp(CameraBobFullIntensitySpeed, 1.0, 20.0);
        }
    }
}
