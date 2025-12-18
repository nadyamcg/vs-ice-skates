using System;
using HarmonyLib;
using IceSkates.src.Commands;
using IceSkates.src.CameraControl;
using IceSkates.src.Hud;
using IceSkates.src.Movement;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using IceSkates.src.Config;

namespace IceSkates.src
{
    /// <summary>
    /// main mod system for Ice Skates - handles initialization and coordination of all subsystems
    /// </summary>
    public class IceSkatesModSystem : ModSystem
    {
        public const string ModId = "iceskates";

        // singleton instance for easy access from other classes
        public static IceSkatesModSystem Instance { get; private set; } = null!;

        // API references
        public ICoreAPI Api { get; private set; } = null!;
        public ICoreClientAPI? ClientApi { get; private set; }
        public ICoreServerAPI? ServerApi { get; private set; }

        // configuration system
        public IceSkatesConfig Config { get; private set; } = null!;

        // harmony instance for patching
        private Harmony? _harmony;

        // camera controller (client-side only)
        private SkatingCameraController? _cameraController;

        // speed overlay HUD (client-side only)
        private SpeedOverlay? _speedOverlay;

        // enhanced movement overlay (client-side only)
        private EnhancedMovementOverlay? _enhancedMovementOverlay;

        // logger shortcut
        public ILogger Logger => Mod.Logger;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Instance = this;
            Api = api;

            Logger.Notification($"[{ModId}] Ice Skates mod loading...");

            // load configuration
            LoadConfig();

            // initialize Harmony for patching
            _harmony = new Harmony(ModId);
            _harmony.PatchAll();

            Logger.Notification($"[{ModId}] Ice Skates mod started!");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            ClientApi = api;

            Logger.Notification($"[{ModId}] Client-side initialization...");

            // initialize skating camera controller
            _cameraController = new SkatingCameraController();
            _cameraController.Initialize(api);

            // movement overlay - choose between enhanced or legacy based on config
            if (Config.ShowSpeedOverlay)
            {
                if (Config.UseEnhancedMovementOverlay)
                {
                    // enhanced movement overlay with comprehensive metrics
                    _enhancedMovementOverlay = new EnhancedMovementOverlay(api)
                    {
                        UpdateIntervalSeconds = Config.MovementOverlayUpdateInterval
                    };

                    // parse display mode from config
                    if (Enum.TryParse<MovementDisplayMode>(Config.MovementOverlayDisplayMode, out var displayMode))
                    {
                        _enhancedMovementOverlay.DisplayMode = displayMode;
                    }

                    // parse speed unit from config
                    if (Enum.TryParse<SpeedUnit>(Config.MovementOverlaySpeedUnit, out var speedUnit))
                    {
                        _enhancedMovementOverlay.SpeedUnit = speedUnit;
                    }

                    // apply style settings
                    _enhancedMovementOverlay.UpdateStyle(
                        Config.MovementOverlayFontSize,
                        EnumDialogArea.LeftTop,
                        Config.MovementOverlayOffsetX,
                        Config.MovementOverlayOffsetY
                    );

                    api.Event.LevelFinalize += () =>
                    {
                        _enhancedMovementOverlay?.TryOpen();
                        _enhancedMovementOverlay?.Reset();
                    };

                    api.Event.LeaveWorld += () => _enhancedMovementOverlay?.TryClose();

                    Logger.Notification($"[{ModId}] Enhanced movement overlay initialized (Mode: {Config.MovementOverlayDisplayMode})");
                }
                else
                {
                    // legacy simple speed overlay
                    _speedOverlay = new SpeedOverlay(api)
                    {
                        UpdateIntervalSeconds = 0.25,
                        HorizontalOnly = true
                    };

                    api.Event.LevelFinalize += () =>
                    {
                        _speedOverlay?.TryOpen();
                        _speedOverlay?.Reset();
                    };

                    api.Event.LeaveWorld += () => _speedOverlay?.TryClose();

                    Logger.Notification($"[{ModId}] Legacy speed overlay initialized");
                }
            }

            // register client-side commands for overlay control
            ClientCommands.Register(api);

            // TODO: register other client-side systems
            // - client-side prediction for smooth skating
            // - HUD elements for debug mode
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            ServerApi = api;

            Logger.Notification($"[{ModId}] Server-side initialization...");

            // register developer commands
            RegisterCommands(api);

            // TODO: register server-side systems
            // - physics patches
            // - network channels for synchronization
            // - config export/logging system
        }
        private void LoadConfig()
        {
            try
            {
                var loadedConfig = Api.LoadModConfig<IceSkatesConfig>($"{ModId}.json");
                if (loadedConfig == null)
                {
                    Logger.Warning($"[{ModId}] No config found, creating default configuration");
                    Config = new IceSkatesConfig();
                    Api.StoreModConfig(Config, $"{ModId}.json");
                }
                else
                {
                    Config = loadedConfig;
                    Logger.Notification($"[{ModId}] Configuration loaded successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[{ModId}] Failed to load config: {ex.Message}");
                Config = new IceSkatesConfig();
            }
        }

        public void ReloadConfig()
        {
            Logger.Notification($"[{ModId}] Reloading configuration...");
            LoadConfig();

            // TODO: broadcast config changes to all connected clients
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            if (Config.EnableDevCommands)
            {
                DevCommands.Register(api);
                Logger.Notification($"[{ModId}] Developer commands registered");
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            // clean up overlays
            _speedOverlay?.Dispose();
            _enhancedMovementOverlay?.Dispose();

            Logger.Notification($"[{ModId}] Ice Skates mod unloading...");
        }

        /// <summary>
        /// get access to the enhanced movement overlay (if enabled)
        /// useful for other systems that want to read movement metrics
        /// </summary>
        public EnhancedMovementOverlay? GetEnhancedMovementOverlay() => _enhancedMovementOverlay;
    }
}

