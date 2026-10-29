using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Commands;
using PMM_WeaponPaints.Config;
using PMM_WeaponPaints.Core;
using PMM_WeaponPaints.Data;
using PMM_WeaponPaints.Equip;
using PMM_WeaponPaints.Hooks;
using PMM_WeaponPaints.Locker;
using PMM_WeaponPaints.Services;
using PMM_WeaponPaints.View;

namespace PMM_WeaponPaints;

/*
  Panorama locker for WeaponPaints, ported to SwiftlyS2.

  The plugin draws the WeaponPaints item lists as a card grid on a panorama
  window and saves what a player picks into the WeaponPaints tables, using the
  database connection from configs/database.jsonc. It has no other plugin or
  library to load beside SwiftlyS2 itself.
*/
[PluginMetadata(
    Id = "PMM_WeaponPaints",
    Version = LockerConstants.PluginVersion,
    Name = "PMM WeaponPaints",
    Author = "Rimmer",
    Description = "Panorama locker for WeaponPaints. SwiftlyS2 port by SyntX34.",
    Website = "https://github.com/RRimmer/PMM-WeaponPaints")]
public sealed class Plugin : BasePlugin
{
    private DebugLog? _debug;
    private LockerStore? _store;
    private LockerWindow? _window;
    private LockerCommands? _commands;
    private DebugCommands? _debugCommands;
    private PlayerHooks? _hooks;

    public Plugin(ISwiftlyCore core) : base(core)
    {
    }

    public override void Load(bool hotReload)
    {
        PluginSettings settings = ReadSettings();

        _debug = new DebugLog(Core.Logger, settings);
        _debug.Open(Core.PluginPath);
        _debug.Write(null, $"load {LockerConstants.PluginVersion} hotReload={hotReload} folder={Core.PluginPath} language={settings.Language}");

        LockerText text = new(Core.Logger);
        text.Load(Path.Combine(Core.PluginPath, "resources", "translations"), settings.Language);

        ItemLibrary items = new();
        items.Load(Path.Combine(Core.PluginPath, "resources", "pmm_items.json"), Core.Logger);

        IconLibrary icons = new();
        icons.Load(Path.Combine(Core.PluginPath, "resources", "icons.json"), Core.Logger);

        TriggerBook triggers = new();
        new TriggerLoader(triggers, Core.Logger).Load(Core.PluginPath);

        DatabaseGate gate = new(() => Core.Database.GetConnection(settings.DatabaseConnection), Core.Logger);
        new SchemaInstaller(gate, Core.Logger).Run(Path.Combine(Core.PluginPath, "resources", "sql"));
        gate.TryPing();
        _debug.Write(null, $"database={gate.Ready} connection={settings.DatabaseConnection}");

        WpDatabase database = new(gate);
        EquipService equip = new(gate);
        MenuBook menus = new(items, text);
        _store = new LockerStore();
        HudWriter hud = new();
        LayoutFactory layout = new(Core, _debug);
        CardFactory cards = new(items, icons, text, menus);
        HomeSummary summary = new(items, icons, text, menus);
        GridRenderer grid = new(layout, hud, cards, summary, triggers, text, items, _debug);

        _window = new LockerWindow(Core, _store, settings, _debug, triggers, menus, items, text, database, equip, layout, grid);

        _commands = new LockerCommands(Core, _window, settings, _debug);
        _commands.Register();

        _debugCommands = new DebugCommands(Core, _store, _debug, gate, layout, icons, items, triggers);
        _debugCommands.Register();

        _hooks = new PlayerHooks(Core, _store, _window, _debug);
        _hooks.Attach();

        Core.Logger.LogInformation("PMM_WeaponPaints {Version} is ready. The locker opens with {Commands}.",
            LockerConstants.PluginVersion, string.Join(", ", settings.Commands.Select(c => "!" + c)));
    }

    public override void Unload()
    {
        _debug?.Write(null, "unload");

        _hooks?.Detach();
        _debugCommands?.Unregister();
        _commands?.Unregister();
        _store?.CloseAll();
        _debug?.Close();

        _hooks = null;
        _debugCommands = null;
        _commands = null;
        _window = null;
        _store = null;
        _debug = null;
    }

    private PluginSettings ReadSettings()
    {
        Core.Configuration.InitializeJsonWithModel<PluginConfig>("config.jsonc", "PMM_WeaponPaints");
        PluginConfig? config = Core.Configuration.Manager.GetSection("PMM_WeaponPaints").Get<PluginConfig>();
        return PluginSettings.From(config);
    }
}
