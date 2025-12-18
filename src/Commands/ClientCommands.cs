using System;
using System.Text;
using IceSkates.src.Movement;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace IceSkates.src.Commands
{
    /// <summary>
    /// client-side commands for overlay control and other client-only features.
    /// these commands run locally and don't require server privileges.
    /// </summary>
    public static class ClientCommands
    {
        public static void Register(ICoreClientAPI api)
        {
            api.ChatCommands.Create("overlay")
                .WithDescription("Control movement overlay (client-side)")
                .BeginSubCommand("toggle")
                    .WithDescription("Toggle overlay visibility")
                    .HandleWith(_ => OnToggleOverlay(api))
                .EndSubCommand()
                .BeginSubCommand("mode")
                    .WithDescription("Set display mode (Minimal/Simple/Developer/SessionSummary/IceSkating)")
                    .WithArgs(api.ChatCommands.Parsers.Word("mode"))
                    .HandleWith(args => OnSetOverlayMode(api, args))
                .EndSubCommand()
                .BeginSubCommand("cycle")
                    .WithDescription("Cycle through display modes")
                    .HandleWith(_ => OnCycleMode(api))
                .EndSubCommand()
                .BeginSubCommand("unit")
                    .WithDescription("Set speed unit (MetersPerSecond/KilometersPerHour/MilesPerHour/BlocksPerSecond)")
                    .WithArgs(api.ChatCommands.Parsers.Word("unit"))
                    .HandleWith(args => OnSetSpeedUnit(api, args))
                .EndSubCommand()
                .BeginSubCommand("reset")
                    .WithDescription("Reset movement metrics (start new session)")
                    .HandleWith(_ => OnResetMetrics(api))
                .EndSubCommand()
                .BeginSubCommand("resetpeaks")
                    .WithDescription("Reset peak values only")
                    .HandleWith(_ => OnResetPeaks(api))
                .EndSubCommand()
                .BeginSubCommand("stats")
                    .WithDescription("Show current movement statistics")
                    .HandleWith(_ => OnShowStats(api))
                .EndSubCommand();

            api.Logger.Notification($"[{IceSkatesModSystem.ModId}] Client overlay commands registered (/overlay)");
        }

        private static TextCommandResult OnToggleOverlay(ICoreClientAPI api)
        {
            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled. Set UseEnhancedMovementOverlay=true in config.");
            }

            if (overlay.IsOpened())
            {
                overlay.TryClose();
                return TextCommandResult.Success("Overlay hidden");
            }
            else
            {
                overlay.TryOpen();
                return TextCommandResult.Success("Overlay shown");
            }
        }

        private static TextCommandResult OnSetOverlayMode(ICoreClientAPI api, TextCommandCallingArgs args)
        {
            var modeStr = args[0] as string;

            if (string.IsNullOrEmpty(modeStr))
            {
                return TextCommandResult.Error("Usage: /overlay mode <Minimal|Simple|Developer|SessionSummary|IceSkating>");
            }

            if (!Enum.TryParse<MovementDisplayMode>(modeStr, true, out var mode))
            {
                return TextCommandResult.Error($"Invalid mode: {modeStr}. Options: Minimal, Simple, Developer, SessionSummary, IceSkating");
            }

            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled.");
            }

            overlay.SetDisplayMode(mode);
            return TextCommandResult.Success($"Display mode set to: {mode}");
        }

        private static TextCommandResult OnCycleMode(ICoreClientAPI api)
        {
            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled.");
            }

            overlay.CycleDisplayMode();
            return TextCommandResult.Success($"Display mode: {overlay.DisplayMode}");
        }

        private static TextCommandResult OnSetSpeedUnit(ICoreClientAPI api, TextCommandCallingArgs args)
        {
            var unitStr = args[0] as string;

            if (string.IsNullOrEmpty(unitStr))
            {
                return TextCommandResult.Error("Usage: /overlay unit <MetersPerSecond|KilometersPerHour|MilesPerHour|BlocksPerSecond>");
            }

            if (!Enum.TryParse<SpeedUnit>(unitStr, true, out var unit))
            {
                return TextCommandResult.Error($"Invalid unit: {unitStr}. Options: MetersPerSecond, KilometersPerHour, MilesPerHour, BlocksPerSecond");
            }

            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled.");
            }

            overlay.SpeedUnit = unit;
            return TextCommandResult.Success($"Speed unit set to: {unit}");
        }

        private static TextCommandResult OnResetMetrics(ICoreClientAPI api)
        {
            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled.");
            }

            overlay.Reset();
            return TextCommandResult.Success("Movement metrics reset - new session started");
        }

        private static TextCommandResult OnResetPeaks(ICoreClientAPI api)
        {
            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled.");
            }

            overlay.ResetPeaks();
            return TextCommandResult.Success("Peak values reset");
        }

        private static TextCommandResult OnShowStats(ICoreClientAPI api)
        {
            var overlay = IceSkatesModSystem.Instance.GetEnhancedMovementOverlay();
            if (overlay == null)
            {
                return TextCommandResult.Error("Enhanced movement overlay is not enabled.");
            }

            var metrics = overlay.GetMetrics();
            var sb = new StringBuilder();

            sb.AppendLine("=== MOVEMENT STATISTICS ===");
            sb.AppendLine($"Session: {metrics.SessionDuration.TotalSeconds:F1}s");
            sb.AppendLine($"Speed: {metrics.HorizontalSpeed:F2} m/s");
            sb.AppendLine($"Peak: {metrics.PeakSpeed:F2} m/s");
            sb.AppendLine($"Avg: {metrics.AverageSpeed:F2} m/s");
            sb.AppendLine($"Distance: {metrics.TotalDistance:F1} m");
            sb.AppendLine($"Slide: {metrics.SlideDistance:F1} m");

            return TextCommandResult.Success(sb.ToString());
        }
    }
}
