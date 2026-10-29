namespace WeaponPaints.Config;

/* The config after a cleanup pass: one command name, one connection, sane numbers. */
public sealed class PluginSettings
{
    public string RefreshCommand { get; private init; } = "css_wp";

    public string DatabaseConnection { get; private init; } = "default";

    public float DefaultWear { get; private init; }

    public bool ApplyOnSpawn { get; private init; }

    public static PluginSettings From(PluginConfig? config)
    {
        config ??= new PluginConfig();

        string name = (config.RefreshCommand ?? "").Trim().TrimStart('!', '/').Trim();
        if (name.StartsWith("css_", StringComparison.OrdinalIgnoreCase))
        {
            name = name[4..];
        }

        if (name.Length == 0)
        {
            name = "wp";
        }

        string connection = (config.DatabaseConnection ?? "").Trim();
        float wear = config.DefaultWear;
        if (wear <= 0f || wear > 1f)
        {
            wear = 0.01f;
        }

        return new PluginSettings
        {
            RefreshCommand = "css_" + name,
            DatabaseConnection = connection.Length > 0 ? connection : "default",
            DefaultWear = wear,
            ApplyOnSpawn = config.ApplyOnSpawn
        };
    }
}
