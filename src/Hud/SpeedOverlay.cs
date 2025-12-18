using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace IceSkates.src.Hud
{
    /// <summary>
    /// simple HUD overlay that shows the player's current speed in m/s (1 block = 1 meter)
    /// default: horizontal (XZ) speed, averaged over a short interval for readability
    /// </summary>
    public sealed class SpeedOverlay : HudElement
    {
        private readonly GuiElementDynamicText _text;

        private double _lastUpdateTimeMs = -1; // absolute game time in milliseconds
        private readonly double[] _speedHistory = new double[10]; // rolling speed buffer
        private int _speedHistoryIndex;
        private readonly Vec3d _lastPos = new(double.NaN, double.NaN, double.NaN);

        private readonly CairoFont _font;

        public double UpdateIntervalSeconds { get; set; } = 0.25; // average window
        public bool HorizontalOnly { get; set; } = true;           // ignore Y motion by default

        public override bool PrefersUngrabbedMouse => false;
        public override bool Focusable => false;
        public override bool Focused => false;
        public override double DrawOrder => 0.5;
        public override bool CaptureAllInputs() => false;
        public override bool CaptureRawMouse() => false;
        public override bool ShouldReceiveKeyboardEvents() => false;
        public override bool ShouldReceiveMouseEvents() => false;

        public SpeedOverlay(ICoreClientAPI api) : base(api)
        {
            _font = CairoFont.WhiteDetailText()
                .WithColor([1, 1, 1, 1])
                .WithFont(GuiStyle.StandardFontName)
                .WithStroke([0, 0, 0, 0.85], 2)
                .WithFontSize(18);

            var dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.LeftTop)
                .WithFixedAlignmentOffset(10, 10)
                .WithFixedPadding(10, 0);

            var textBounds = ElementBounds.FixedOffseted(EnumDialogArea.LeftTop, 0, 0, 50, 50);

            SingleComposer = api.Gui.CreateCompo("IceSkatesSpeedOverlay", dialogBounds)
                .AddDynamicText("", _font, textBounds, "speedtext")
                .Compose();

            _text = SingleComposer.GetDynamicText("speedtext");
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
            double currentTimeMs = world.ElapsedMilliseconds;
            var pos = player.Pos;

            if (_lastUpdateTimeMs < 0 || double.IsNaN(_lastPos.X))
            {
                _lastPos.Set(pos.X, pos.Y, pos.Z);
                _lastUpdateTimeMs = currentTimeMs;
                return;
            }

            // minimum update interval for stable measurements (50ms)
            double elapsedMs = currentTimeMs - _lastUpdateTimeMs;
            if (elapsedMs < 50) return;

            double elapsedSeconds = elapsedMs / 1000.0;

            double dx = pos.X - _lastPos.X;
            double dz = pos.Z - _lastPos.Z;
            double dy = pos.Y - _lastPos.Y;

            double distance = HorizontalOnly ? GameMath.Sqrt(dx * dx + dz * dz) : GameMath.Sqrt(dx * dx + dy * dy + dz * dz);
            double instantSpeed = distance / elapsedSeconds;

            // add to rolling history
            _speedHistory[_speedHistoryIndex] = instantSpeed;
            _speedHistoryIndex = (_speedHistoryIndex + 1) % _speedHistory.Length;

            // calculate smoothed speed (average of history)
            double smoothedSpeed = 0;
            for (int i = 0; i < _speedHistory.Length; i++)
            {
                smoothedSpeed += _speedHistory[i];
            }
            smoothedSpeed /= _speedHistory.Length;

            _lastPos.Set(pos.X, pos.Y, pos.Z);
            _lastUpdateTimeMs = currentTimeMs;

            // update display at configured interval
            UpdateSpeedText(smoothedSpeed);
        }

        private void UpdateSpeedText(double mps)
        {
            // show one decimal place, e.g., 6.3 m/s
            string text = string.Format("{0:0.0} m/s", mps);
            _text.Text = text;
            _text.Font.AutoBoxSize(text, _text.Bounds);
            _text.Bounds.CalcWorldBounds();
            SingleComposer.Bounds.CalcWorldBounds();
            _text.RecomposeText(true);
        }

        public void UpdateStyle(int fontSize, EnumDialogArea align, int offsetX, int offsetY)
        {
            _text.Font.WithFont(GuiStyle.StandardFontName)
                .WithFontSize(fontSize);

            SingleComposer.Bounds.Alignment = align;
            SingleComposer.Bounds.WithFixedAlignmentOffset(offsetX, offsetY);
            _text.Bounds.CalcWorldBounds();
            SingleComposer.Bounds.CalcWorldBounds();
        }

        public override void Dispose()
        {
            TryClose();
            base.Dispose();
        }

        public void Reset()
        {
            _lastUpdateTimeMs = -1;
            _speedHistoryIndex = 0;
            Array.Clear(_speedHistory, 0, _speedHistory.Length);
            _lastPos.Set(double.NaN, double.NaN, double.NaN);
        }
    }
}