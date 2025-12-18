using System;
using IceSkates.src.Config;
using IceSkates.src.Physics;

namespace IceSkates.src.CameraControl
{
    /// <summary>
    /// applies camera bob synchronized to stride cycle for immersive skating feel.
    /// produces vertical bob, lateral sway. should be expanded in the future.
    /// </summary>
    public class CameraBobController
    {
        /// <summary>current vertical bob offset (in blocks)</summary>
        public double VerticalOffset { get; private set; }

        /// <summary>current lateral sway offset (in blocks)</summary>
        public double LateralOffset { get; private set; }

        /// <summary>current roll angle in radians</summary>
        public double RollAngle { get; private set; }

        /// <summary>current intensity multiplier (0-1 based on speed)</summary>
        public double Intensity { get; private set; }

        // smoothing for gradual transitions
        private double _smoothedIntensity;
        private double _smoothedPhase;

        // constants for smoothing
        private const double IntensitySmoothingRate = 5.0; // how fast intensity changes
        private const double PhaseSmoothingRate = 0.1; // minimal phase smoothing to avoid jitter

        /// <summary>
        /// update camera bob based on stride state
        /// </summary>
        /// <param name="entityId">entity ID to get stride state from</param>
        /// <param name="currentSpeed">current horizontal speed in m/s</param>
        /// <param name="dt">delta time in seconds</param>
        /// <param name="config">config reference</param>
        public void Update(long entityId, double currentSpeed, double dt, IceSkatesConfig config)
        {
            if (!config.EnableStrideCameraBob)
            {
                Reset();
                return;
            }

            // get stride state
            var strideState = StridePhysicsSystem.GetStrideState(entityId);
            if (strideState == null || !strideState.Value.IsActivelyStriding)
            {
                // fade out bob when not striding
                _smoothedIntensity *= (1.0 - dt * IntensitySmoothingRate);
                if (_smoothedIntensity < 0.01)
                {
                    Reset();
                    return;
                }
            }
            else
            {
                var state = strideState.Value;

                // calculate target intensity based on speed
                double targetIntensity = Math.Min(1.0, currentSpeed / config.CameraBobFullIntensitySpeed);

                // smooth intensity changes
                _smoothedIntensity += (targetIntensity - _smoothedIntensity) * dt * IntensitySmoothingRate;

                // get cycle phase (0-1)
                double cyclePhase = state.CyclePhase;

                // smooth phase to reduce jitter
                double phaseDiff = cyclePhase - _smoothedPhase;
                // handle wrap-around
                if (phaseDiff > 0.5) phaseDiff -= 1.0;
                if (phaseDiff < -0.5) phaseDiff += 1.0;
                _smoothedPhase += phaseDiff * (1.0 - Math.Pow(PhaseSmoothingRate, dt * 60));
                if (_smoothedPhase > 1.0) _smoothedPhase -= 1.0;
                if (_smoothedPhase < 0.0) _smoothedPhase += 1.0;
            }

            Intensity = _smoothedIntensity;

            // calculate bob offsets using sinusoidal waves
            // use smoothed phase for calculations
            double phaseRadians = _smoothedPhase * Math.PI * 2.0;

            // vertical bob: two full cycles per stride
            // creates the up-down motion of skating
            double verticalWave = Math.Sin(phaseRadians * 2.0);
            VerticalOffset = verticalWave * config.CameraBobVertical * Intensity;

            // lateral sway: one full cycle per stride
            // negative sign to sway opposite to push direction
            double lateralWave = -Math.Sin(phaseRadians);
            LateralOffset = lateralWave * config.CameraBobLateral * Intensity;

            // roll: follows lateral sway for natural head tilt
            // convert degrees to radians
            double rollDegrees = lateralWave * config.CameraBobRoll * Intensity;
            RollAngle = rollDegrees * Math.PI / 180.0;
        }

        /// <summary>
        /// reset all bob values to zero
        /// </summary>
        public void Reset()
        {
            VerticalOffset = 0;
            LateralOffset = 0;
            RollAngle = 0;
            Intensity = 0;
            _smoothedIntensity = 0;
        }

        /// <summary>
        /// get bob values as a tuple for easy application
        /// </summary>
        public (double vertical, double lateral, double roll) GetBobValues()
        {
            return (VerticalOffset, LateralOffset, RollAngle);
        }
    }
}
