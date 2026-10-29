using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using PMM_WeaponPaints.Catalog;
using PMM_WeaponPaints.Core;
using PMM_WeaponPaints.Data;
using PMM_WeaponPaints.Services;
using PMM_WeaponPaints.View;

namespace PMM_WeaponPaints.Commands;

/*
  css_pmm_debug prints what the locker knows right now. A 0 or a 1 after it
  switches the debug log off or on without a restart.
*/
public sealed class DebugCommands
{
    private readonly ISwiftlyCore _core;
    private readonly LockerStore _store;
    private readonly DebugLog _debug;
    private readonly DatabaseGate _gate;
    private readonly LayoutFactory _layout;
    private readonly IconLibrary _icons;
    private readonly ItemLibrary _items;
    private readonly TriggerBook _triggers;
    private Guid _command;

    public DebugCommands(ISwiftlyCore core, LockerStore store, DebugLog debug, DatabaseGate gate,
        LayoutFactory layout, IconLibrary icons, ItemLibrary items, TriggerBook triggers)
    {
        _core = core;
        _store = store;
        _debug = debug;
        _gate = gate;
        _layout = layout;
        _icons = icons;
        _items = items;
        _triggers = triggers;
    }

    public void Register()
    {
        _command = _core.Command.RegisterCommand("css_pmm_debug", Run, registerRaw: true, permission: "",
            helpText: "PMM_WeaponPaints state. Add 0 or 1 to switch the debug log");
    }

    public void Unregister()
    {
        if (_command != Guid.Empty)
        {
            _core.Command.UnregisterCommand(_command);
            _command = Guid.Empty;
        }
    }

    private void Run(ICommandContext context)
    {
        string arg = context.Args.Length > 0 ? context.Args[0].Trim() : "";
        if (arg is "0" or "1")
        {
            _debug.Settings.Debug = arg == "1";
            if (_debug.Settings.Debug)
            {
                _debug.Open(_core.PluginPath);
            }
        }

        List<string> lines = new()
        {
            $"PMM_WeaponPaints {LockerConstants.PluginVersion} debug={_debug.On} log={_debug.FilePath ?? "-"}",
            $"database={(_gate.Ready ? "ok" : "not connected")} layout={(_layout.Alive() ? "ok" : "none")}",
            $"pictures={_icons.Count} agents={_items.Agents.Count} music={_items.Music.Count} coins={_items.Pins.Count}",
            "triggers: " + string.Join(", ", _triggers.Groups.Keys)
        };

        IPlayer? player = context.Sender;
        if (PlayerTools.Fits(player))
        {
            SlotState state = _store.Get(player.Slot);
            lines.Add($"you: open={state.Open} view={state.View} active={state.Active} menu='{state.Menu?.Title}' cards={state.Cards?.Count ?? 0} page={state.Page}");
            foreach (string line in lines)
            {
                _debug.Write(player, line);
            }
        }

        foreach (string line in lines)
        {
            context.Reply("[PMM-DBG] " + line);
        }
    }
}
