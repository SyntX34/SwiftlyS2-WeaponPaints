using PMM_WeaponPaints.Core;

namespace PMM_WeaponPaints.Config;

/*
  The config after a cleanup pass, so the rest of the plugin never has to
  worry about a half empty file. Commands are stored the way the console
  wants them, with the css_ prefix already in place.
*/
public sealed class PluginSettings
{
    public List<string> Commands { get; private init; } = new();

    // One of en, ru, da, de, fr, fi, es or hu.
    public string Language { get; private init; } = "en";

    public float OpenDelay { get; private init; }

    public float InputDelay { get; private init; }

    public string DatabaseConnection { get; private init; } = "default";

    public bool Debug { get; set; }

    public static PluginSettings From(PluginConfig? config)
    {
        config ??= new PluginConfig();

        List<string> commands = new();
        foreach (string entry in config.Commands ?? new List<string>())
        {
            string name = entry.Trim().TrimStart('!', '/').Trim();
            if (name.StartsWith("css_", StringComparison.OrdinalIgnoreCase))
            {
                name = name[4..];
            }

            if (name.Length > 0 && !commands.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                commands.Add(name);
            }
        }

        if (commands.Count == 0)
        {
            commands.Add("pws");
        }

        string connection = (config.DatabaseConnection ?? "").Trim();

        return new PluginSettings
        {
            Commands = commands,
            Language = LockerText.Normalize(config.Language),
            OpenDelay = Math.Max(0f, config.OpenDelay),
            InputDelay = Math.Max(0f, config.InputDelay),
            DatabaseConnection = connection.Length > 0 ? connection : "default",
            Debug = config.Debug
        };
    }
}
