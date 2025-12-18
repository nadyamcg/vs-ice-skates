using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace IceSkates.src.Movement
{
    /// <summary>
    /// enhanced HUD overlay showing comprehensive movement analysis.
    /// can be used standalone or as part of the Ice Skates mod.
    /// supports multiple display modes from minimal to full developer overlay.
    /// </summary>
    public sealed class EnhancedMovementOverlay : HudElement
    {
        private readonly GuiElementDynamicText _text;
        private readonly MovementMetrics _metrics;
        private readonly CairoFont _font;

        // configuration
        public MovementDisplayMode DisplayMode { get; set; } = MovementDisplayMode.Simple;
        public SpeedUnit SpeedUnit { get; set; } = SpeedUnit.MetersPerSecond;
        public double UpdateIntervalSeconds { get; set; } = 0.1; // faster updates for more responsive display

        // tracking - use absolute game time for accurate velocity calculations
        private double _timeSinceLastDisplayUpdate;
        private double _lastUpdateTimeMs = -1; // absolute game time in milliseconds
        private readonly Vec3d _lastPos = new(double.NaN, double.NaN, double.NaN);
        private const double MinUpdateIntervalMs = 50; // minimum 50ms between updates for stable measurements

        // input tracking (optional, could be enhanced with actual input detection)
        private bool _hasInput = true; // default assumption

        public override bool PrefersUngrabbedMouse => false;
        public override bool Focusable => false;
        public override bool Focused => false;
        public override double DrawOrder => 0.5;
        public override bool CaptureAllInputs() => false;
        public override bool CaptureRawMouse() => false;
        public override bool ShouldReceiveKeyboardEvents() => false;
        public override bool ShouldReceiveMouseEvents() => false;

        public EnhancedMovementOverlay(ICoreClientAPI api) : base(api)
        {
            _metrics = new MovementMetrics();

            // font setup
            _font = CairoFont.WhiteDetailText()
                .WithColor([1, 1, 1, 1])
                .WithFont(GuiStyle.StandardFontName)
                .WithStroke([0, 0, 0, 0.85], 2)
                .WithFontSize(16);

            double padding = 10;
            double width = 300;
            double height = 200;

            var dialogBounds = ElementBounds.Fixed(
                EnumDialogArea.LeftTop,
                padding,
                padding,
                width,
                height
            );

            var bgBounds = ElementBounds.Fill.WithFixedPadding(6);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            var textBounds = ElementBounds.Fixed(0, 0, width - 12, height - 12);

            SingleComposer = api.Gui.CreateCompo("EnhancedMovementOverlay", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .BeginChildElements(bgBounds)
                    .AddDynamicText("", _font, textBounds, "movementtext")
                .EndChildElements()
                .Compose();

            _text = SingleComposer.GetDynamicText("movementtext");
        }

        public override void OnFinalizeFrame(float dt)
        {
            base.OnFinalizeFrame(dt);

            var world = capi.World;
            var player = world?.Player?.Entity;
            if (player == null || world == null)
            {
                Reset();
                return;
            }

            // use absolute game time for accurate velocity calculations
            // this prevents velocity spikes from frame timing variations
            double currentTimeMs = world.ElapsedMilliseconds;

            var pos = player.Pos.XYZ;
            double yaw = player.Pos.Yaw * GameMath.RAD2DEG; // Convert to degrees

            // initialize on first frame
            if (_lastUpdateTimeMs < 0 || double.IsNaN(_lastPos.X))
            {
                _lastPos.Set(pos);
                _lastUpdateTimeMs = currentTimeMs;
                return;
            }

            // check if enough time has passed for a metrics update
            double elapsedMs = currentTimeMs - _lastUpdateTimeMs;
            if (elapsedMs >= MinUpdateIntervalMs)
            {
                double elapsedSeconds = elapsedMs / 1000.0;

                // detect input
                _hasInput = DetectPlayerInput(player);

                // update metrics with ACTUAL elapsed time for accurate velocity calculation
                _metrics.Update(pos, _hasInput, yaw, elapsedSeconds);

                _lastPos.Set(pos);
                _lastUpdateTimeMs = currentTimeMs;
            }

            // update display at configured interval (independent of physics updates)
            _timeSinceLastDisplayUpdate += dt;
            if (_timeSinceLastDisplayUpdate >= UpdateIntervalSeconds)
            {
                UpdateDisplay();
                _timeSinceLastDisplayUpdate = 0;
            }
        }

        /// <summary>
        /// detect if player is providing movement input (heuristic)
        /// </summary>
        private bool DetectPlayerInput(Entity player)
        {
            // check if player has controls (moving)
            if (player is EntityPlayer entityPlayer)
            {
                var controls = entityPlayer.Controls;
                return controls.Forward || controls.Backward || controls.Left || controls.Right || controls.Sprint || controls.Sneak;
            }

            // fallback: assume input if moving above threshold
            double speed = _metrics.HorizontalSpeed;
            return speed > 0.1;
        }

        /// <summary>
        /// update the displayed text based on current mode
        /// </summary>
        private void UpdateDisplay()
        {
            string displayText = MovementDisplayFormatter.Format(_metrics, DisplayMode, SpeedUnit);

            _text.Text = displayText;
            _text.Font.AutoBoxSize(displayText, _text.Bounds);
            _text.Bounds.CalcWorldBounds();
            SingleComposer.Bounds.CalcWorldBounds();
            _text.RecomposeText(true);
        }

        /// <summary>
        /// update overlay style and positioning
        /// </summary>
        public void UpdateStyle(int fontSize, EnumDialogArea align, int offsetX, int offsetY)
        {
            _text.Font.WithFont(GuiStyle.StandardFontName)
                .WithFontSize(fontSize);

            SingleComposer.Bounds.Alignment = align;
            SingleComposer.Bounds.WithFixedAlignmentOffset(offsetX, offsetY);
            _text.Bounds.CalcWorldBounds();
            SingleComposer.Bounds.CalcWorldBounds();
        }

        /// <summary>
        /// change display mode at runtime
        /// </summary>
        public void SetDisplayMode(MovementDisplayMode mode)
        {
            DisplayMode = mode;
            UpdateDisplay();
        }

        /// <summary>
        /// cycle through display modes
        /// </summary>
        public void CycleDisplayMode()
        {
            DisplayMode = DisplayMode switch
            {
                MovementDisplayMode.Minimal => MovementDisplayMode.Simple,
                MovementDisplayMode.Simple => MovementDisplayMode.Developer,
                MovementDisplayMode.Developer => MovementDisplayMode.SessionSummary,
                MovementDisplayMode.SessionSummary => MovementDisplayMode.IceSkating,
                MovementDisplayMode.IceSkating => MovementDisplayMode.Minimal,
                _ => MovementDisplayMode.Simple
            };

            UpdateDisplay();
        }

        /// <summary>
        /// get access to underlying metrics for external use
        /// </summary>
        public MovementMetrics GetMetrics() => _metrics;

        /// <summary>
        /// reset metrics (start new session)
        /// </summary>
        public void Reset()
        {
            _metrics.Reset();
            _timeSinceLastDisplayUpdate = 0;
            _lastUpdateTimeMs = -1;
            _lastPos.Set(double.NaN, double.NaN, double.NaN);
        }

        /// <summary>
        /// reset only peak values
        /// </summary>
        public void ResetPeaks()
        {
            _metrics.ResetPeaks();
        }

        public override void Dispose()
        {
            TryClose();
            base.Dispose();
        }
    }
}
