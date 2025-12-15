using System;
using HarmonyLib;
using IceSkates.src.Commands;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace IceSkates.src
{
    /// <summary>
    /// Main mod system for Ice Skates - handles initialization and coordination of all subsystems
    /// </summary>
    public class IceSkatesModSystem : ModSystem
    {
        public const string ModId = "iceskates";

        // Singleton instance for easy access from other classes
        public static IceSkatesModSystem Instance { get; private set; } = null!;

        // API references
        public ICoreAPI Api { get; private set; } = null!;
        public ICoreClientAPI? ClientApi { get; private set; }
        public ICoreServerAPI? ServerApi { get; private set; }

        // Configuration system
        public IceSkatesConfig Config { get; private set; } = null!;

        // Harmony instance for patching
        private Harmony? _harmony;

        // Logger shortcut
        public ILogger Logger => Mod.Logger;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Instance = this;
            Api = api;

            Logger.Notification($"[{ModId}] Ice Skates mod loading...");

            // Load configuration
            LoadConfig();

            // Initialize Harmony for patching
            _harmony = new Harmony(ModId);
            _harmony.PatchAll();

            Logger.Notification($"[{ModId}] Ice Skates mod started!");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            ClientApi = api;

            Logger.Notification($"[{ModId}] Client-side initialization...");

            // TODO: Register client-side systems
            // - Third-person camera integration
            // - Client-side prediction for smooth skating
            // - HUD elements for debug mode
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            ServerApi = api;

            Logger.Notification($"[{ModId}] Server-side initialization...");

            // Register developer commands
            RegisterCommands(api);

            // TODO: Register server-side systems
            // - Physics patches
            // - Network channels for synchronization
            // - Config export/logging system
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

            // TODO: Broadcast config changes to all connected clients
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
            Logger.Notification($"[{ModId}] Ice Skates mod unloading...");
        }
    }
}
