using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using WeaponPaints.Commands;
using WeaponPaints.Config;
using WeaponPaints.Data;
using WeaponPaints.Hooks;
using WeaponPaints.Skin;

namespace WeaponPaints;

/*
  WeaponPaints for SwiftlyS2.

  It keeps weapon, knife, glove, agent, music kit and coin choices in the
  WeaponPaints tables and puts the skin of the current side on the player. The
  tables come from resources/sql/schema.sql, which runs on start, and the
  connection is the one from configs/database.jsonc, so nothing else has to be
  installed.
*/
[PluginMetadata(
    Id = "WeaponPaints",
    Version = WeaponPaintsVersion,
    Name = "WeaponPaints",
    Author = "Nereziel",
    Description = "Skins, knives, gloves and agents from a database. SwiftlyS2 port by SyntX34.",
    Website = "https://github.com/Nereziel/cs2-WeaponPaints")]
public sealed class Plugin : BasePlugin
{
    private const string WeaponPaintsVersion = "1.0.0";

    private GameHooks? _hooks;
    private RefreshCommands? _commands;
    private LoadoutService? _loadout;

    public Plugin(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        Core.Configuration.InitializeJsonWithModel<PluginConfig>("config.jsonc", "WeaponPaints");
        PluginConfig? config = Core.Configuration.Manager.GetSection("WeaponPaints").Get<PluginConfig>();
        PluginSettings settings = PluginSettings.From(config);

        DatabaseGate gate = new(() => Core.Database.GetConnection(settings.DatabaseConnection), Core.Logger);
        new SchemaInstaller(gate, Core.Logger).Run(Path.Combine(Core.PluginPath, "resources", "sql"));
        gate.TryPing();

        LoadoutStore store = new(gate);
        SkinApplier applier = new(Core, Core.Logger, settings);
        _loadout = new LoadoutService(Core, Core.Logger, store, applier);

        _commands = new RefreshCommands(Core, _loadout, settings);
        _commands.Register();

        _hooks = new GameHooks(Core, _loadout, settings);
        _hooks.Attach();

        Core.Logger.LogInformation("WeaponPaints {Version} is ready. Database connection: {Connection} ({State}).",
            WeaponPaintsVersion, settings.DatabaseConnection, gate.Ready ? "ok" : "not reachable");
    }

    public override void Unload()
    {
        _hooks?.Detach();
        _commands?.Unregister();
        _loadout?.ForgetAll();

        _hooks = null;
        _commands = null;
        _loadout = null;
    }
}
