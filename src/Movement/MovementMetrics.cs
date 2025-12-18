using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace IceSkates.src.Movement
{
    /// <summary>
    /// comprehensive movement statistics tracking for player entities.
    /// tracks velocity, acceleration, distance, efficiency, and more.
    /// designed to be used both for ice skating analysis and general movement debugging.
    /// </summary>
    public class MovementMetrics
    {
        // ============================================
        // INSTANTANEOUS VALUES
        // ============================================

        /// <summary>current velocity vector (m/s) - smoothed for accuracy</summary>
        public Vec3d Velocity { get; private set; } = new Vec3d();

        /// <summary>current velocity magnitude (m/s)</summary>
        public double Speed => Velocity.Length();

        /// <summary>current horizontal (XZ) velocity magnitude (m/s)</summary>
        public double HorizontalSpeed => Math.Sqrt(Velocity.X * Velocity.X + Velocity.Z * Velocity.Z);

        // velocity smoothing to reduce measurement noise
        private readonly Queue<Vec3d> _velocityHistory = new();
        private const int VelocitySmoothingWindow = 10; // average over 10 samples (~500ms at 50ms intervals)

        /// <summary>current vertical (Y) velocity (m/s)</summary>
        public double VerticalSpeed => Math.Abs(Velocity.Y);

        /// <summary>current acceleration vector (m/s²)</summary>
        public Vec3d Acceleration { get; private set; } = new Vec3d();

        /// <summary>current acceleration magnitude (m/s²)</summary>
        public double AccelerationMagnitude => Acceleration.Length();

        /// <summary>current horizontal acceleration magnitude (m/s²)</summary>
        public double HorizontalAcceleration => Math.Sqrt(Acceleration.X * Acceleration.X + Acceleration.Z * Acceleration.Z);


        // ============================================
        // PEAK VALUES
        // ============================================

        /// <summary>peak speed reached in this session (m/s)</summary>
        public double PeakSpeed { get; private set; }

        /// <summary>peak horizontal speed reached (m/s)</summary>
        public double PeakHorizontalSpeed { get; private set; }

        /// <summary>peak vertical speed reached (m/s)</summary>
        public double PeakVerticalSpeed { get; private set; }

        /// <summary>peak acceleration magnitude (m/s²)</summary>
        public double PeakAcceleration { get; private set; }

        /// <summary>timestamp when peak speed was reached</summary>
        public DateTime PeakSpeedTime { get; private set; } = DateTime.MinValue;


        // ============================================
        // DISTANCE TRACKING
        // ============================================

        /// <summary>total distance traveled (m)</summary>
        public double TotalDistance { get; private set; }

        /// <summary>total horizontal distance (m)</summary>
        public double TotalHorizontalDistance { get; private set; }

        /// <summary>distance traveled on X axis (m)</summary>
        public double DistanceX { get; private set; }

        /// <summary>distance traveled on Y axis (m)</summary>
        public double DistanceY { get; private set; }

        /// <summary>distance traveled on Z axis (m)</summary>
        public double DistanceZ { get; private set; }


        // ============================================
        // AVERAGING & HISTORY
        // ============================================

        /// <summary>average speed over entire session (m/s)</summary>
        public double AverageSpeed => _sessionTime > 0 ? TotalDistance / _sessionTime : 0;

        /// <summary>average horizontal speed over session (m/s)</summary>
        public double AverageHorizontalSpeed => _sessionTime > 0 ? TotalHorizontalDistance / _sessionTime : 0;

        /// <summary>rolling average speed (configurable window)</summary>
        public double RollingAverageSpeed { get; private set; }

        private readonly Queue<(double speed, double time)> _speedHistory = new();
        private readonly Queue<double> _accelerationHistory = new();

        /// <summary>time window for rolling averages (seconds)</summary>
        public double RollingWindowSeconds { get; set; } = 2.0;


        // ============================================
        // sESSION TRACKING
        // ============================================

        /// <summary>total session time (seconds)</summary>
        private double _sessionTime;

        /// <summary>session start time</summary>
        public DateTime SessionStartTime { get; private set; } = DateTime.Now;

        /// <summary>session duration</summary>
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;


        // ============================================
        // POSITION TRACKING
        // ============================================

        private readonly Vec3d _lastPosition = new(double.NaN, double.NaN, double.NaN);
        private readonly Vec3d _lastVelocity = new();
        private DateTime _lastUpdateTime = DateTime.Now;


        // ============================================
        // EFFICIENCY & ANALYSIS
        // ============================================

        /// <summary>
        /// movement efficiency: ratio of direct path distance to actual traveled distance.
        /// 100% = perfectly straight line. lower = more meandering.
        /// </summary>
        public double MovementEfficiency
        {
            get
            {
                if (TotalDistance < 0.1) return 100.0;

                double directDistance = (_lastPosition - _sessionStartPosition).Length();
                return Math.Min(100.0, (directDistance / TotalDistance) * 100.0);
            }
        }

        private readonly Vec3d _sessionStartPosition = new();


        // ============================================
        // SLIDING & MOMENTUM (ICE SKATING SPECIFIC)
        // ============================================

        /// <summary>distance slid without input (m)</summary>
        public double SlideDistance { get; private set; }

        /// <summary>is currently sliding (no input but still moving)</summary>
        public bool IsSliding { get; private set; }

        private bool _hadInputLastFrame = true;
        private readonly Vec3d _slideStartPosition = new();


        // ============================================
        // FRICTION & DECELERATION ANALYSIS
        // ============================================

        /// <summary>estimated friction coefficient (derived from deceleration)</summary>
        public double EstimatedFriction { get; private set; }

        /// <summary>current deceleration rate when no input (m/s²)</summary>
        public double DecelerationRate { get; private set; }


        // ============================================
        // TURNING ANALYSIS
        // ============================================

        /// <summary>current turn rate (degrees/second)</summary>
        public double TurnRate { get; private set; }

        /// <summary>current yaw angle (0-360 degrees)</summary>
        public double CurrentYaw { get; private set; }

        /// <summary>speed lost during turns (m/s)</summary>
        public double TurnSpeedLoss { get; private set; }

        private double _lastYaw = double.NaN;
        private double _speedBeforeTurn = 0;


        // ============================================
        // PUBLIC METHODS
        // ============================================

        /// <summary>
        /// update metrics with new position and input state
        /// </summary>
        public void Update(Vec3d currentPosition, bool hasInput, double yaw, double deltaTime)
        {
            if (double.IsNaN(_lastPosition.X))
            {
                // first update - initialize
                _lastPosition.Set(currentPosition);
                _sessionStartPosition.Set(currentPosition);
                _lastUpdateTime = DateTime.Now;
                _lastYaw = yaw;
                return;
            }

            // calculate instantaneous velocity
            Vec3d displacement = currentPosition - _lastPosition;
            double distance = displacement.Length();
            double horizontalDist = Math.Sqrt(displacement.X * displacement.X + displacement.Z * displacement.Z);

            // calculate raw velocity
            Vec3d rawVelocity = new(
                displacement.X / deltaTime,
                displacement.Y / deltaTime,
                displacement.Z / deltaTime
            );

            // apply velocity smoothing to reduce measurement noise and spikes
            _velocityHistory.Enqueue(rawVelocity);
            if (_velocityHistory.Count > VelocitySmoothingWindow)
            {
                _velocityHistory.Dequeue();
            }

            // calculate smoothed velocity (average of recent samples)
            double avgX = 0, avgY = 0, avgZ = 0;
            int count = _velocityHistory.Count;
            foreach (var vel in _velocityHistory)
            {
                avgX += vel.X;
                avgY += vel.Y;
                avgZ += vel.Z;
            }
            Velocity.Set(avgX / count, avgY / count, avgZ / count);

            // calculate acceleration
            Vec3d velocityChange = Velocity - _lastVelocity;
            Acceleration.Set(velocityChange.X / deltaTime, velocityChange.Y / deltaTime, velocityChange.Z / deltaTime);

            // update distances
            TotalDistance += distance;
            TotalHorizontalDistance += horizontalDist;
            DistanceX += Math.Abs(displacement.X);
            DistanceY += Math.Abs(displacement.Y);
            DistanceZ += Math.Abs(displacement.Z);

            // update peaks using SMOOTHED velocity to prevent spurious spikes
            // raw velocity can spike due to position jitter or timing issues
            double smoothedSpeed = Speed;
            double smoothedHorizontalSpeed = HorizontalSpeed;

            // only update peaks when speed is above threshold (avoid measurement noise)
            if (smoothedSpeed > 0.01)
            {
                if (smoothedSpeed > PeakSpeed)
                {
                    PeakSpeed = smoothedSpeed;
                    PeakSpeedTime = DateTime.Now;
                }
                PeakHorizontalSpeed = Math.Max(PeakHorizontalSpeed, smoothedHorizontalSpeed);
                PeakVerticalSpeed = Math.Max(PeakVerticalSpeed, VerticalSpeed);
            }

            // acceleration peaks use smoothed values (already dampened)
            PeakAcceleration = Math.Max(PeakAcceleration, AccelerationMagnitude);

            // update session time
            _sessionTime += deltaTime;

            // update rolling averages (using smoothedSpeed from above)
            UpdateRollingAverages(smoothedSpeed, deltaTime);

            // sliding detection
            UpdateSlidingMetrics(hasInput, currentPosition, smoothedSpeed);

            // friction estimation
            UpdateFrictionEstimate(hasInput, deltaTime);

            // turn analysis
            UpdateTurnMetrics(yaw, smoothedSpeed, deltaTime);

            // store for next update
            _lastPosition.Set(currentPosition);
            _lastVelocity.Set(Velocity);
            _lastUpdateTime = DateTime.Now;
            _hadInputLastFrame = hasInput;
        }

        /// <summary>
        /// reset all metrics to initial state (start new session)
        /// </summary>
        public void Reset()
        {
            Velocity.Set(0, 0, 0);
            Acceleration.Set(0, 0, 0);

            PeakSpeed = 0;
            PeakHorizontalSpeed = 0;
            PeakVerticalSpeed = 0;
            PeakAcceleration = 0;
            PeakSpeedTime = DateTime.MinValue;

            TotalDistance = 0;
            TotalHorizontalDistance = 0;
            DistanceX = 0;
            DistanceY = 0;
            DistanceZ = 0;

            SlideDistance = 0;
            CurrentYaw = 0;
            TurnRate = 0;
            TurnSpeedLoss = 0;

            _sessionTime = 0;
            _speedHistory.Clear();
            _accelerationHistory.Clear();
            _velocityHistory.Clear();

            _lastPosition.Set(double.NaN, double.NaN, double.NaN);
            _lastVelocity.Set(0, 0, 0);
            _sessionStartPosition.Set(0, 0, 0);

            SessionStartTime = DateTime.Now;
            _lastUpdateTime = DateTime.Now;
            _lastYaw = double.NaN;
        }

        /// <summary>
        /// reset only peak values (useful for tracking multiple runs)
        /// </summary>
        public void ResetPeaks()
        {
            PeakSpeed = Speed;
            PeakHorizontalSpeed = HorizontalSpeed;
            PeakVerticalSpeed = VerticalSpeed;
            PeakAcceleration = AccelerationMagnitude;
            PeakSpeedTime = DateTime.Now;
        }


        // ============================================
        // PRIVATE HELPER METHODS
        // ============================================

        private void UpdateRollingAverages(double currentSpeed, double deltaTime)
        {
            _speedHistory.Enqueue((currentSpeed, deltaTime));

            // remove old entries outside the window
            double totalTime = 0;
            while (_speedHistory.Count > 0)
            {
                totalTime += _speedHistory.Peek().time;
                if (totalTime > RollingWindowSeconds)
                {
                    _speedHistory.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // calculate rolling average
            if (_speedHistory.Count > 0)
            {
                double sum = _speedHistory.Sum(x => x.speed * x.time);
                double timeSum = _speedHistory.Sum(x => x.time);
                RollingAverageSpeed = timeSum > 0 ? sum / timeSum : 0;
            }
        }

        private void UpdateSlidingMetrics(bool hasInput, Vec3d currentPosition, double currentSpeed)
        {
            bool wasSliding = IsSliding;
            IsSliding = !hasInput && currentSpeed > 0.05; // sliding if moving without input

            if (IsSliding && !wasSliding)
            {
                // started sliding
                _slideStartPosition.Set(currentPosition);
            }
            else if (!IsSliding && wasSliding)
            {
                // stopped sliding
                double slideDist = (currentPosition - _slideStartPosition).Length();
                SlideDistance += slideDist;
            }
        }

        private void UpdateFrictionEstimate(bool hasInput, double deltaTime)
        {
            if (!hasInput && HorizontalSpeed > 0.1)
            {
                // when no input, deceleration is primarily from friction
                DecelerationRate = Math.Abs(HorizontalAcceleration);

                // f = ma, friction coefficient µ ≈ a/g (simplified)
                const double gravity = 9.81;
                EstimatedFriction = DecelerationRate / gravity;
            }
        }

        private void UpdateTurnMetrics(double currentYaw, double currentSpeed, double deltaTime)
        {
            // normalize yaw to 0-360 range
            CurrentYaw = ((currentYaw % 360) + 360) % 360;

            if (double.IsNaN(_lastYaw))
            {
                _lastYaw = currentYaw;
                _speedBeforeTurn = currentSpeed;
                return;
            }

            double yawDelta = currentYaw - _lastYaw;

            // normalize delta to -180 to 180
            while (yawDelta > 180) yawDelta -= 360;
            while (yawDelta < -180) yawDelta += 360;

            double absTurnAngle = Math.Abs(yawDelta);

            if (absTurnAngle > 0.1) // turning threshold
            {
                TurnRate = absTurnAngle / deltaTime;

                // track speed loss during turn
                if (currentSpeed < _speedBeforeTurn)
                {
                    TurnSpeedLoss += (_speedBeforeTurn - currentSpeed);
                }

                _speedBeforeTurn = currentSpeed;
            }
            else
            {
                TurnRate = 0;
                _speedBeforeTurn = Math.Max(_speedBeforeTurn, currentSpeed);
            }

            _lastYaw = currentYaw;
        }
    }
}
