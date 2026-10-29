using System.ComponentModel;

namespace WeaponPaints.Config;

/*
  Server side settings for the skin plugin.
  The file is configs/plugins/WeaponPaints/config.jsonc, section WeaponPaints.
*/
public sealed class PluginConfig
{
    [Description("Chat command that reloads the loadout of the player who typed it. Written without ! or / or css_.")]
    public string RefreshCommand { get; set; } = "wp";

    [Description("Which connection from swiftlys2/configs/database.jsonc holds the WeaponPaints tables.")]
    public string DatabaseConnection { get; set; } = "default";

    [Description("Wear used for a skin that has no wear of its own in the database.")]
    public float DefaultWear { get; set; } = 0.01f;

    [Description("Put the loadout back on the player after every spawn.")]
    public bool ApplyOnSpawn { get; set; } = true;
}
