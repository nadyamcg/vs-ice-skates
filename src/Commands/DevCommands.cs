using System;
using System.Linq;
using System.Reflection;
using System.Text;
using IceSkates.src.Commands;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace IceSkates.src.Commands
{
    /// <summary>
    /// Developer commands for runtime tweaking of ice skating parameters
    /// Requires OP privileges on server
    /// </summary>
    public static class DevCommands
    {
        public static void Register(ICoreServerAPI api)
        {
            var parsers = api.ChatCommands.Parsers;

            api.ChatCommands.Create("iceskate")
                .WithDescription("Ice skating developer commands")
                .RequiresPrivilege(Privilege.controlserver)
                .BeginSubCommand("set")
                    .WithDescription("Set a configuration parameter")
                    .WithArgs(parsers.Word("parameter"), parsers.All("value"))
                    .HandleWith(OnSetParameter)
                .EndSubCommand()
                .BeginSubCommand("get")
                    .WithDescription("Get a configuration parameter value")
                    .WithArgs(parsers.Word("parameter"))
                    .HandleWith(OnGetParameter)
                .EndSubCommand()
                .BeginSubCommand("list")
                    .WithDescription("List all configuration parameters")
                    .HandleWith(OnListParameters)
                .EndSubCommand()
                .BeginSubCommand("export")
                    .WithDescription("Export current configuration to log file")
                    .HandleWith(OnExportConfig)
                .EndSubCommand()
                .BeginSubCommand("reload")
                    .WithDescription("Reload configuration from file")
                    .HandleWith(OnReloadConfig)
                .EndSubCommand()
                .BeginSubCommand("reset")
                    .WithDescription("Reset configuration to defaults")
                    .HandleWith(OnResetConfig)
                .EndSubCommand()
                .BeginSubCommand("debug")
                    .WithDescription("Toggle debug mode")
                    .WithArgs(parsers.OptionalBool("enabled"))
                    .HandleWith(OnToggleDebug)
                .EndSubCommand()
                .BeginSubCommand("preset")
                    .WithDescription("Load a physics preset (arcade/simulation/hybrid)")
                    .WithArgs(parsers.Word("preset"))
                    .HandleWith(OnLoadPreset)
                .EndSubCommand();
        }

        private static TextCommandResult OnSetParameter(TextCommandCallingArgs args)
        {
            var config = IceSkatesModSystem.Instance.Config;
            var paramName = args[0] as string;
            var valueStr = args[1] as string;

            if (string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(valueStr))
            {
                return TextCommandResult.Error("Usage: /iceskate set <parameter> <value>");
            }

            // Use reflection to find and set the property
            var property = typeof(IceSkatesConfig).GetProperty(paramName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
            {
                return TextCommandResult.Error($"Unknown parameter: {paramName}. Use /iceskate list to see all parameters.");
            }

            if (!property.CanWrite)
            {
                return TextCommandResult.Error($"Parameter '{paramName}' is read-only.");
            }

            try
            {
                object? newValue = null;
                var propertyType = property.PropertyType;

                // Parse value based on property type
                if (propertyType == typeof(bool))
                {
                    newValue = bool.Parse(valueStr);
                }
                else if (propertyType == typeof(int))
                {
                    newValue = int.Parse(valueStr);
                }
                else if (propertyType == typeof(double))
                {
                    newValue = double.Parse(valueStr);
                }
                else if (propertyType == typeof(string))
                {
                    newValue = valueStr;
                }
                else
                {
                    return TextCommandResult.Error($"Unsupported parameter type: {propertyType.Name}");
                }

                var oldValue = property.GetValue(config);
                property.SetValue(config, newValue);
                config.Validate(); // Ensure values are in valid ranges

                // Save to disk
                IceSkatesModSystem.Instance.Api.StoreModConfig(config, $"{IceSkatesModSystem.ModId}.json");

                // Auto-export if enabled
                if (config.AutoExportConfigOnChange)
                {
                    ConfigExporter.ExportToLog(config);
                }

                return TextCommandResult.Success($"Set {property.Name} = {newValue} (was: {oldValue})");
            }
            catch (Exception ex)
            {
                return TextCommandResult.Error($"Failed to set parameter: {ex.Message}");
            }
        }

        private static TextCommandResult OnGetParameter(TextCommandCallingArgs args)
        {
            var config = IceSkatesModSystem.Instance.Config;
            var paramName = args[0] as string;

            if (string.IsNullOrEmpty(paramName))
            {
                return TextCommandResult.Error("Usage: /iceskate get <parameter>");
            }

            var property = typeof(IceSkatesConfig).GetProperty(paramName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
            {
                return TextCommandResult.Error($"Unknown parameter: {paramName}");
            }

            var value = property.GetValue(config);
            return TextCommandResult.Success($"{property.Name} = {value}");
        }

        private static TextCommandResult OnListParameters(TextCommandCallingArgs args)
        {
            var config = IceSkatesModSystem.Instance.Config;
            var properties = typeof(IceSkatesConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .OrderBy(p => p.Name);

            var sb = new StringBuilder();
            sb.AppendLine("=== Ice Skating Configuration Parameters ===");
            sb.AppendLine();

            var currentCategory = "";
            foreach (var prop in properties)
            {
                // Group by naming convention (e.g., "IceTau", "OffIce", etc.)
                var category = GetCategory(prop.Name);
                if (category != currentCategory)
                {
                    currentCategory = category;
                    sb.AppendLine($"--- {category} ---");
                }

                var value = prop.GetValue(config);
                sb.AppendLine($"  {prop.Name} = {value}");
            }

            sb.AppendLine();
            sb.AppendLine("Use '/iceskate set <parameter> <value>' to modify");

            return TextCommandResult.Success(sb.ToString());
        }

        private static TextCommandResult OnExportConfig(TextCommandCallingArgs args)
        {
            var config = IceSkatesModSystem.Instance.Config;
            var filePath = ConfigExporter.ExportToLog(config);
            return TextCommandResult.Success($"Configuration exported to: {filePath}");
        }

        private static TextCommandResult OnReloadConfig(TextCommandCallingArgs args)
        {
            IceSkatesModSystem.Instance.ReloadConfig();
            return TextCommandResult.Success("Configuration reloaded from file");
        }

        private static TextCommandResult OnResetConfig(TextCommandCallingArgs args)
        {
            var config = new IceSkatesConfig();
            IceSkatesModSystem.Instance.Api.StoreModConfig(config, $"{IceSkatesModSystem.ModId}.json");
            IceSkatesModSystem.Instance.ReloadConfig();
            return TextCommandResult.Success("Configuration reset to defaults");
        }

        private static TextCommandResult OnToggleDebug(TextCommandCallingArgs args)
        {
            var config = IceSkatesModSystem.Instance.Config;

            if (args.Parsers[0].IsMissing)
            {
                // Toggle
                config.DebugMode = !config.DebugMode;
            }
            else
            {
                // Set to specific value
                config.DebugMode = (bool)args[0];
            }

            IceSkatesModSystem.Instance.Api.StoreModConfig(config, $"{IceSkatesModSystem.ModId}.json");
            return TextCommandResult.Success($"Debug mode: {(config.DebugMode ? "ENABLED" : "DISABLED")}");
        }

        private static TextCommandResult OnLoadPreset(TextCommandCallingArgs args)
        {
            var presetName = (args[0] as string)?.ToLower();
            var config = IceSkatesModSystem.Instance.Config;

            switch (presetName)
            {
                case "arcade":
                    // Easy controls, fast turning, quick stops
                    config.SetIceTaus(
                        walkUp: 1.5,
                        walkDown: 1.0,
                        sprintUp: 1.2,
                        sprintDown: 0.8,
                        noInput: 1.0,
                        turnStrength: 0.5
                    );
                    config.MaxSkatingSpeedMultiplier = 2.0;
                    break;

                case "simulation":
                    // Realistic physics, wide turns, hard to stop
                    config.SetIceTaus(
                        walkUp: 0.5,
                        walkDown: 1.5,
                        sprintUp: 0.4,
                        sprintDown: 1.2,
                        noInput: 2.0,
                        turnStrength: 1.2
                    );
                    config.MaxSkatingSpeedMultiplier = 1.3;
                    break;

                case "hybrid":
                    // Balanced approach (default)
                    config.SetIceTaus(
                        walkUp: 0.8,
                        walkDown: 1.2,
                        sprintUp: 0.6,
                        sprintDown: 1.0,
                        noInput: 1.5,
                        turnStrength: 0.8
                    );
                    config.MaxSkatingSpeedMultiplier = 1.5;
                    break;

                default:
                    return TextCommandResult.Error($"Unknown preset: {presetName}. Available: arcade, simulation, hybrid");
            }

            config.Validate();
            IceSkatesModSystem.Instance.Api.StoreModConfig(config, $"{IceSkatesModSystem.ModId}.json");

            return TextCommandResult.Success($"Loaded '{presetName}' preset");
        }

        private static string GetCategory(string propertyName)
        {
            if (propertyName.StartsWith("IceTau")) return "Ice Physics";
            if (propertyName.StartsWith("OffIce")) return "Off-Ice Penalty";
            if (propertyName.StartsWith("Surface")) return "Surface Detection";
            if (propertyName.StartsWith("MaxSkating") || propertyName.StartsWith("Minimum")) return "Speed Limits";
            if (propertyName.StartsWith("ThirdPerson") || propertyName.StartsWith("Force") || propertyName.StartsWith("HighSpeed")) return "Camera";
            if (propertyName.StartsWith("Stamina")) return "Stamina (Future)";
            if (propertyName.StartsWith("Enable") || propertyName.StartsWith("Require")) return "General";
            if (propertyName.StartsWith("Debug") || propertyName.StartsWith("Auto") || propertyName.StartsWith("Show") || propertyName.StartsWith("Log")) return "Developer Tools";
            return "Other";
        }
    }
}
