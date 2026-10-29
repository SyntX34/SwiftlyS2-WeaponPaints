using System.ComponentModel;

namespace PMM_WeaponPaints.Config;

/*
  Everything the server owner can change.
  The file is configs/plugins/PMM_WeaponPaints/config.jsonc, section PMM_WeaponPaints.
*/
public sealed class PluginConfig
{
    [Description("Chat commands that open the locker. Written without the ! or / and without the css_ prefix.")]
    public List<string> Commands { get; set; } = new() { "pws" };

    [Description("Locker language, ru or en.")]
    public string Language { get; set; } = "en";

    [Description("Seconds to wait before the window is drawn. Keeps the game menu from fighting with it.")]
    public float OpenDelay { get; set; } = 0.2f;

    [Description("Seconds to wait before the mouse is handed to the locker.")]
    public float InputDelay { get; set; } = 0.2f;

    [Description("Which connection from swiftlys2/configs/database.jsonc holds the WeaponPaints tables.")]
    public string DatabaseConnection { get; set; } = "default";

    [Description("Write a detailed log next to the plugin and echo the lines to the server console.")]
    public bool Debug { get; set; } = false;
}
