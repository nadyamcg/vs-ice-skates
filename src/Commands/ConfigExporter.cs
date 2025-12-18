using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IceSkates.src.Config;

namespace IceSkates.src.Commands
{
    /// <summary>
    /// exports configuration to dedicated log files for playtester sharing and analysis
    /// </summary>
    public static class ConfigExporter
    {
        private const string ExportDirectory = "ModConfig/IceSkates/exports";

        /// <summary>
        /// export current configuration to a timestamped log file
        /// </summary>
        public static string ExportToLog(IceSkatesConfig config)
        {
            var api = IceSkatesModSystem.Instance.Api;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var exportDir = Path.Combine(api.GetOrCreateDataPath(ExportDirectory));

            Directory.CreateDirectory(exportDir);

            var fileName = $"iceskates_config_{timestamp}.txt";
            var filePath = Path.Combine(exportDir, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("=================================================================");
            sb.AppendLine("      ICE SKATES CONFIGURATION EXPORT");
            sb.AppendLine("=================================================================");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Mod Version: 0.0.1");
            sb.AppendLine($"Game Version: {api.World?.Config?.GetString("GameVersion") ?? "Unknown"}");
            sb.AppendLine("=================================================================");
            sb.AppendLine();

            // export all config properties grouped by category
            var properties = typeof(IceSkatesConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            var currentCategory = "";
            foreach (var prop in properties.OrderBy(p => GetCategory(p.Name)).ThenBy(p => p.Name))
            {
                var category = GetCategory(prop.Name);
                if (category != currentCategory)
                {
                    currentCategory = category;
                    sb.AppendLine();
                    sb.AppendLine($"--- {category.ToUpper()} ---");
                }

                var value = prop.GetValue(config);
                var xmlDoc = prop.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

                if (!string.IsNullOrEmpty(xmlDoc))
                {
                    sb.AppendLine($"# {xmlDoc}");
                }

                sb.AppendLine($"{prop.Name} = {value}");
            }

            sb.AppendLine();
            sb.AppendLine("=================================================================");
            sb.AppendLine("  PRESET QUICK COPY (for /iceskate set commands)");
            sb.AppendLine("=================================================================");
            sb.AppendLine();

            // Generate quick copy commands
            foreach (var prop in properties.Where(p => p.CanWrite).OrderBy(p => p.Name))
            {
                var value = prop.GetValue(config);
                sb.AppendLine($"/iceskate set {prop.Name} {value}");
            }

            sb.AppendLine();
            sb.AppendLine("=================================================================");
            sb.AppendLine("  End of export");
            sb.AppendLine("=================================================================");

            File.WriteAllText(filePath, sb.ToString());

            IceSkatesModSystem.Instance.Logger.Notification($"[iceskates] Configuration exported to: {filePath}");

            return filePath;
        }

        /// <summary>
        /// export a minimal "interesting config" - just the physics values
        /// </summary>
        public static string ExportPhysicsOnly(IceSkatesConfig config, string note = "")
        {
            var api = IceSkatesModSystem.Instance.Api;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var exportDir = Path.Combine(api.GetOrCreateDataPath(ExportDirectory));

            Directory.CreateDirectory(exportDir);

            var fileName = $"iceskates_physics_{timestamp}.txt";
            var filePath = Path.Combine(exportDir, fileName);

            var sb = new StringBuilder();
            sb.AppendLine("=== Ice Skating Physics Snapshot ===");
            sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            if (!string.IsNullOrEmpty(note))
            {
                sb.AppendLine($"Note: {note}");
            }

            sb.AppendLine();
            sb.AppendLine("Ice Tau Values:");
            sb.AppendLine($"  WalkUp: {config.IceTauWalkUp}");
            sb.AppendLine($"  WalkDown: {config.IceTauWalkDown}");
            sb.AppendLine($"  SprintUp: {config.IceTauSprintUp}");
            sb.AppendLine($"  SprintDown: {config.IceTauSprintDown}");
            sb.AppendLine($"  NoInput: {config.IceTauNoInput}");
            sb.AppendLine($"  TurnStrength: {config.IceTauTurnStrength}");
            sb.AppendLine();
            sb.AppendLine($"Max Speed Multiplier: {config.MaxSkatingSpeedMultiplier}");
            sb.AppendLine($"Off-Ice Walk Speed: {config.OffIceWalkSpeedMultiplier}");
            sb.AppendLine($"Off-Ice Sprint Speed: {config.OffIceSprintSpeedMultiplier}");
            sb.AppendLine();
            sb.AppendLine("Quick Copy:");
            sb.AppendLine($"/iceskate preset hybrid");
            sb.AppendLine($"/iceskate set IceTauWalkUp {config.IceTauWalkUp}");
            sb.AppendLine($"/iceskate set IceTauWalkDown {config.IceTauWalkDown}");
            sb.AppendLine($"/iceskate set IceTauSprintUp {config.IceTauSprintUp}");
            sb.AppendLine($"/iceskate set IceTauSprintDown {config.IceTauSprintDown}");
            sb.AppendLine($"/iceskate set IceTauNoInput {config.IceTauNoInput}");
            sb.AppendLine($"/iceskate set IceTauTurnStrength {config.IceTauTurnStrength}");
            sb.AppendLine($"/iceskate set MaxSkatingSpeedMultiplier {config.MaxSkatingSpeedMultiplier}");

            File.WriteAllText(filePath, sb.ToString());

            return filePath;
        }

        private static string GetCategory(string propertyName)
        {
            if (propertyName.StartsWith("IceTau")) return "Ice Physics";
            if (propertyName.StartsWith("OffIce")) return "Off-Ice Penalty";
            if (propertyName.StartsWith("Surface")) return "Surface Detection";
            if (propertyName.StartsWith("MaxSkating") || propertyName.StartsWith("Minimum")) return "Speed Limits";
            if (propertyName.StartsWith("ThirdPerson") || propertyName.StartsWith("Force") || propertyName.StartsWith("HighSpeed")) return "Camera";
            if (propertyName.StartsWith("Stamina")) return "Stamina";
            if (propertyName.StartsWith("Enable") || propertyName.StartsWith("Require")) return "General";
            if (propertyName.StartsWith("Debug") || propertyName.StartsWith("Auto") || propertyName.StartsWith("Show") || propertyName.StartsWith("Log")) return "Developer Tools";
            return "Other";
        }
    }
}
