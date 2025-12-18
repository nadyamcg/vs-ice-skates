using System;
using System.Text;

namespace IceSkates.src.Movement
{
    /// <summary>
    /// formats MovementMetrics data for display in different modes
    /// </summary>
    public static class MovementDisplayFormatter
    {
        /// <summary>
        /// format metrics according to display mode
        /// </summary>
        public static string Format(MovementMetrics metrics, MovementDisplayMode mode, SpeedUnit unit = SpeedUnit.MetersPerSecond)
        {
            return mode switch
            {
                MovementDisplayMode.Minimal => FormatMinimal(metrics, unit),
                MovementDisplayMode.Simple => FormatSimple(metrics, unit),
                MovementDisplayMode.Developer => FormatDeveloper(metrics, unit),
                MovementDisplayMode.SessionSummary => FormatSessionSummary(metrics, unit),
                MovementDisplayMode.IceSkating => FormatIceSkating(metrics, unit),
                _ => FormatSimple(metrics, unit)
            };
        }

        /// <summary>
        /// convert speed to specified unit
        /// </summary>
        public static double ConvertSpeed(double mps, SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.MetersPerSecond => mps,
                SpeedUnit.BlocksPerSecond => mps,
                SpeedUnit.KilometersPerHour => mps * 3.6,
                SpeedUnit.MilesPerHour => mps * 2.23694,
                _ => mps
            };
        }

        /// <summary>
        /// get unit string abbreviation
        /// </summary>
        public static string GetUnitString(SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.MetersPerSecond => "m/s",
                SpeedUnit.BlocksPerSecond => "b/s",
                SpeedUnit.KilometersPerHour => "km/h",
                SpeedUnit.MilesPerHour => "mph",
                _ => "m/s"
            };
        }

        /// <summary>
        /// format speed with unit
        /// </summary>
        private static string FormatSpeed(double mps, SpeedUnit unit, int decimals = 1)
        {
            double converted = ConvertSpeed(mps, unit);
            return $"{converted.ToString($"F{decimals}")} {GetUnitString(unit)}";
        }

        /// <summary>
        /// minimal mode: just the speed number
        /// example: "12.4 m/s"
        /// </summary>
        private static string FormatMinimal(MovementMetrics metrics, SpeedUnit unit)
        {
            return FormatSpeed(metrics.HorizontalSpeed, unit, 1);
        }

        /// <summary>
        /// simple mode: current speed with peak and average
        /// example:
        /// SPD: 12.4 m/s (44.6 km/h) ▲+0.8
        /// PEAK: 14.2 m/s  AVG: 10.3 m/s
        /// DIST: 342.1 m   SLIDE: 8.2 m
        /// </summary>
        private static string FormatSimple(MovementMetrics metrics, SpeedUnit unit)
        {
            var sb = new StringBuilder();

            double currentSpeed = metrics.HorizontalSpeed;
            double accel = metrics.HorizontalAcceleration;
            string accelIndicator = accel > 0.1 ? "▲" : accel < -0.1 ? "▼" : "─";
            string accelSign = accel >= 0 ? "+" : "";

            // line 1: current speed with acceleration
            sb.AppendLine($"SPD: {FormatSpeed(currentSpeed, unit)} {accelIndicator}{accelSign}{accel:F1}");

            // line 2: peak and average
            sb.AppendLine($"PEAK: {FormatSpeed(metrics.PeakHorizontalSpeed, unit)}  AVG: {FormatSpeed(metrics.AverageHorizontalSpeed, unit)}");

            // line 3: distance and slide
            sb.Append($"DIST: {metrics.TotalHorizontalDistance:F1} m");
            if (metrics.SlideDistance > 0.1)
            {
                sb.Append($"   SLIDE: {metrics.SlideDistance:F1} m");
            }

            return sb.ToString();
        }

        /// <summary>
        /// developer mode: comprehensive technical overlay
        /// example:
        /// TIME: 34.2s  |  DIST: 421.3m
        /// VEL: X=8.4 Y=5.2 Z=0.1 | MAG=9.8
        /// ACC: X=+0.3 Y=-0.1 Z=0.0 | MAG=+0.32
        /// EFFICIENCY: 87%  |  DRIFT: 2.4m
        /// FRICTION: 0.08  |  TURN: 12.3°/s
        /// </summary>
        private static string FormatDeveloper(MovementMetrics metrics, SpeedUnit unit)
        {
            var sb = new StringBuilder();

            // line 1: time and distance
            sb.AppendLine($"TIME: {metrics.SessionDuration.TotalSeconds:F1}s  |  DIST: {metrics.TotalDistance:F1}m");

            // line 2: velocity components
            sb.AppendLine($"VEL: X={metrics.Velocity.X:F1} Y={metrics.Velocity.Y:F1} Z={metrics.Velocity.Z:F1} | MAG={metrics.Speed:F1}");

            // line 3: acceleration components
            string accX = metrics.Acceleration.X >= 0 ? $"+{metrics.Acceleration.X:F1}" : $"{metrics.Acceleration.X:F1}";
            string accY = metrics.Acceleration.Y >= 0 ? $"+{metrics.Acceleration.Y:F1}" : $"{metrics.Acceleration.Y:F1}";
            string accZ = metrics.Acceleration.Z >= 0 ? $"+{metrics.Acceleration.Z:F1}" : $"{metrics.Acceleration.Z:F1}";
            string accMag = metrics.AccelerationMagnitude >= 0 ? $"+{metrics.AccelerationMagnitude:F2}" : $"{metrics.AccelerationMagnitude:F2}";
            sb.AppendLine($"ACC: X={accX} Y={accY} Z={accZ} | MAG={accMag}");

            // line 4: efficiency and drift
            double drift = metrics.TotalDistance - (metrics.TotalDistance * metrics.MovementEfficiency / 100.0);
            sb.AppendLine($"EFFICIENCY: {metrics.MovementEfficiency:F0}%  |  DRIFT: {drift:F1}m");

            // line 5: friction and turn rate
            sb.Append($"FRICTION: {metrics.EstimatedFriction:F2}  |  TURN: {metrics.TurnRate:F1}°/s");

            return sb.ToString();
        }

        /// <summary>
        /// session summary mode: comprehensive analysis
        /// example:
        /// === MOVEMENT SESSION ANALYSIS ===
        /// Duration: 2:34 | Distance: 812.4m
        /// Avg Speed: 5.26 m/s | Peak: 14.8 m/s
        /// Slide Efficiency: 84% | Turn Loss: 12%
        /// Path Efficiency: 94% | Jitter: Low
        /// </summary>
        private static string FormatSessionSummary(MovementMetrics metrics, SpeedUnit unit)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== MOVEMENT SESSION ===");

            // duration and distance
            TimeSpan duration = metrics.SessionDuration;
            string durationStr = duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
                : $"{duration.Minutes}:{duration.Seconds:D2}";

            sb.AppendLine($"Duration: {durationStr} | Distance: {metrics.TotalDistance:F1}m");

            // speed analysis
            sb.AppendLine($"Avg Speed: {FormatSpeed(metrics.AverageSpeed, unit)} | Peak: {FormatSpeed(metrics.PeakSpeed, unit)}");

            // efficiency metrics
            double slideEfficiency = metrics.TotalDistance > 0 ? (metrics.SlideDistance / metrics.TotalDistance) * 100 : 0;
            double turnLossPercent = metrics.PeakSpeed > 0 ? (metrics.TurnSpeedLoss / metrics.PeakSpeed) * 100 : 0;

            sb.AppendLine($"Slide: {metrics.SlideDistance:F1}m ({slideEfficiency:F0}%) | Turn Loss: {turnLossPercent:F0}%");

            // path efficiency
            sb.Append($"Path Efficiency: {metrics.MovementEfficiency:F0}%");

            return sb.ToString();
        }

        /// <summary>
        /// ice skating specific mode
        /// example:
        /// SPEED: 12.4 m/s | SLIDE: 8.2m
        /// FRICTION: 0.08 | CONTROL: 92%
        /// TURN ANGLE: 124° | LOSS: 2.1 m/s
        /// </summary>
        private static string FormatIceSkating(MovementMetrics metrics, SpeedUnit unit)
        {
            var sb = new StringBuilder();

            // line 1: speed and slide
            sb.AppendLine($"SPEED: {FormatSpeed(metrics.HorizontalSpeed, unit)} | SLIDE: {metrics.SlideDistance:F1}m");

            // line 2: friction and control
            double control = 100.0 - (metrics.EstimatedFriction * 100.0); // Higher friction = less control
            control = Math.Clamp(control, 0, 100);
            sb.AppendLine($"FRICTION: {metrics.EstimatedFriction:F2} | CONTROL: {control:F0}%");

            // line 3: turn metrics
            sb.Append($"YAW: {metrics.CurrentYaw:F0}° | TURN RATE: {metrics.TurnRate:F1}°/s | LOSS: {FormatSpeed(metrics.TurnSpeedLoss, unit)}");

            return sb.ToString();
        }

        /// <summary>
        /// create a compact single-line display
        /// </summary>
        public static string FormatCompact(MovementMetrics metrics, SpeedUnit unit = SpeedUnit.MetersPerSecond)
        {
            double speed = metrics.HorizontalSpeed;
            double peak = metrics.PeakHorizontalSpeed;
            double avg = metrics.AverageHorizontalSpeed;

            return $"{FormatSpeed(speed, unit)} | Peak: {FormatSpeed(peak, unit)} | Avg: {FormatSpeed(avg, unit)}";
        }

        /// <summary>
        /// format progress bar for visual representation
        /// </summary>
        public static string CreateProgressBar(double value, double max, int width = 10, char fillChar = '=', char emptyChar = ' ', char markerChar = '•')
        {
            if (max <= 0) return new string(emptyChar, width);

            double percentage = Math.Clamp(value / max, 0, 1);
            int fillCount = (int)(percentage * width);
            int markerPos = Math.Min(fillCount, width - 1);

            var sb = new StringBuilder(width);
            sb.Append('[');

            for (int i = 0; i < width; i++)
            {
                if (i < fillCount && i != markerPos)
                    sb.Append(fillChar);
                else if (i == markerPos)
                    sb.Append(markerChar);
                else
                    sb.Append(emptyChar);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }
}
